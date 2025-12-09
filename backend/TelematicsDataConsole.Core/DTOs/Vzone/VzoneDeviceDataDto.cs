using System.Text.Json.Serialization;

namespace TelematicsDataConsole.Core.DTOs.Vzone;

/// <summary>
/// DTO for Vzone API response - live device data
/// </summary>
public class VzoneDeviceDataDto
{
    [JsonPropertyName("imei")]
    public string Imei { get; set; } = string.Empty;

    [JsonPropertyName("trackTime")]
    public DateTime TrackTime { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("speed")]
    public decimal Speed { get; set; }

    [JsonPropertyName("location")]
    public VzoneLocationDto? Location { get; set; }

    [JsonPropertyName("data")]
    public List<VzoneIoDataDto> Data { get; set; } = new();
}

/// <summary>
/// Location information from Vzone API
/// </summary>
public class VzoneLocationDto
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("locationName")]
    public string? LocationName { get; set; }

    [JsonPropertyName("locationProximity")]
    public double? LocationProximity { get; set; }
}

/// <summary>
/// IO data from Vzone API (sensor readings)
/// </summary>
public class VzoneIoDataDto
{
    [JsonPropertyName("universalIOID")]
    public int? UniversalIOID { get; set; }

    [JsonPropertyName("universalIOName")]
    public string? UniversalIOName { get; set; }

    [JsonPropertyName("ioCode")]
    public string? IoCode { get; set; }

    [JsonPropertyName("ioName")]
    public string? IoName { get; set; }

    [JsonPropertyName("value")]
    public object? Value { get; set; }

    [JsonPropertyName("rawValue")]
    public string? RawValue { get; set; }
}

/// <summary>
/// Processed device data for frontend display
/// </summary>
public class LiveDeviceDataDto
{
    public string Imei { get; set; } = string.Empty;
    public DateTime TrackTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Speed { get; set; }
    public bool IsOnline => Status?.ToUpper() != "OFFLINE";

    // Location
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? LocationName { get; set; }
    public double? LocationProximity { get; set; }

    // All IO Data for detailed view
    public List<IoDataItemDto> IoData { get; set; } = new();
}

/// <summary>
/// IO data item for display - includes all VZone API fields
/// </summary>
public class IoDataItemDto
{
    public int? UniversalIOID { get; set; }
    public string? UniversalIOName { get; set; }
    public string? IoCode { get; set; }
    public string? IoName { get; set; }
    public object? Value { get; set; }
    public string? RawValue { get; set; }
}

