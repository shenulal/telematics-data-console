namespace TelematicsDataConsole.Core.Entities;

public class ImeiRestriction
{
    public int RestrictionId { get; set; }
    public int TechnicianId { get; set; }
    public int? DeviceId { get; set; }
    public int? TagId { get; set; }
    public int? AccessType { get; set; }
    public int? Priority { get; set; }
    public string? Reason { get; set; }
    public bool? IsPermanent { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string? Notes { get; set; }
    public int Status { get; set; } = 1;
    public int? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public int? UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Technician Technician { get; set; } = null!;
    public virtual Tag? Tag { get; set; }
}

public enum AccessType
{
    Allow = 1,
    Deny = 2
}

public enum RestrictionStatus
{
    Inactive = 0,
    Active = 1,
    Expired = 2
}

