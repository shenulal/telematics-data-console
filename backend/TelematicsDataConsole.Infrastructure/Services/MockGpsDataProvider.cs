using TelematicsDataConsole.Core.DTOs.Imei;

namespace TelematicsDataConsole.Infrastructure.Services;

/// <summary>
/// Mock GPS data provider for development and testing.
/// Replace with actual GPS data source integration in production.
/// </summary>
public class MockGpsDataProvider : IGpsDataProvider
{
    private static readonly Random _random = new();

    public Task<int?> GetDeviceIdByImeiAsync(string imei)
    {
        // In production, this would query your GPS tracking device database
        // For demo, we'll generate a consistent device ID from the IMEI
        if (string.IsNullOrEmpty(imei) || imei.Length < 5)
            return Task.FromResult<int?>(null);

        var deviceId = Math.Abs(imei.GetHashCode() % 100000);
        return Task.FromResult<int?>(deviceId);
    }

    public Task<DeviceDataDto?> GetDeviceDataAsync(string imei)
    {
        if (string.IsNullOrEmpty(imei))
            return Task.FromResult<DeviceDataDto?>(null);

        // Generate mock GPS data
        var deviceData = new DeviceDataDto
        {
            DeviceId = Math.Abs(imei.GetHashCode() % 100000),
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

        return Task.FromResult<DeviceDataDto?>(deviceData);
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

