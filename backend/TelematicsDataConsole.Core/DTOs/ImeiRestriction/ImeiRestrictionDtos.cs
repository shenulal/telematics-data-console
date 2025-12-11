using System.ComponentModel.DataAnnotations;

namespace TelematicsDataConsole.Core.DTOs.ImeiRestriction;

public class ImeiRestrictionDto
{
    public int RestrictionId { get; set; }
    public int TechnicianId { get; set; }
    public string? TechnicianName { get; set; }
    public int? DeviceId { get; set; }
    public string? DeviceImei { get; set; }
    public int? TagId { get; set; }
    public string? TagName { get; set; }
    public short? AccessType { get; set; }
    public short? Priority { get; set; }
    public string? Reason { get; set; }
    public bool? IsPermanent { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string? Notes { get; set; }
    public short Status { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class CreateImeiRestrictionDto
{
    [Required]
    public int TechnicianId { get; set; }

    public int? DeviceId { get; set; }

    public int? TagId { get; set; }

    [Required]
    public short AccessType { get; set; }

    public short? Priority { get; set; }

    [MaxLength(255)]
    public string? Reason { get; set; }

    public bool IsPermanent { get; set; } = true;

    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidUntil { get; set; }

    [MaxLength(1024)]
    public string? Notes { get; set; }
}

public class UpdateImeiRestrictionDto
{
    public int? DeviceId { get; set; }
    public int? TagId { get; set; }
    public short? AccessType { get; set; }
    public short? Priority { get; set; }

    [MaxLength(255)]
    public string? Reason { get; set; }

    public bool? IsPermanent { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }

    [MaxLength(1024)]
    public string? Notes { get; set; }

    public short? Status { get; set; }
}

