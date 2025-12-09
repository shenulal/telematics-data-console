namespace TelematicsDataConsole.Core.DTOs.User;

public class UserDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? Phone { get; set; }
    public string? AliasName { get; set; }
    public string? FullName { get; set; }
    public int? ResellerId { get; set; }
    public string? ResellerName { get; set; }
    public short Status { get; set; }
    public string StatusText => Status switch
    {
        0 => "INACTIVE",
        1 => "ACTIVE",
        2 => "SUSPENDED",
        3 => "LOCKED",
        4 => "DELETED",
        _ => "UNKNOWN"
    };
    public DateTime? LastLoginAt { get; set; }
    public DateTime? LockoutUntil { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateUserDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? Phone { get; set; }
    public string? AliasName { get; set; }
    public string? FullName { get; set; }
    public int? ResellerId { get; set; }
    public short Status { get; set; } = 1;
    public DateTime? LockoutUntil { get; set; }
    public List<int>? RoleIds { get; set; }
    public List<string>? Roles { get; set; } // Role names: SUPERADMIN, RESELLER ADMIN, TECHNICIAN, SUPERVISOR
}

public class UpdateUserDto
{
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public string? Phone { get; set; }
    public string? AliasName { get; set; }
    public string? FullName { get; set; }
    public int? ResellerId { get; set; }
    public short? Status { get; set; }
    public DateTime? LockoutUntil { get; set; }
    public List<int>? RoleIds { get; set; }
    public List<string>? Roles { get; set; } // Role names: SUPERADMIN, RESELLER ADMIN, TECHNICIAN, SUPERVISOR
}

public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class ResetPasswordDto
{
    public string NewPassword { get; set; } = string.Empty;
}

