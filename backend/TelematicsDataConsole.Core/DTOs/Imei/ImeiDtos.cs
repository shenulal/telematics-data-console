using System.ComponentModel.DataAnnotations;

namespace TelematicsDataConsole.Core.DTOs.Imei;

public class DeviceDataDto
{
    public int DeviceId { get; set; }
    public string Imei { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? DeviceModel { get; set; }
    public string? FirmwareVersion { get; set; }
    public bool IsOnline { get; set; }
    public GpsDataDto? LastGpsData { get; set; }
    public VehicleInfoDto? VehicleInfo { get; set; }
}

public class GpsDataDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Altitude { get; set; }
    public double? Speed { get; set; }
    public double? Heading { get; set; }
    public int? Satellites { get; set; }
    public int? SignalStrength { get; set; }
    public bool? IgnitionOn { get; set; }
    public double? BatteryVoltage { get; set; }
    public double? ExternalVoltage { get; set; }
    public DateTime GpsTime { get; set; }
    public DateTime ServerTime { get; set; }
}

public class VehicleInfoDto
{
    public int? VehicleId { get; set; }
    public string? PlateNumber { get; set; }
    public string? VehicleName { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public string? Vin { get; set; }
    public string? OwnerName { get; set; }
}

public class VerificationRequest
{
    [Required]
    public string Imei { get; set; } = string.Empty;

    public string VerificationStatus { get; set; } = "Verified";

    [MaxLength(500)]
    public string? Notes { get; set; }

    public GpsDataDto? GpsData { get; set; }
}

public class VerificationHistoryDto
{
    public int VerificationId { get; set; }
    public int DeviceId { get; set; }
    public string Imei { get; set; } = string.Empty;
    public string VerificationStatus { get; set; } = "Verified";
    public string? Notes { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime? GpsTime { get; set; }
    public DateTime VerifiedAt { get; set; }
}

public class VerificationHistoryFilterDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class VerificationHistoryPagedResult
{
    public List<VerificationHistoryDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

