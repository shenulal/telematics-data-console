using System.ComponentModel.DataAnnotations;

namespace TelematicsDataConsole.Core.DTOs.Technician;

public class TechnicianDto
{
    public int TechnicianId { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public int? ResellerId { get; set; }
    public string? ResellerName { get; set; }
    public string? EmployeeCode { get; set; }
    public string? Skillset { get; set; }
    public string? Certification { get; set; }
    public string? WorkRegion { get; set; }
    public int DailyLimit { get; set; }
    public short Status { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Number of active IMEI restrictions for this technician
    /// </summary>
    public int ImeiRestrictionCount { get; set; }

    /// <summary>
    /// IMEI Restriction mode: 0 = None, 1 = Allow List (only specific IMEIs allowed), 2 = Deny List (specific IMEIs blocked)
    /// </summary>
    public short ImeiRestrictionMode { get; set; }
}

public class CreateTechnicianDto
{
    [Required, MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? FullName { get; set; }

    public int? ResellerId { get; set; }

    [MaxLength(50)]
    public string? EmployeeCode { get; set; }

    [MaxLength(255)]
    public string? Skillset { get; set; }

    [MaxLength(255)]
    public string? Certification { get; set; }

    [MaxLength(100)]
    public string? WorkRegion { get; set; }

    public int DailyLimit { get; set; } = 50;
}

public class UpdateTechnicianDto
{
    [MaxLength(150)]
    public string? FullName { get; set; }

    public int? ResellerId { get; set; }

    [MaxLength(50)]
    public string? EmployeeCode { get; set; }

    [MaxLength(255)]
    public string? Skillset { get; set; }

    [MaxLength(255)]
    public string? Certification { get; set; }

    [MaxLength(100)]
    public string? WorkRegion { get; set; }

    public int? DailyLimit { get; set; }
    public short? Status { get; set; }
}

public class TechnicianFilterDto : FilterBase
{
    public int? ResellerId { get; set; }
    public short? Status { get; set; }
    public string? WorkRegion { get; set; }
}

public class TechnicianStatsDto
{
    public int TechnicianId { get; set; }
    public int TotalVerifications { get; set; }
    public int VerificationsToday { get; set; }
    public int VerificationsThisWeek { get; set; }
    public int VerificationsThisMonth { get; set; }
    public DateTime? LastVerificationAt { get; set; }
    public int RemainingDailyLimit { get; set; }
}

