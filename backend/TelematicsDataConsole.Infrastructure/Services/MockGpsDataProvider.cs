using Microsoft.Extensions.Logging;
using TelematicsDataConsole.Core.DTOs.Imei;
using TelematicsDataConsole.Core.Interfaces.Services;

namespace TelematicsDataConsole.Infrastructure.Services;

/// <summary>
/// Real GPS data provider that uses the ExternalDeviceService to get actual device IDs.
/// </summary>
public class RealGpsDataProvider : IGpsDataProvider
{
    private readonly IExternalDeviceService _externalDeviceService;
    private readonly ILogger<RealGpsDataProvider> _logger;
    private static readonly Random _random = new();

    public RealGpsDataProvider(IExternalDeviceService externalDeviceService, ILogger<RealGpsDataProvider> logger)
    {
        _externalDeviceService = externalDeviceService;
        _logger = logger;
    }

    public async Task<int?> GetDeviceIdByImeiAsync(string imei)
    {
        if (string.IsNullOrEmpty(imei) || imei.Length < 5)
            return null;

        try
        {
            var device = await _externalDeviceService.GetByImeiAsync(imei);
            if (device == null)
            {
                _logger.LogWarning("Device not found in external database for IMEI: {Imei}", imei);
                return null;
            }

            _logger.LogDebug("Found device {DeviceId} for IMEI {Imei}", device.DeviceId, imei);
            return (int)device.DeviceId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching device ID for IMEI: {Imei}", imei);
            return null;
        }
    }

    public async Task<DeviceDataDto?> GetDeviceDataAsync(string imei)
    {
        if (string.IsNullOrEmpty(imei))
            return null;

        // Get the real device ID from the external database
        var device = await _externalDeviceService.GetByImeiAsync(imei);
        var deviceId = device?.DeviceId ?? Math.Abs(imei.GetHashCode() % 100000);

        // Generate mock GPS data but with real device ID
        var deviceData = new DeviceDataDto
        {
            DeviceId = (int)deviceId,
            Imei = imei,
            SerialNumber = $"SN-{imei[..8]}",
            DeviceModel = "GT06N",
            FirmwareVersion = "V2.1.5",
            IsOnline = _random.NextDouble() > 0.2, // 80% chance online
            LastGpsData = new GpsDataDto
            {
                Latitude = 25.2048 + (_random.NextDouble() - 0.5) * 0.1,
                Longitude = 55.2708 + (_random.NextDouble() - 0.5) * 0.1,
                Altitude = 10 + _random.NextDouble() * 50,
                Speed = _random.NextDouble() * 80,
                Heading = _random.NextDouble() * 360,
                Satellites = _random.Next(6, 14),
                SignalStrength = _random.Next(60, 100),
                IgnitionOn = _random.NextDouble() > 0.3,
                BatteryVoltage = 3.7 + _random.NextDouble() * 0.5,
                ExternalVoltage = 12 + _random.NextDouble() * 2,
                GpsTime = DateTime.UtcNow.AddMinutes(-_random.Next(0, 10)),
                ServerTime = DateTime.UtcNow
            },
            VehicleInfo = new VehicleInfoDto
            {
                VehicleId = _random.Next(1000, 9999),
                PlateNumber = $"DXB-{_random.Next(10000, 99999)}",
                VehicleName = $"Vehicle-{imei[^4..]}",
                Make = GetRandomMake(),
                Model = GetRandomModel(),
                Year = _random.Next(2018, 2024),
                Vin = GenerateVin(),
                OwnerName = "Fleet Owner"
            }
        };

        return deviceData;
    }

    private static string GetRandomMake()
    {
        var makes = new[] { "Toyota", "Nissan", "Ford", "Chevrolet", "Honda", "Mitsubishi" };
        return makes[_random.Next(makes.Length)];
    }

    private static string GetRandomModel()
    {
        var models = new[] { "Hilux", "Patrol", "F-150", "Silverado", "Civic", "Pajero" };
        return models[_random.Next(models.Length)];
    }

    private static string GenerateVin()
    {
        const string chars = "ABCDEFGHJKLMNPRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, 17).Select(s => s[_random.Next(s.Length)]).ToArray());
    }
}

