namespace TelematicsDataConsole.Core.Entities;

public class User
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? Phone { get; set; }
    public string? AliasName { get; set; }
    public string? FullName { get; set; }
    public string? PasswordHash { get; set; }
    public int? ResellerId { get; set; }
    public short Status { get; set; } = 1;
    public DateTime? LastLoginAt { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutUntil { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Reseller? Reseller { get; set; }
    public virtual Technician? Technician { get; set; }
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

public enum UserStatus : short
{
    Inactive = 0,
    Active = 1,
    Suspended = 2,
    Locked = 3,
    Deleted = 4
}

