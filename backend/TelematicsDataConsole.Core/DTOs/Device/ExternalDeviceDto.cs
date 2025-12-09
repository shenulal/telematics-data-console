namespace TelematicsDataConsole.Core.DTOs.Device;

public class ExternalDeviceDto
{
    public long DeviceId { get; set; }
    public string Imei { get; set; } = string.Empty;
    public string? TimeZone { get; set; }
    public string? Sim { get; set; }
    public string? CountryCode { get; set; }
    public int? TypeId { get; set; }
    public string? Server { get; set; }
}

public class DeviceSearchFilter
{
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

