using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TelematicsDataConsole.API.Authorization;
using TelematicsDataConsole.Core.DTOs.Audit;
using TelematicsDataConsole.Core.Entities;
using TelematicsDataConsole.Core.Interfaces.Services;

namespace TelematicsDataConsole.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = SystemRoles.SuperAdmin)]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    /// <summary>
    /// Get audit logs with filtering
    /// </summary>
    [HttpGet("logs")]
    [RequirePermission(Permissions.AuditLogView)]
    public async Task<IActionResult> GetLogs([FromQuery] AuditFilterDto filter)
    {
        var result = await _auditService.GetLogsAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Get user activity log
    /// </summary>
    [HttpGet("user/{userId}/activity")]
    [RequirePermission(Permissions.AuditLogView)]
    public async Task<IActionResult> GetUserActivity(int userId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var activity = await _auditService.GetUserActivityAsync(userId, from, to);
        return Ok(activity);
    }

    /// <summary>
    /// Get entity history
    /// </summary>
    [HttpGet("entity/{entityType}/{entityId}")]
    [RequirePermission(Permissions.AuditLogView)]
    public async Task<IActionResult> GetEntityHistory(string entityType, string entityId)
    {
        var history = await _auditService.GetEntityHistoryAsync(entityType, entityId);
        return Ok(history);
    }

    /// <summary>
    /// Get unique actions for filter dropdown
    /// </summary>
    [HttpGet("actions")]
    [RequirePermission(Permissions.AuditLogView)]
    public async Task<IActionResult> GetUniqueActions()
    {
        var actions = await _auditService.GetUniqueActionsAsync();
        return Ok(actions);
    }

    /// <summary>
    /// Get users for filter dropdown
    /// </summary>
    [HttpGet("users")]
    [RequirePermission(Permissions.AuditLogView)]
    public async Task<IActionResult> GetUsersForFilter()
    {
        var users = await _auditService.GetUsersForFilterAsync();
        return Ok(users);
    }
}

