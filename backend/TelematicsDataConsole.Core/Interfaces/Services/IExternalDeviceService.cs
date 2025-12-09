using TelematicsDataConsole.Core.DTOs;
using TelematicsDataConsole.Core.DTOs.Device;

namespace TelematicsDataConsole.Core.Interfaces.Services;

public interface IExternalDeviceService
{
    Task<PagedResult<ExternalDeviceDto>> SearchDevicesAsync(DeviceSearchFilter filter);
    Task<ExternalDeviceDto?> GetByIdAsync(long deviceId);
    Task<ExternalDeviceDto?> GetByImeiAsync(string imei);
    Task<IEnumerable<ExternalDeviceDto>> GetByIdsAsync(IEnumerable<long> deviceIds);
}

