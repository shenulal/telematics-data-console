using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TelematicsDataConsole.Core.DTOs.VerificationLog;
using TelematicsDataConsole.Core.Entities;
using TelematicsDataConsole.Core.Interfaces.Services;

namespace TelematicsDataConsole.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VerificationLogsController : ControllerBase
{
    private readonly IVerificationLogService _verificationLogService;
    private readonly ILogger<VerificationLogsController> _logger;

    public VerificationLogsController(IVerificationLogService verificationLogService, ILogger<VerificationLogsController> logger)
    {
        _verificationLogService = verificationLogService;
        _logger = logger;
    }

    /// <summary>
    /// Get all verification logs with advanced filtering (Super Admin and Reseller Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = $"{SystemRoles.SuperAdmin},{SystemRoles.ResellerAdmin}")]
    public async Task<IActionResult> GetAll([FromQuery] VerificationLogFilterDto filter)
    {
        // For Reseller Admin, force filter to their reseller only
        if (!User.IsInRole(SystemRoles.SuperAdmin))
        {
            var resellerClaim = User.FindFirst("ResellerId")?.Value;
            if (!string.IsNullOrEmpty(resellerClaim) && int.TryParse(resellerClaim, out var resellerId))
            {
                filter.ResellerId = resellerId;
            }
            else
            {
                return Forbid();
            }
        }

        var result = await _verificationLogService.GetAllAsync(filter);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = $"{SystemRoles.SuperAdmin},{SystemRoles.ResellerAdmin}")]
    public async Task<IActionResult> GetById(int id)
    {
        var log = await _verificationLogService.GetByIdAsync(id);
        if (log == null)
            return NotFound(new { message = "Verification log not found" });

        // For Reseller Admin, check if the log belongs to their reseller
        if (!User.IsInRole(SystemRoles.SuperAdmin))
        {
            var resellerClaim = User.FindFirst("ResellerId")?.Value;
            if (!string.IsNullOrEmpty(resellerClaim) && int.TryParse(resellerClaim, out var resellerId))
            {
                if (log.ResellerId != resellerId)
                    return Forbid();
            }
        }

        return Ok(log);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVerificationLogDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var log = await _verificationLogService.CreateAsync(dto);
        _logger.LogInformation("Verification log created: {VerificationId} for DeviceId: {DeviceId}",
            log.VerificationId, log.DeviceId);
        return CreatedAtAction(nameof(GetById), new { id = log.VerificationId }, log);
    }

    [HttpGet("technician/{technicianId}")]
    [Authorize(Roles = $"{SystemRoles.SuperAdmin},{SystemRoles.ResellerAdmin}")]
    public async Task<IActionResult> GetByTechnician(int technicianId, [FromQuery] int limit = 50)
    {
        var logs = await _verificationLogService.GetByTechnicianIdAsync(technicianId, limit);

        // For Reseller Admin, filter logs to their reseller only
        if (!User.IsInRole(SystemRoles.SuperAdmin))
        {
            var resellerClaim = User.FindFirst("ResellerId")?.Value;
            if (!string.IsNullOrEmpty(resellerClaim) && int.TryParse(resellerClaim, out var resellerId))
            {
                logs = logs.Where(l => l.ResellerId == resellerId).ToList();
            }
        }

        return Ok(logs);
    }

    [HttpGet("device/{deviceId}")]
    [Authorize(Roles = $"{SystemRoles.SuperAdmin},{SystemRoles.ResellerAdmin}")]
    public async Task<IActionResult> GetByDevice(int deviceId, [FromQuery] int limit = 50)
    {
        var logs = await _verificationLogService.GetByDeviceIdAsync(deviceId, limit);

        // For Reseller Admin, filter logs to their reseller only
        if (!User.IsInRole(SystemRoles.SuperAdmin))
        {
            var resellerClaim = User.FindFirst("ResellerId")?.Value;
            if (!string.IsNullOrEmpty(resellerClaim) && int.TryParse(resellerClaim, out var resellerId))
            {
                logs = logs.Where(l => l.ResellerId == resellerId).ToList();
            }
        }

        return Ok(logs);
    }

    [HttpGet("statistics")]
    [Authorize(Roles = $"{SystemRoles.SuperAdmin},{SystemRoles.ResellerAdmin}")]
    public async Task<IActionResult> GetStatistics([FromQuery] int? technicianId = null,
        [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
    {
        var stats = await _verificationLogService.GetStatisticsAsync(technicianId, fromDate, toDate);
        return Ok(stats);
    }
}

