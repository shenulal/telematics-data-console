using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TelematicsDataConsole.Core.DTOs;
using TelematicsDataConsole.Core.DTOs.Audit;
using TelematicsDataConsole.Core.Entities;
using TelematicsDataConsole.Core.Interfaces.Services;
using TelematicsDataConsole.Infrastructure.Data;

namespace TelematicsDataConsole.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;

    public AuditService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(AuditLogDto log)
    {
        var entity = new AuditLog
        {
            UserId = log.UserId,
            Username = log.Username,
            Action = log.Action,
            EntityType = log.EntityType,
            EntityId = log.EntityId,
            OldValues = log.OldValues,
            NewValues = log.NewValues,
            IpAddress = log.IpAddress,
            UserAgent = log.UserAgent,
            CreatedAt = DateTime.UtcNow
        };

        await _context.AuditLogs.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task LogAsync(int? userId, string action, string entityType, string? entityId = null,
        object? oldValues = null, object? newValues = null)
    {
        string? username = null;
        if (userId.HasValue)
        {
            var user = await _context.Users.FindAsync(userId.Value);
            username = user?.Username;
        }

        var entity = new AuditLog
        {
            UserId = userId,
            Username = username,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
            NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
            CreatedAt = DateTime.UtcNow
        };

        await _context.AuditLogs.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<PagedResult<AuditLogDto>> GetLogsAsync(AuditFilterDto filter)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (filter.UserId.HasValue)
            query = query.Where(a => a.UserId == filter.UserId);

        if (!string.IsNullOrEmpty(filter.Username))
            query = query.Where(a => a.Username != null && a.Username.Contains(filter.Username));

        if (!string.IsNullOrEmpty(filter.Action))
            query = query.Where(a => a.Action == filter.Action);

        if (!string.IsNullOrEmpty(filter.EntityType))
            query = query.Where(a => a.EntityType == filter.EntityType);

        if (filter.FromDate.HasValue)
            query = query.Where(a => a.CreatedAt >= filter.FromDate);

        if (filter.ToDate.HasValue)
        {
            // Add 1 day to include the entire end date
            var toDateEnd = filter.ToDate.Value.Date.AddDays(1);
            query = query.Where(a => a.CreatedAt < toDateEnd);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(a => new AuditLogDto
            {
                AuditId = a.AuditId,
                UserId = a.UserId,
                Username = a.Username,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                OldValues = a.OldValues,
                NewValues = a.NewValues,
                IpAddress = a.IpAddress,
                UserAgent = a.UserAgent,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<AuditLogDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<IEnumerable<AuditLogDto>> GetUserActivityAsync(int userId, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.AuditLogs.Where(a => a.UserId == userId);

        if (from.HasValue) query = query.Where(a => a.CreatedAt >= from);
        if (to.HasValue) query = query.Where(a => a.CreatedAt <= to);

        return await query.OrderByDescending(a => a.CreatedAt)
            .Take(100)
            .Select(a => new AuditLogDto
            {
                AuditId = a.AuditId,
                UserId = a.UserId,
                Username = a.Username,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLogDto>> GetEntityHistoryAsync(string entityType, string entityId)
    {
        return await _context.AuditLogs
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AuditLogDto
            {
                AuditId = a.AuditId,
                UserId = a.UserId,
                Username = a.Username,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                OldValues = a.OldValues,
                NewValues = a.NewValues,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetUniqueActionsAsync()
    {
        return await _context.AuditLogs
            .Select(a => a.Action)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditUserDto>> GetUsersForFilterAsync()
    {
        return await _context.Users
            .Where(u => u.Status == 1)
            .OrderBy(u => u.Username)
            .Select(u => new AuditUserDto
            {
                UserId = u.UserId,
                Username = u.Username,
                FullName = u.FullName
            })
            .ToListAsync();
    }
}
