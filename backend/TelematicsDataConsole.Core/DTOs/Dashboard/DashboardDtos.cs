namespace TelematicsDataConsole.Core.DTOs.Dashboard;

/// <summary>
/// Dashboard statistics for Super Admin
/// </summary>
public class SuperAdminDashboardDto
{
    public int TotalResellers { get; set; }
    public int ActiveResellers { get; set; }
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalTechnicians { get; set; }
    public int ActiveTechnicians { get; set; }

    // Verification Summary (Combined - all technicians)
    public VerificationSummaryDto VerificationSummary { get; set; } = new();

    public List<ResellerSummaryDto> TopResellers { get; set; } = new();
    public List<TechnicianSummaryDto> TopTechnicians { get; set; } = new();
    public List<DailyVerificationDto> VerificationTrend { get; set; } = new();
    public List<HourlyVerificationDto> HourlyBreakdown { get; set; } = new();
}

/// <summary>
/// Dashboard statistics for Reseller Admin
/// </summary>
public class ResellerAdminDashboardDto
{
    public int ResellerId { get; set; }
    public string ResellerName { get; set; } = string.Empty;
    public int TotalTechnicians { get; set; }
    public int ActiveTechnicians { get; set; }
    public int TotalUsers { get; set; }

    // Verification Summary (Combined - all technicians in reseller)
    public VerificationSummaryDto VerificationSummary { get; set; } = new();

    public List<TechnicianSummaryDto> TopTechnicians { get; set; } = new();
    public List<DailyVerificationDto> VerificationTrend { get; set; } = new();
    public List<HourlyVerificationDto> HourlyBreakdown { get; set; } = new();
}

/// <summary>
/// Dashboard statistics for Supervisor
/// </summary>
public class SupervisorDashboardDto
{
    public int TotalTechnicians { get; set; }
    public int ActiveTechnicians { get; set; }

    // Verification Summary (Combined - all technicians supervised)
    public VerificationSummaryDto VerificationSummary { get; set; } = new();

    public List<TechnicianSummaryDto> TechnicianStats { get; set; } = new();
    public List<DailyVerificationDto> VerificationTrend { get; set; } = new();
    public List<HourlyVerificationDto> HourlyBreakdown { get; set; } = new();
}

/// <summary>
/// Dashboard statistics for Technician (Solo view)
/// </summary>
public class TechnicianDashboardDto
{
    public int TechnicianId { get; set; }
    public string TechnicianName { get; set; } = string.Empty;
    public int DailyLimit { get; set; }

    // Verification Summary (Solo - this technician only)
    public VerificationSummaryDto VerificationSummary { get; set; } = new();

    public int RemainingToday { get; set; }
    public DateTime? LastVerificationAt { get; set; }
    public List<RecentVerificationDto> RecentVerifications { get; set; } = new();
    public List<DailyVerificationDto> VerificationTrend { get; set; } = new();
    public List<HourlyVerificationDto> HourlyBreakdown { get; set; } = new();
}

/// <summary>
/// Summary of reseller for dashboard
/// </summary>
public class ResellerSummaryDto
{
    public int ResellerId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int TechnicianCount { get; set; }
    public int VerificationsThisMonth { get; set; }
}

/// <summary>
/// Summary of technician for dashboard
/// </summary>
public class TechnicianSummaryDto
{
    public int TechnicianId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ResellerId { get; set; }
    public string ResellerName { get; set; } = string.Empty;
    public int VerificationsToday { get; set; }
    public int VerificationsThisWeek { get; set; }
    public int VerificationsThisMonth { get; set; }
    public DateTime? LastVerificationAt { get; set; }
}

/// <summary>
/// Daily verification count for trend charts
/// </summary>
public class DailyVerificationDto
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}

/// <summary>
/// Recent verification for technician dashboard
/// </summary>
public class RecentVerificationDto
{
    public long VerificationId { get; set; }
    public int DeviceId { get; set; }
    public DateTime VerifiedAt { get; set; }
}

/// <summary>
/// Verification summary with counts and unique devices
/// </summary>
public class VerificationSummaryDto
{
    public int TotalVerificationsToday { get; set; }
    public int TotalVerificationsThisWeek { get; set; }
    public int TotalVerificationsThisMonth { get; set; }
    public int TotalVerificationsAllTime { get; set; }

    // Unique device counts
    public int UniqueDevicesToday { get; set; }
    public int UniqueDevicesThisWeek { get; set; }
    public int UniqueDevicesThisMonth { get; set; }
    public int UniqueDevicesAllTime { get; set; }

    // Average verifications
    public double AverageVerificationsPerDay { get; set; }
}

/// <summary>
/// Hourly verification breakdown for today
/// </summary>
public class HourlyVerificationDto
{
    public int Hour { get; set; } // 0-23
    public string HourLabel { get; set; } = string.Empty; // "12 AM", "1 PM", etc.
    public int Count { get; set; }
}

/// <summary>
/// Summary of technician for dashboard with unique device count
/// </summary>
public class TechnicianDetailedSummaryDto
{
    public int TechnicianId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DailyLimit { get; set; }
    public int VerificationsToday { get; set; }
    public int UniqueDevicesToday { get; set; }
    public int VerificationsThisWeek { get; set; }
    public int VerificationsThisMonth { get; set; }
    public double AveragePerDay { get; set; }
    public DateTime? LastVerificationAt { get; set; }
}
