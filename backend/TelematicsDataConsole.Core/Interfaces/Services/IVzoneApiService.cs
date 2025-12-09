using TelematicsDataConsole.Core.DTOs.Vzone;

namespace TelematicsDataConsole.Core.Interfaces.Services;

public interface IVzoneApiService
{
    Task<LiveDeviceDataDto?> GetLiveDeviceDataAsync(string imei);
}

