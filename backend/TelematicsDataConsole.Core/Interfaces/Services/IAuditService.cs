using TelematicsDataConsole.Core.DTOs;
using TelematicsDataConsole.Core.DTOs.Audit;

namespace TelematicsDataConsole.Core.Interfaces.Services;

public interface IAuditService
{
    Task LogAsync(AuditLogDto log);
    Task LogAsync(int? userId, string action, string entityType, string? entityId = null,
        object? oldValues = null, object? newValues = null);
    Task<PagedResult<AuditLogDto>> GetLogsAsync(AuditFilterDto filter);
    Task<IEnumerable<AuditLogDto>> GetUserActivityAsync(int userId, DateTime? from = null, DateTime? to = null);
    Task<IEnumerable<AuditLogDto>> GetEntityHistoryAsync(string entityType, string entityId);
}

