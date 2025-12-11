using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TelematicsDataConsole.Core.DTOs.Imei;
using TelematicsDataConsole.Core.Entities;
using TelematicsDataConsole.Core.Interfaces.Services;
using TelematicsDataConsole.Infrastructure.Data;

namespace TelematicsDataConsole.Infrastructure.Services;

public class ImeiService : IImeiService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IGpsDataProvider _gpsDataProvider;
    private readonly ILogger<ImeiService> _logger;

    public ImeiService(ApplicationDbContext context, IAuditService auditService, IGpsDataProvider gpsDataProvider, ILogger<ImeiService> logger)
    {
        _context = context;
        _auditService = auditService;
        _gpsDataProvider = gpsDataProvider;
        _logger = logger;
    }

    public async Task<ImeiAccessResult> CheckAccessAsync(int technicianId, string imei)
    {
        var technician = await _context.Technicians
            .Include(t => t.ImeiRestrictions)
                .ThenInclude(r => r.Tag)
                    .ThenInclude(t => t!.TagItems)
            .FirstOrDefaultAsync(t => t.TechnicianId == technicianId);

        if (technician == null)
        {
            return new ImeiAccessResult { HasAccess = false, Message = "Technician not found" };
        }

        if (technician.Status != (short)TechnicianStatus.Active)
        {
            return new ImeiAccessResult { HasAccess = false, Message = "Technician account is not active" };
        }

        // Get device ID from IMEI (this would come from your GPS data source)
        var deviceId = await _gpsDataProvider.GetDeviceIdByImeiAsync(imei);
        if (deviceId == null)
        {
            return new ImeiAccessResult { HasAccess = false, Message = "Device not found" };
        }

        // Check daily limit
        var todayVerifications = await _context.VerificationLogs
            .CountAsync(v => v.TechnicianId == technicianId && v.VerifiedAt.Date == DateTime.UtcNow.Date);

        if (technician.DailyLimit > 0 && todayVerifications >= technician.DailyLimit)
        {
            return new ImeiAccessResult { HasAccess = false, Message = "Daily verification limit reached" };
        }

        // Check IMEI restrictions based on mode
        var hasAccess = await CheckImeiRestrictions(technician, deviceId.Value);

        if (!hasAccess)
        {
            await _auditService.LogAsync(technician.UserId, AuditActions.ImeiAccessDenied, "Device", imei);
            return new ImeiAccessResult
            {
                HasAccess = false,
                Message = "Restricted Access – You are not authorized to view data for this IMEI.",
                RestrictionReason = "IMEI is restricted for this technician"
            };
        }

        return new ImeiAccessResult { HasAccess = true, DeviceId = deviceId };
    }

    private Task<bool> CheckImeiRestrictions(Technician technician, int deviceId)
    {
        _logger.LogInformation("CheckImeiRestrictions: TechnicianId={TechnicianId}, DeviceId={DeviceId}",
            technician.TechnicianId, deviceId);

        var now = DateTime.UtcNow;
        var activeRestrictions = technician.ImeiRestrictions
            .Where(r => r.Status == (int)RestrictionStatus.Active &&
                       (r.IsPermanent == true || (r.ValidFrom <= now && r.ValidUntil >= now)))
            .ToList();

        _logger.LogInformation("Found {Count} active restrictions for technician {TechnicianId}",
            activeRestrictions.Count, technician.TechnicianId);

        if (!activeRestrictions.Any())
        {
            // No restrictions - allow access to any IMEI
            _logger.LogInformation("No active restrictions - allowing access");
            return Task.FromResult(true);
        }

        // Determine restriction mode based on the types of restrictions present
        // If technician has ANY Allow restrictions, they operate in AllowList mode (deny by default)
        // If technician has ONLY Deny restrictions, they operate in DenyList mode (allow by default)
        bool hasAllowRestrictions = activeRestrictions.Any(r => r.AccessType == (short)AccessType.Allow);
        bool hasDenyRestrictions = activeRestrictions.Any(r => r.AccessType == (short)AccessType.Deny);

        _logger.LogInformation("Restriction mode: hasAllowRestrictions={HasAllow}, hasDenyRestrictions={HasDeny}",
            hasAllowRestrictions, hasDenyRestrictions);

        // Check direct device restrictions first (highest priority)
        var directRestriction = activeRestrictions.FirstOrDefault(r => r.DeviceId == deviceId);
        if (directRestriction != null)
        {
            var result = directRestriction.AccessType == (short)AccessType.Allow;
            _logger.LogInformation("Direct device restriction found: AccessType={AccessType}, Result={Result}",
                directRestriction.AccessType, result);
            return Task.FromResult(result);
        }

        // Check tag-based restrictions
        foreach (var restriction in activeRestrictions.Where(r => r.TagId != null))
        {
            _logger.LogInformation("Checking tag-based restriction: RestrictionId={RestrictionId}, TagId={TagId}, AccessType={AccessType}",
                restriction.RestrictionId, restriction.TagId, restriction.AccessType);

            // Get device IDs from tag items (EntityType 1 = Device)
            var tagItems = restriction.Tag?.TagItems
                .Where(ti => ti.EntityType == (short)TagEntityType.Device)
                .ToList() ?? new List<TagItem>();

            _logger.LogInformation("Tag {TagId} has {Count} device items", restriction.TagId, tagItems.Count);

            foreach (var ti in tagItems)
            {
                _logger.LogDebug("TagItem: EntityId={EntityId}, EntityIdentifier={EntityIdentifier}",
                    ti.EntityId, ti.EntityIdentifier);
            }

            var tagDevices = tagItems.Select(ti => ti.EntityId).ToList();

            // Cast deviceId to long for proper comparison
            if (tagDevices.Contains((long)deviceId))
            {
                var result = restriction.AccessType == (short)AccessType.Allow;
                _logger.LogInformation("Device {DeviceId} found in tag {TagId}: AccessType={AccessType}, Result={Result}",
                    deviceId, restriction.TagId, restriction.AccessType, result);
                return Task.FromResult(result);
            }
        }

        // Device not found in any restriction - apply default based on derived mode
        // If has Allow restrictions (allow-list mode): deny by default (device not in allow list)
        // If only Deny restrictions (deny-list mode): allow by default (device not in deny list)
        if (hasAllowRestrictions)
        {
            _logger.LogInformation("Device {DeviceId} not found in any allow list - denying access", deviceId);
            return Task.FromResult(false); // Allow-list mode: deny devices not in the list
        }
        else if (hasDenyRestrictions)
        {
            _logger.LogInformation("Device {DeviceId} not found in any deny list - allowing access", deviceId);
            return Task.FromResult(true); // Deny-list mode: allow devices not in the list
        }

        // Fallback: allow
        _logger.LogInformation("Fallback: allowing access for device {DeviceId}", deviceId);
        return Task.FromResult(true);
    }

    public async Task<ImeiDataResult> GetDeviceDataAsync(int technicianId, string imei)
    {
        var accessResult = await CheckAccessAsync(technicianId, imei);
        if (!accessResult.HasAccess)
        {
            return new ImeiDataResult { Success = false, Message = accessResult.Message };
        }

        var deviceData = await _gpsDataProvider.GetDeviceDataAsync(imei);
        if (deviceData == null)
        {
            return new ImeiDataResult { Success = false, Message = "Unable to fetch device data" };
        }

        await _auditService.LogAsync(null, AuditActions.ImeiAccess, "Device", imei);

        return new ImeiDataResult { Success = true, Data = deviceData };
    }

    public async Task<VerificationResult> VerifyDeviceAsync(int technicianId, VerificationRequest request)
    {
        var accessResult = await CheckAccessAsync(technicianId, request.Imei);
        if (!accessResult.HasAccess)
        {
            return new VerificationResult { Success = false, Message = accessResult.Message };
        }

        var deviceId = accessResult.DeviceId ?? 0;
        var timeGapThreshold = DateTime.UtcNow.AddHours(-VerificationLog.TIME_GAP_HOURS);

        // Check if there's a recent verification for same technician and device
        var existingLog = await _context.VerificationLogs
            .Where(v => v.TechnicianId == technicianId
                     && v.DeviceId == deviceId
                     && v.VerifiedAt >= timeGapThreshold)
            .OrderByDescending(v => v.VerifiedAt)
            .FirstOrDefaultAsync();

        if (existingLog != null)
        {
            // Update existing log with new data
            existingLog.VerificationStatus = request.VerificationStatus;
            existingLog.Notes = request.Notes;
            if (request.GpsData != null)
            {
                existingLog.Latitude = request.GpsData.Latitude;
                existingLog.Longitude = request.GpsData.Longitude;
                existingLog.GpsTime = request.GpsData.GpsTime;
            }
            await _context.SaveChangesAsync();
            return new VerificationResult { Success = true, VerificationId = existingLog.VerificationId };
        }

        var log = new VerificationLog
        {
            TechnicianId = technicianId,
            DeviceId = deviceId,
            Imei = request.Imei,
            VerificationStatus = request.VerificationStatus,
            Notes = request.Notes,
            Latitude = request.GpsData?.Latitude,
            Longitude = request.GpsData?.Longitude,
            GpsTime = request.GpsData?.GpsTime,
            VerifiedAt = DateTime.UtcNow
        };

        await _context.VerificationLogs.AddAsync(log);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(null, AuditActions.ImeiVerification, "VerificationLog", log.VerificationId.ToString());

        return new VerificationResult { Success = true, VerificationId = log.VerificationId };
    }

    public async Task<VerificationHistoryPagedResult> GetVerificationHistoryAsync(int technicianId, VerificationHistoryFilterDto filter)
    {
        // Default to current day if no dates provided
        var fromDate = filter.FromDate ?? DateTime.UtcNow.Date;
        var toDate = filter.ToDate ?? DateTime.UtcNow.Date.AddDays(1).AddSeconds(-1);

        var query = _context.VerificationLogs
            .Where(v => v.TechnicianId == technicianId && v.VerifiedAt >= fromDate && v.VerifiedAt <= toDate);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(v => v.VerifiedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(v => new VerificationHistoryDto
            {
                VerificationId = v.VerificationId,
                DeviceId = v.DeviceId,
                Imei = v.Imei,
                VerificationStatus = v.VerificationStatus,
                Notes = v.Notes,
                Latitude = v.Latitude,
                Longitude = v.Longitude,
                GpsTime = v.GpsTime,
                VerifiedAt = v.VerifiedAt
            }).ToListAsync();

        return new VerificationHistoryPagedResult
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    /// <summary>
    /// Check access for Admin users (Super Admin, Reseller Admin, Supervisor)
    /// Super Admin: Can access any IMEI
    /// Reseller Admin/Supervisor: Can access cumulative set of IMEIs from their technicians' restrictions
    ///                            If no restrictions exist, can access any IMEI
    /// </summary>
    public async Task<ImeiAccessResult> CheckAdminAccessAsync(int userId, int? resellerId, string imei)
    {
        // Get device ID from IMEI
        var deviceId = await _gpsDataProvider.GetDeviceIdByImeiAsync(imei);
        if (deviceId == null)
        {
            return new ImeiAccessResult { HasAccess = false, Message = "Device not found" };
        }

        // If no reseller ID (Super Admin), allow access to any IMEI
        if (!resellerId.HasValue)
        {
            return new ImeiAccessResult { HasAccess = true, DeviceId = deviceId };
        }

        // For Reseller Admin/Supervisor, check cumulative IMEI restrictions from their technicians
        var hasAccess = await CheckCumulativeResellerAccess(resellerId.Value, deviceId.Value);

        if (!hasAccess)
        {
            await _auditService.LogAsync(userId, AuditActions.ImeiAccessDenied, "Device", imei);
            return new ImeiAccessResult
            {
                HasAccess = false,
                Message = "Restricted Access – This IMEI is not in your technicians' allowed list.",
                RestrictionReason = "IMEI not found in any technician's allowed restrictions"
            };
        }

        return new ImeiAccessResult { HasAccess = true, DeviceId = deviceId };
    }

    /// <summary>
    /// Check if a device is accessible based on cumulative restrictions of all technicians under a reseller
    /// Logic:
    /// - If any technician has no restrictions, allow any IMEI
    /// - Otherwise, collect all allowed devices from all technicians and check if the device is in the list
    /// </summary>
    private async Task<bool> CheckCumulativeResellerAccess(int resellerId, int deviceId)
    {
        var now = DateTime.UtcNow;

        // Get all technicians under this reseller
        var technicians = await _context.Technicians
            .Include(t => t.ImeiRestrictions)
                .ThenInclude(r => r.Tag)
                    .ThenInclude(t => t!.TagItems)
            .Where(t => t.ResellerId == resellerId && t.Status == (short)TechnicianStatus.Active)
            .ToListAsync();

        if (!technicians.Any())
        {
            // No technicians under this reseller - allow access
            return true;
        }

        // Collect all allowed device IDs and denied device IDs from all technicians' restrictions
        var allAllowedDevices = new HashSet<int>();
        var allDeniedDevices = new HashSet<int>();
        bool hasAnyRestrictions = false;
        bool hasAnyAllowRestrictions = false;

        foreach (var technician in technicians)
        {
            var activeRestrictions = technician.ImeiRestrictions
                .Where(r => r.Status == (int)RestrictionStatus.Active &&
                           (r.IsPermanent == true || (r.ValidFrom <= now && r.ValidUntil >= now)))
                .ToList();

            if (!activeRestrictions.Any())
            {
                // This technician has no restrictions - they can access any IMEI
                // So the admin can also access any IMEI
                return true;
            }

            hasAnyRestrictions = true;

            // Collect devices from this technician's restrictions
            foreach (var restriction in activeRestrictions)
            {
                var isAllow = restriction.AccessType == (short)AccessType.Allow;
                if (isAllow) hasAnyAllowRestrictions = true;

                // Direct device restriction
                if (restriction.DeviceId.HasValue)
                {
                    if (isAllow)
                        allAllowedDevices.Add(restriction.DeviceId.Value);
                    else
                        allDeniedDevices.Add(restriction.DeviceId.Value);
                }

                // Tag-based restriction
                if (restriction.TagId != null && restriction.Tag != null)
                {
                    var tagDevices = restriction.Tag.TagItems
                        .Where(ti => ti.EntityType == (short)TagEntityType.Device)
                        .Select(ti => (int)ti.EntityId);
                    foreach (var device in tagDevices)
                    {
                        if (isAllow)
                            allAllowedDevices.Add(device);
                        else
                            allDeniedDevices.Add(device);
                    }
                }
            }
        }

        // If no active restrictions at all, allow access
        if (!hasAnyRestrictions)
        {
            return true;
        }

        // If there are Allow restrictions, device must be in the allowed list
        if (hasAnyAllowRestrictions)
        {
            return allAllowedDevices.Contains(deviceId);
        }

        // If only Deny restrictions, device must NOT be in the denied list
        return !allDeniedDevices.Contains(deviceId);
    }

    public async Task<ImeiDataResult> GetDeviceDataForAdminAsync(int userId, int? resellerId, string imei)
    {
        var accessResult = await CheckAdminAccessAsync(userId, resellerId, imei);
        if (!accessResult.HasAccess)
        {
            return new ImeiDataResult { Success = false, Message = accessResult.Message };
        }

        var deviceData = await _gpsDataProvider.GetDeviceDataAsync(imei);
        if (deviceData == null)
        {
            return new ImeiDataResult { Success = false, Message = "Unable to fetch device data" };
        }

        await _auditService.LogAsync(userId, AuditActions.ImeiAccess, "Device", imei);

        return new ImeiDataResult { Success = true, Data = deviceData };
    }

    public async Task<VerificationResult> VerifyDeviceForAdminAsync(int userId, int? resellerId, VerificationRequest request)
    {
        var accessResult = await CheckAdminAccessAsync(userId, resellerId, request.Imei);
        if (!accessResult.HasAccess)
        {
            return new VerificationResult { Success = false, Message = accessResult.Message };
        }

        var deviceId = accessResult.DeviceId ?? 0;
        var timeGapThreshold = DateTime.UtcNow.AddHours(-VerificationLog.TIME_GAP_HOURS);

        // For admin verifications, we use userId as a pseudo-TechnicianId (stored as negative to distinguish)
        // Or we can create a separate log without TechnicianId
        // For now, we'll log it with a special indicator - using 0 as TechnicianId for admin verifications

        // Check if there's a recent verification for same device by this admin user
        var existingLog = await _context.VerificationLogs
            .Where(v => v.DeviceId == deviceId && v.VerifiedAt >= timeGapThreshold)
            .OrderByDescending(v => v.VerifiedAt)
            .FirstOrDefaultAsync();

        if (existingLog != null)
        {
            // Update existing log with new data
            existingLog.VerificationStatus = request.VerificationStatus;
            existingLog.Notes = request.Notes;
            if (request.GpsData != null)
            {
                existingLog.Latitude = request.GpsData.Latitude;
                existingLog.Longitude = request.GpsData.Longitude;
                existingLog.GpsTime = request.GpsData.GpsTime;
            }
            await _context.SaveChangesAsync();
            return new VerificationResult { Success = true, VerificationId = existingLog.VerificationId };
        }

        // For admin verifications, we need to handle the case where TechnicianId is required
        // We'll create a verification log with TechnicianId = 0 to indicate admin verification
        // Alternatively, we could make TechnicianId nullable - for now we'll use a workaround
        var log = new VerificationLog
        {
            TechnicianId = 0, // 0 indicates admin verification
            DeviceId = deviceId,
            Imei = request.Imei,
            VerificationStatus = request.VerificationStatus,
            Notes = request.Notes,
            Latitude = request.GpsData?.Latitude,
            Longitude = request.GpsData?.Longitude,
            GpsTime = request.GpsData?.GpsTime,
            VerifiedAt = DateTime.UtcNow
        };

        await _context.VerificationLogs.AddAsync(log);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(userId, AuditActions.ImeiVerification, "VerificationLog", log.VerificationId.ToString());

        return new VerificationResult { Success = true, VerificationId = log.VerificationId };
    }
}

// Interface for GPS data provider (to be implemented based on your GPS data source)
public interface IGpsDataProvider
{
    Task<int?> GetDeviceIdByImeiAsync(string imei);
    Task<DeviceDataDto?> GetDeviceDataAsync(string imei);
}

