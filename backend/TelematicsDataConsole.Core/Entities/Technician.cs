namespace TelematicsDataConsole.Core.Entities;

public class Technician
{
    public int TechnicianId { get; set; }
    public int UserId { get; set; }
    public int? ResellerId { get; set; }
    public string? EmployeeCode { get; set; }
    public string? Skillset { get; set; }
    public string? Certification { get; set; }
    public string? WorkRegion { get; set; }
    public int DailyLimit { get; set; }
    public short Status { get; set; } = 1;
    public int? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Reseller? Reseller { get; set; }
    public virtual ICollection<ImeiRestriction> ImeiRestrictions { get; set; } = new List<ImeiRestriction>();
    public virtual ICollection<VerificationLog> VerificationLogs { get; set; } = new List<VerificationLog>();
}

public enum TechnicianStatus : short
{
    Inactive = 0,
    Active = 1,
    Suspended = 2
}

