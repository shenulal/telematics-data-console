using Microsoft.EntityFrameworkCore;
using TelematicsDataConsole.Core.DTOs;
using TelematicsDataConsole.Core.DTOs.User;
using TelematicsDataConsole.Core.Entities;
using TelematicsDataConsole.Core.Interfaces.Services;
using TelematicsDataConsole.Infrastructure.Data;

namespace TelematicsDataConsole.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public UserService(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<PagedResult<UserDto>> GetAllAsync(int page = 1, int pageSize = 20, string? search = null, short? status = null, int? resellerId = null, bool excludeSuperAdmin = false)
    {
        var query = _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.Reseller)
            .AsQueryable();

        // Filter by reseller if specified (Reseller Admin can only see their own users)
        if (resellerId.HasValue)
        {
            query = query.Where(u => u.ResellerId == resellerId.Value);
        }

        // Exclude SUPERADMIN users for non-superadmin users
        if (excludeSuperAdmin)
        {
            query = query.Where(u => !u.UserRoles.Any(ur => ur.Role.RoleName == SystemRoles.SuperAdmin));
        }

        if (status.HasValue)
            query = query.Where(u => u.Status == status.Value);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(u =>
                u.Username.Contains(search) ||
                u.Email.Contains(search) ||
                (u.FullName != null && u.FullName.Contains(search)));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserDto
            {
                UserId = u.UserId,
                Username = u.Username,
                Email = u.Email,
                Mobile = u.Mobile,
                Phone = u.Phone,
                AliasName = u.AliasName,
                FullName = u.FullName,
                ResellerId = u.ResellerId,
                ResellerName = u.Reseller != null ? u.Reseller.CompanyName : null,
                Status = u.Status,
                LastLoginAt = u.LastLoginAt,
                LockoutUntil = u.LockoutUntil,
                Roles = u.UserRoles.Select(ur => ur.Role.RoleName).ToList(),
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            })
            .ToListAsync();

        return new PagedResult<UserDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        return await _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.Reseller)
            .Where(u => u.UserId == id)
            .Select(u => new UserDto
            {
                UserId = u.UserId,
                Username = u.Username,
                Email = u.Email,
                Mobile = u.Mobile,
                Phone = u.Phone,
                AliasName = u.AliasName,
                FullName = u.FullName,
                ResellerId = u.ResellerId,
                ResellerName = u.Reseller != null ? u.Reseller.CompanyName : null,
                Status = u.Status,
                LastLoginAt = u.LastLoginAt,
                LockoutUntil = u.LockoutUntil,
                Roles = u.UserRoles.Select(ur => ur.Role.RoleName).ToList(),
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<UserDto?> GetByUsernameAsync(string username)
    {
        return await _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Where(u => u.Username == username)
            .Select(u => new UserDto
            {
                UserId = u.UserId,
                Username = u.Username,
                Email = u.Email,
                FullName = u.FullName,
                Status = u.Status,
                Roles = u.UserRoles.Select(ur => ur.Role.RoleName).ToList(),
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<UserDto> CreateAsync(CreateUserDto dto, int createdBy)
    {
        if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
            throw new InvalidOperationException("Username already exists");

        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            throw new InvalidOperationException("Email already exists");

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, 11),
            Mobile = dto.Mobile,
            Phone = dto.Phone,
            AliasName = dto.AliasName,
            FullName = dto.FullName,
            ResellerId = dto.ResellerId,
            Status = dto.Status,
            LockoutUntil = dto.Status == 3 ? dto.LockoutUntil : null, // 3 = Locked
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Handle role assignment - support both RoleIds and Role names
        var roleIds = new List<int>();
        if (dto.RoleIds != null && dto.RoleIds.Count > 0)
        {
            roleIds = dto.RoleIds;
        }
        else if (dto.Roles != null && dto.Roles.Count > 0)
        {
            // Map role names to role IDs
            roleIds = await _context.Roles
                .Where(r => dto.Roles.Contains(r.RoleName))
                .Select(r => r.RoleId)
                .ToListAsync();
        }

        if (roleIds.Count > 0)
        {
            foreach (var roleId in roleIds)
            {
                await _context.UserRoles.AddAsync(new UserRole
                {
                    UserId = user.UserId,
                    RoleId = roleId,
                    CreatedBy = createdBy
                });
            }
            await _context.SaveChangesAsync();
        }

        // If user has TECHNICIAN role, automatically create a Technician record
        var technicianRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == SystemRoles.Technician);
        if (technicianRole != null && roleIds.Contains(technicianRole.RoleId))
        {
            // Check if technician record already exists
            var existingTechnician = await _context.Technicians.FirstOrDefaultAsync(t => t.UserId == user.UserId);
            if (existingTechnician == null)
            {
                var technician = new Technician
                {
                    UserId = user.UserId,
                    ResellerId = user.ResellerId,
                    Status = 1, // Active
                    CreatedBy = createdBy,
                    UpdatedBy = createdBy
                };
                await _context.Technicians.AddAsync(technician);
                await _context.SaveChangesAsync();
            }
        }

        await _auditService.LogAsync(createdBy, AuditActions.Create, "User", user.UserId.ToString(), null, dto);
        return (await GetByIdAsync(user.UserId))!;
    }

    public async Task<UserDto> UpdateAsync(int id, UpdateUserDto dto, int updatedBy)
    {
        var user = await _context.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.UserId == id)
            ?? throw new KeyNotFoundException("User not found");

        if (dto.Email != null && dto.Email != user.Email)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email && u.UserId != id))
                throw new InvalidOperationException("Email already exists");
            user.Email = dto.Email;
        }

        if (dto.Mobile != null) user.Mobile = dto.Mobile;
        if (dto.Phone != null) user.Phone = dto.Phone;
        if (dto.AliasName != null) user.AliasName = dto.AliasName;
        if (dto.FullName != null) user.FullName = dto.FullName;
        if (dto.ResellerId.HasValue) user.ResellerId = dto.ResellerId;
        if (dto.Status.HasValue)
        {
            user.Status = dto.Status.Value;
            // Handle lockout - set LockoutUntil only when status is LOCKED (3)
            if (dto.Status.Value == 3)
            {
                user.LockoutUntil = dto.LockoutUntil;
            }
            else
            {
                user.LockoutUntil = null;
            }
        }
        user.UpdatedBy = updatedBy;
        user.UpdatedAt = DateTime.UtcNow;

        // Handle role assignment - support both RoleIds and Role names
        var roleIds = new List<int>();
        if (dto.RoleIds != null && dto.RoleIds.Count > 0)
        {
            roleIds = dto.RoleIds;
        }
        else if (dto.Roles != null && dto.Roles.Count > 0)
        {
            // Map role names to role IDs
            roleIds = await _context.Roles
                .Where(r => dto.Roles.Contains(r.RoleName))
                .Select(r => r.RoleId)
                .ToListAsync();
        }

        if (roleIds.Count > 0)
        {
            _context.UserRoles.RemoveRange(user.UserRoles);
            foreach (var roleId in roleIds)
            {
                await _context.UserRoles.AddAsync(new UserRole
                {
                    UserId = user.UserId,
                    RoleId = roleId,
                    CreatedBy = updatedBy
                });
            }
        }

        await _context.SaveChangesAsync();

        // If user now has TECHNICIAN role, create Technician record if not exists
        var technicianRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == SystemRoles.Technician);
        if (technicianRole != null && roleIds.Contains(technicianRole.RoleId))
        {
            var existingTechnician = await _context.Technicians.FirstOrDefaultAsync(t => t.UserId == user.UserId);
            if (existingTechnician == null)
            {
                var technician = new Technician
                {
                    UserId = user.UserId,
                    ResellerId = user.ResellerId,
                    Status = 1, // Active
                    CreatedBy = updatedBy,
                    UpdatedBy = updatedBy
                };
                await _context.Technicians.AddAsync(technician);
                await _context.SaveChangesAsync();
            }
        }

        await _auditService.LogAsync(updatedBy, AuditActions.Update, "User", id.ToString());
        return (await GetByIdAsync(id))!;
    }

    public async Task<bool> DeleteAsync(int id, int deletedBy = 0)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;

        var oldValues = new { user.UserId, user.Username, user.Email, user.Status };
        user.Status = (short)UserStatus.Deleted;
        user.UpdatedBy = deletedBy;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(deletedBy, AuditActions.Delete, "User", id.ToString(), oldValues, null);
        return true;
    }

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            throw new InvalidOperationException("Current password is incorrect");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword, 11);
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ResetPasswordAsync(int userId, ResetPasswordDto dto, int adminId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword, 11);
        user.UpdatedBy = adminId;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(adminId, AuditActions.Update, "User", userId.ToString(), null, "Password reset");
        return true;
    }

    public async Task<List<UserDto>> GetByResellerIdAsync(int resellerId)
    {
        return await _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Where(u => u.ResellerId == resellerId)
            .Select(u => new UserDto
            {
                UserId = u.UserId,
                Username = u.Username,
                Email = u.Email,
                FullName = u.FullName,
                Status = u.Status,
                Roles = u.UserRoles.Select(ur => ur.Role.RoleName).ToList()
            })
            .ToListAsync();
    }

    public async Task<bool> IsSuperAdminAsync(int userId)
    {
        return await _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Where(u => u.UserId == userId)
            .AnyAsync(u => u.UserRoles.Any(ur => ur.Role.RoleName == SystemRoles.SuperAdmin));
    }
}
