using TelematicsDataConsole.Core.DTOs.Auth;

namespace TelematicsDataConsole.Core.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(LoginRequest request, string ipAddress, string userAgent);
    Task<AuthResult> RefreshTokenAsync(string refreshToken);
    Task<bool> LogoutAsync(int userId);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request);
    Task<bool> ResetPasswordAsync(string email);
    Task<bool> ValidateTokenAsync(string token);
}

public class AuthResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public UserInfo? User { get; set; }
}

public class UserInfo
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public int? ResellerId { get; set; }
    public int? TechnicianId { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}

