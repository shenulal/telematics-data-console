using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TelematicsDataConsole.Core.DTOs;
using TelematicsDataConsole.Core.DTOs.Device;
using TelematicsDataConsole.Core.Interfaces.Services;

namespace TelematicsDataConsole.Infrastructure.Services;

public class ExternalDeviceService : IExternalDeviceService
{
    private readonly string _connectionString;
    private readonly ILogger<ExternalDeviceService> _logger;

    public ExternalDeviceService(IConfiguration configuration, ILogger<ExternalDeviceService> logger)
    {
        _connectionString = configuration.GetConnectionString("ExternalDeviceDb")
            ?? throw new InvalidOperationException("ExternalDeviceDb connection string not configured");
        _logger = logger;
    }

    public async Task<PagedResult<ExternalDeviceDto>> SearchDevicesAsync(DeviceSearchFilter filter)
    {
        var devices = new List<ExternalDeviceDto>();
        var totalCount = 0;

        try
        {
            _logger.LogInformation("Connecting to external device database...");
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            _logger.LogInformation("Connected successfully to external device database");

            // Get total count - search by IMEI only
            var countQuery = "SELECT COUNT(*) FROM Device WHERE IsActive = 1";
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                countQuery += " AND DeviceIMEI LIKE @Search";
            }

            using (var countCmd = new SqlCommand(countQuery, connection))
            {
                if (!string.IsNullOrWhiteSpace(filter.Search))
                {
                    countCmd.Parameters.AddWithValue("@Search", $"%{filter.Search}%");
                }
                totalCount = (int)await countCmd.ExecuteScalarAsync();
                _logger.LogInformation("Found {Count} devices matching search", totalCount);
            }

            // Get paginated data - search by IMEI only
            var query = @"
                SELECT ID as DeviceId, DeviceIMEI as IMEI, TimeZone, SIMNO as Sim, CountryCode, TypeId,
                       CASE AccessKey WHEN 1 THEN 'VZoneTrack' WHEN 2 THEN 'Fleetoxia' END as Server
                FROM Device
                WHERE IsActive = 1";

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                query += " AND DeviceIMEI LIKE @Search";
            }

            query += " ORDER BY ID OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            using var cmd = new SqlCommand(query, connection);
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                cmd.Parameters.AddWithValue("@Search", $"%{filter.Search}%");
            }
            cmd.Parameters.AddWithValue("@Offset", (filter.Page - 1) * filter.PageSize);
            cmd.Parameters.AddWithValue("@PageSize", filter.PageSize);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                devices.Add(MapToDto(reader));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching external devices: {Message}", ex.Message);
            throw;
        }

        return new PagedResult<ExternalDeviceDto>
        {
            Items = devices,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<ExternalDeviceDto?> GetByIdAsync(long deviceId)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = @"
            SELECT ID as DeviceId, DeviceIMEI as IMEI, TimeZone, SIMNO as Sim, CountryCode, TypeId,
                   CASE AccessKey WHEN 1 THEN 'VZoneTrack' WHEN 2 THEN 'Fleetoxia' END as Server
            FROM Device WHERE ID = @DeviceId AND IsActive = 1";

        using var cmd = new SqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@DeviceId", deviceId);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapToDto(reader);
        }
        return null;
    }

    public async Task<ExternalDeviceDto?> GetByImeiAsync(string imei)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = @"
            SELECT ID as DeviceId, DeviceIMEI as IMEI, TimeZone, SIMNO as Sim, CountryCode, TypeId,
                   CASE AccessKey WHEN 1 THEN 'VZoneTrack' WHEN 2 THEN 'Fleetoxia' END as Server
            FROM Device WHERE DeviceIMEI = @Imei AND IsActive = 1";

        using var cmd = new SqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@Imei", imei);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapToDto(reader);
        }
        return null;
    }

    public async Task<IEnumerable<ExternalDeviceDto>> GetByIdsAsync(IEnumerable<long> deviceIds)
    {
        var devices = new List<ExternalDeviceDto>();
        var ids = deviceIds.ToList();
        if (!ids.Any()) return devices;

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = $@"
            SELECT ID as DeviceId, DeviceIMEI as IMEI, TimeZone, SIMNO as Sim, CountryCode, TypeId,
                   CASE AccessKey WHEN 1 THEN 'VZoneTrack' WHEN 2 THEN 'Fleetoxia' END as Server
            FROM Device WHERE ID IN ({string.Join(",", ids)}) AND IsActive = 1";

        using var cmd = new SqlCommand(query, connection);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            devices.Add(MapToDto(reader));
        }
        return devices;
    }

    private static ExternalDeviceDto MapToDto(SqlDataReader reader) => new()
    {
        DeviceId = reader.GetInt64(reader.GetOrdinal("DeviceId")),
        Imei = reader.GetString(reader.GetOrdinal("IMEI")),
        TimeZone = reader.IsDBNull(reader.GetOrdinal("TimeZone")) ? null : reader["TimeZone"]?.ToString(),
        Sim = reader.IsDBNull(reader.GetOrdinal("Sim")) ? null : reader["Sim"]?.ToString(),
        CountryCode = reader.IsDBNull(reader.GetOrdinal("CountryCode")) ? null : reader["CountryCode"]?.ToString(),
        TypeId = reader.IsDBNull(reader.GetOrdinal("TypeId")) ? null : reader.GetInt32(reader.GetOrdinal("TypeId")),
        Server = reader.IsDBNull(reader.GetOrdinal("Server")) ? null : reader["Server"]?.ToString()
    };
}

