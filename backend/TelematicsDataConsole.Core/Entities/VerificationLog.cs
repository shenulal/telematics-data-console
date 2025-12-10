namespace TelematicsDataConsole.Core.Entities;

/// <summary>
/// Represents a verification log entry.
/// - One entry per technician per device per day (first check)
/// - If same device checked again after TIME_GAP_HOURS, creates new entry
/// </summary>
public class VerificationLog
{
    /// <summary>
    /// Time gap in hours between checks that triggers a new entry
    /// </summary>
    public const int TIME_GAP_HOURS = 4;

    public int VerificationId { get; set; }
    public int TechnicianId { get; set; }
    public int DeviceId { get; set; }
    public string Imei { get; set; } = string.Empty;
    public string VerificationStatus { get; set; } = "Verified";
    public string? Notes { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime? GpsTime { get; set; }
    public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Technician Technician { get; set; } = null!;
}

