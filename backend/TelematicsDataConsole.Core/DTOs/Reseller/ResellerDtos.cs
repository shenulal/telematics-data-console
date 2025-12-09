using System.ComponentModel.DataAnnotations;

namespace TelematicsDataConsole.Core.DTOs.Reseller;

public class ResellerDto
{
    public int ResellerId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public short Status { get; set; }
    public int TechnicianCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateResellerDto
{
    [Required, MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? DisplayName { get; set; }

    [MaxLength(150)]
    public string? ContactPerson { get; set; }

    [EmailAddress, MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Mobile { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(255)]
    public string? AddressLine1 { get; set; }

    [MaxLength(255)]
    public string? AddressLine2 { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? State { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }
}

public class UpdateResellerDto : CreateResellerDto
{
    public short? Status { get; set; }
}

public class ResellerFilterDto : FilterBase
{
    public short? Status { get; set; }
    public string? Country { get; set; }
}

public class ResellerStatsDto
{
    public int ResellerId { get; set; }
    public int TotalTechnicians { get; set; }
    public int ActiveTechnicians { get; set; }
    public int TotalVerifications { get; set; }
    public int VerificationsThisMonth { get; set; }
}

public class UpdateResellerStatusDto
{
    public short Status { get; set; }
}

public class ResellerStatusUpdateResultDto
{
    public int ResellerId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public short NewStatus { get; set; }
    public string StatusText => NewStatus switch
    {
        0 => "INACTIVE",
        1 => "ACTIVE",
        2 => "SUSPENDED",
        _ => "UNKNOWN"
    };
    public int UsersUpdated { get; set; }
    public int TechniciansUpdated { get; set; }
    public int TagsUpdated { get; set; }
    public int RolesUpdated { get; set; }
    public string Message { get; set; } = string.Empty;
}

