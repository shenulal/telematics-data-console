namespace TelematicsDataConsole.Core.Entities;

public class Permission
{
    public int PermissionId { get; set; }
    public string PermissionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Module { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public static class Permissions
{
    // Technician permissions
    public const string TechnicianView = "technician.view";
    public const string TechnicianCreate = "technician.create";
    public const string TechnicianEdit = "technician.edit";
    public const string TechnicianDelete = "technician.delete";

    // Reseller permissions
    public const string ResellerView = "reseller.view";
    public const string ResellerCreate = "reseller.create";
    public const string ResellerEdit = "reseller.edit";
    public const string ResellerDelete = "reseller.delete";

    // IMEI permissions
    public const string ImeiVerify = "imei.verify";
    public const string ImeiViewData = "imei.view";  // Fixed: matches database value
    public const string ImeiRestrictionManage = "imei.restriction.manage";

    // Report permissions
    public const string ReportView = "report.view";
    public const string ReportExport = "report.export";

    // Audit permissions
    public const string AuditLogView = "audit.view";
}

