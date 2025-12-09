namespace TelematicsDataConsole.Core.DTOs.VerificationLog;

/// <summary>
/// DTO for verification log - simplified to match database schema
/// </summary>
public class VerificationLogDto
{
    public int VerificationId { get; set; }
    public int TechnicianId { get; set; }
    public string? TechnicianName { get; set; }
    public int DeviceId { get; set; }
    public DateTime VerifiedAt { get; set; }
}

/// <summary>
/// DTO for creating a verification log entry
/// Time gap check: if same technician checked same device within TIME_GAP_HOURS,
/// no new entry is created
/// </summary>
public class CreateVerificationLogDto
{
    public int TechnicianId { get; set; }
    public int DeviceId { get; set; }
}

public class VerificationLogFilterDto
{
    public int? TechnicianId { get; set; }
    public int? DeviceId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class VerificationStatisticsDto
{
    public int TotalVerifications { get; set; }
    public int UniqueDevices { get; set; }
    public int VerificationsToday { get; set; }
    public int VerificationsThisWeek { get; set; }
    public int VerificationsThisMonth { get; set; }
    public DateTime? LastVerificationAt { get; set; }
}

