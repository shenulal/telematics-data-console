using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TelematicsDataConsole.Core.DTOs.Vzone;
using TelematicsDataConsole.Core.Interfaces.Services;

namespace TelematicsDataConsole.Infrastructure.Services;

public class VzoneApiService : IVzoneApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<VzoneApiService> _logger;
    private readonly string _baseUrl;

    public VzoneApiService(HttpClient httpClient, IConfiguration configuration, ILogger<VzoneApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["VzoneApi:BaseUrl"] ?? "https://www.vzoneinternational.ae/Vzone.Technician.API";
    }

    public async Task<LiveDeviceDataDto?> GetLiveDeviceDataAsync(string imei)
    {
        try
        {
            var url = $"{_baseUrl}/IMEI/{imei}";
            _logger.LogInformation("Fetching live data from Vzone API: {Url}", url);

            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Vzone API returned {StatusCode} for IMEI {Imei}", response.StatusCode, imei);
                return null;
            }

            var vzoneData = await response.Content.ReadFromJsonAsync<VzoneDeviceDataDto>();
            if (vzoneData == null)
            {
                _logger.LogWarning("Failed to deserialize Vzone API response for IMEI {Imei}", imei);
                return null;
            }

            return MapToLiveDeviceData(vzoneData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching live data from Vzone API for IMEI {Imei}", imei);
            return null;
        }
    }

    private LiveDeviceDataDto MapToLiveDeviceData(VzoneDeviceDataDto vzoneData)
    {
        var result = new LiveDeviceDataDto
        {
            Imei = vzoneData.Imei,
            TrackTime = vzoneData.TrackTime,
            Status = vzoneData.Status,
            Speed = vzoneData.Speed,
            Latitude = vzoneData.Location?.Latitude,
            Longitude = vzoneData.Location?.Longitude,
            LocationName = vzoneData.Location?.LocationName,
            LocationProximity = vzoneData.Location?.LocationProximity
        };

        // Parse IO data - pass through all VZone API fields
        foreach (var io in vzoneData.Data)
        {
            result.IoData.Add(new IoDataItemDto
            {
                UniversalIOID = io.UniversalIOID,
                UniversalIOName = io.UniversalIOName,
                IoCode = io.IoCode,
                IoName = io.IoName,
                Value = io.Value,
                RawValue = io.RawValue
            });
        }

        return result;
    }
}

