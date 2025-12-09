using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TelematicsDataConsole.Core.DTOs.Auth;
using TelematicsDataConsole.Core.Entities;
using TelematicsDataConsole.Core.Interfaces;
using TelematicsDataConsole.Core.Interfaces.Services;
using TelematicsDataConsole.Infrastructure.Data;

namespace TelematicsDataConsole.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IAuditService _auditService;

    public AuthService(ApplicationDbContext context, IConfiguration configuration, IAuditService auditService)
    {
        _context = context;
        _configuration = configuration;
        _auditService = auditService;
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, string ipAddress, string userAgent)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .Include(u => u.Technician)
            .FirstOrDefaultAsync(u => u.Username == request.UsernameOrEmail || u.Email == request.UsernameOrEmail);

        if (user == null)
        {
            await _auditService.LogAsync(null, AuditActions.LoginFailed, "User", request.UsernameOrEmail);
            return new AuthResult { Success = false, Message = "Invalid username or password" };
        }

        if (user.Status != (short)UserStatus.Active)
        {
            return new AuthResult { Success = false, Message = "Account is not active" };
        }

        if (user.LockoutUntil.HasValue && user.LockoutUntil > DateTime.UtcNow)
        {
            return new AuthResult { Success = false, Message = "Account is temporarily locked. Please try again later." };
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockoutUntil = DateTime.UtcNow.AddMinutes(15);
            }
            await _context.SaveChangesAsync();
            await _auditService.LogAsync(user.UserId, AuditActions.LoginFailed, "User", user.UserId.ToString());
            return new AuthResult { Success = false, Message = "Invalid username or password" };
        }

        // Reset failed attempts on successful login
        user.FailedLoginAttempts = 0;
        user.LockoutUntil = null;
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList();
        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.PermissionName)
            .Distinct()
            .ToList();

        var accessToken = GenerateJwtToken(user, roles, permissions);
        var refreshToken = GenerateRefreshToken();

        await _auditService.LogAsync(user.UserId, AuditActions.Login, "User", user.UserId.ToString());

        return new AuthResult
        {
            Success = true,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(8),
            User = new UserInfo
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                ResellerId = user.ResellerId,
                TechnicianId = user.Technician?.TechnicianId,
                Roles = roles,
                Permissions = permissions
            }
        };
    }

    private string GenerateJwtToken(User user, List<string> roles, List<string> permissions)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new("TechnicianId", user.Technician?.TechnicianId.ToString() ?? ""),
            new("ResellerId", user.ResellerId?.ToString() ?? "")
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(permissions.Select(perm => new Claim("Permission", perm)));

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public Task<AuthResult> RefreshTokenAsync(string refreshToken) => throw new NotImplementedException();
    public Task<bool> LogoutAsync(int userId) => Task.FromResult(true);
    public Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request) => throw new NotImplementedException();
    public Task<bool> ResetPasswordAsync(string email) => throw new NotImplementedException();
    public Task<bool> ValidateTokenAsync(string token) => throw new NotImplementedException();
}

