using Microsoft.EntityFrameworkCore;
using TelematicsDataConsole.Core.DTOs;
using TelematicsDataConsole.Core.DTOs.VerificationLog;
using TelematicsDataConsole.Core.Entities;
using TelematicsDataConsole.Core.Interfaces.Services;
using TelematicsDataConsole.Infrastructure.Data;

namespace TelematicsDataConsole.Infrastructure.Services;

public class VerificationLogService : IVerificationLogService
{
    private readonly ApplicationDbContext _context;

    public VerificationLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<VerificationLogDto>> GetAllAsync(VerificationLogFilterDto filter)
    {
        var query = _context.VerificationLogs
            .Include(v => v.Technician).ThenInclude(t => t.User)
            .Include(v => v.Technician).ThenInclude(t => t.Reseller)
            .AsQueryable();

        if (filter.TechnicianId.HasValue)
            query = query.Where(v => v.TechnicianId == filter.TechnicianId.Value);

        if (filter.DeviceId.HasValue)
            query = query.Where(v => v.DeviceId == filter.DeviceId.Value);

        if (filter.ResellerId.HasValue)
            query = query.Where(v => v.Technician.ResellerId == filter.ResellerId.Value);

        if (!string.IsNullOrWhiteSpace(filter.TechnicianName))
            query = query.Where(v =>
                (v.Technician.User.FullName != null && v.Technician.User.FullName.Contains(filter.TechnicianName)) ||
                v.Technician.User.Username.Contains(filter.TechnicianName) ||
                (v.Technician.EmployeeCode != null && v.Technician.EmployeeCode.Contains(filter.TechnicianName)));

        if (!string.IsNullOrWhiteSpace(filter.Imei))
            query = query.Where(v => v.Imei != null && v.Imei.Contains(filter.Imei));

        if (filter.FromDate.HasValue)
            query = query.Where(v => v.VerifiedAt >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(v => v.VerifiedAt <= filter.ToDate.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(v => v.VerifiedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(v => new VerificationLogDto
            {
                VerificationId = v.VerificationId,
                TechnicianId = v.TechnicianId,
                TechnicianName = v.Technician.User.FullName ?? v.Technician.User.Username,
                TechnicianEmployeeCode = v.Technician.EmployeeCode,
                ResellerId = v.Technician.ResellerId,
                ResellerName = v.Technician.Reseller != null ? v.Technician.Reseller.CompanyName : null,
                DeviceId = v.DeviceId,
                Imei = v.Imei,
                VerificationStatus = v.VerificationStatus,
                Notes = v.Notes,
                Latitude = v.Latitude,
                Longitude = v.Longitude,
                GpsTime = v.GpsTime,
                VerifiedAt = v.VerifiedAt
            })
            .ToListAsync();

        return new PagedResult<VerificationLogDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<VerificationLogDto?> GetByIdAsync(int id)
    {
        return await _context.VerificationLogs
            .Include(v => v.Technician).ThenInclude(t => t.User)
            .Include(v => v.Technician).ThenInclude(t => t.Reseller)
            .Where(v => v.VerificationId == id)
            .Select(v => new VerificationLogDto
            {
                VerificationId = v.VerificationId,
                TechnicianId = v.TechnicianId,
                TechnicianName = v.Technician.User.FullName ?? v.Technician.User.Username,
                TechnicianEmployeeCode = v.Technician.EmployeeCode,
                ResellerId = v.Technician.ResellerId,
                ResellerName = v.Technician.Reseller != null ? v.Technician.Reseller.CompanyName : null,
                DeviceId = v.DeviceId,
                Imei = v.Imei,
                VerificationStatus = v.VerificationStatus,
                Notes = v.Notes,
                Latitude = v.Latitude,
                Longitude = v.Longitude,
                GpsTime = v.GpsTime,
                VerifiedAt = v.VerifiedAt
            })
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Creates a verification log entry with time gap check.
    /// If same technician verified same device within TIME_GAP_HOURS, returns existing entry.
    /// </summary>
    public async Task<VerificationLogDto> CreateAsync(CreateVerificationLogDto dto)
    {
        var timeGapThreshold = DateTime.UtcNow.AddHours(-VerificationLog.TIME_GAP_HOURS);

        // Check if there's a recent verification for same technician and device
        var existingLog = await _context.VerificationLogs
            .Where(v => v.TechnicianId == dto.TechnicianId
                     && v.DeviceId == dto.DeviceId
                     && v.VerifiedAt >= timeGapThreshold)
            .OrderByDescending(v => v.VerifiedAt)
            .FirstOrDefaultAsync();

        if (existingLog != null)
        {
            // Return existing log without creating a new one
            return (await GetByIdAsync(existingLog.VerificationId))!;
        }

        var log = new VerificationLog
        {
            TechnicianId = dto.TechnicianId,
            DeviceId = dto.DeviceId,
            VerifiedAt = DateTime.UtcNow
        };

        await _context.VerificationLogs.AddAsync(log);
        await _context.SaveChangesAsync();

        return (await GetByIdAsync(log.VerificationId))!;
    }

    public async Task<List<VerificationLogDto>> GetByTechnicianIdAsync(int technicianId, int limit = 50)
    {
        return await _context.VerificationLogs
            .Include(v => v.Technician).ThenInclude(t => t.User)
            .Include(v => v.Technician).ThenInclude(t => t.Reseller)
            .Where(v => v.TechnicianId == technicianId)
            .OrderByDescending(v => v.VerifiedAt)
            .Take(limit)
            .Select(v => new VerificationLogDto
            {
                VerificationId = v.VerificationId,
                TechnicianId = v.TechnicianId,
                TechnicianName = v.Technician.User.FullName ?? v.Technician.User.Username,
                TechnicianEmployeeCode = v.Technician.EmployeeCode,
                ResellerId = v.Technician.ResellerId,
                ResellerName = v.Technician.Reseller != null ? v.Technician.Reseller.CompanyName : null,
                DeviceId = v.DeviceId,
                Imei = v.Imei,
                VerificationStatus = v.VerificationStatus,
                Notes = v.Notes,
                Latitude = v.Latitude,
                Longitude = v.Longitude,
                GpsTime = v.GpsTime,
                VerifiedAt = v.VerifiedAt
            })
            .ToListAsync();
    }

    public async Task<List<VerificationLogDto>> GetByDeviceIdAsync(int deviceId, int limit = 50)
    {
        return await _context.VerificationLogs
            .Include(v => v.Technician).ThenInclude(t => t.User)
            .Include(v => v.Technician).ThenInclude(t => t.Reseller)
            .Where(v => v.DeviceId == deviceId)
            .OrderByDescending(v => v.VerifiedAt)
            .Take(limit)
            .Select(v => new VerificationLogDto
            {
                VerificationId = v.VerificationId,
                TechnicianId = v.TechnicianId,
                TechnicianName = v.Technician.User.FullName ?? v.Technician.User.Username,
                TechnicianEmployeeCode = v.Technician.EmployeeCode,
                ResellerId = v.Technician.ResellerId,
                ResellerName = v.Technician.Reseller != null ? v.Technician.Reseller.CompanyName : null,
                DeviceId = v.DeviceId,
                Imei = v.Imei,
                VerificationStatus = v.VerificationStatus,
                Notes = v.Notes,
                Latitude = v.Latitude,
                Longitude = v.Longitude,
                GpsTime = v.GpsTime,
                VerifiedAt = v.VerifiedAt
            })
            .ToListAsync();
    }

    public async Task<VerificationStatisticsDto> GetStatisticsAsync(int? technicianId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.VerificationLogs.AsQueryable();

        if (technicianId.HasValue)
            query = query.Where(v => v.TechnicianId == technicianId.Value);

        if (fromDate.HasValue)
            query = query.Where(v => v.VerifiedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(v => v.VerifiedAt <= toDate.Value);

        var logs = await query.ToListAsync();
        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        return new VerificationStatisticsDto
        {
            TotalVerifications = logs.Count,
            UniqueDevices = logs.Select(l => l.DeviceId).Distinct().Count(),
            VerificationsToday = logs.Count(l => l.VerifiedAt >= today),
            VerificationsThisWeek = logs.Count(l => l.VerifiedAt >= weekStart),
            VerificationsThisMonth = logs.Count(l => l.VerifiedAt >= monthStart),
            LastVerificationAt = logs.OrderByDescending(l => l.VerifiedAt).FirstOrDefault()?.VerifiedAt
        };
    }
}

