namespace TelematicsDataConsole.Core.Entities;

public class Role
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public int? ResellerId { get; set; }  // Null for system roles, set for reseller-created roles
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Reseller? Reseller { get; set; }
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public static class SystemRoles
{
    public const string SuperAdmin = "SUPERADMIN";
    public const string ResellerAdmin = "RESELLER ADMIN";
    public const string Technician = "TECHNICIAN";
    public const string Supervisor = "SUPERVISOR";
}

