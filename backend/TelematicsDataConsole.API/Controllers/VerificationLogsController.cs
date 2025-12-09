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

    [HttpGet]
    [Authorize(Roles = SystemRoles.SuperAdmin)]
    public async Task<IActionResult> GetAll([FromQuery] VerificationLogFilterDto filter)
    {
        var result = await _verificationLogService.GetAllAsync(filter);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var log = await _verificationLogService.GetByIdAsync(id);
        if (log == null)
            return NotFound(new { message = "Verification log not found" });
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
    public async Task<IActionResult> GetByTechnician(int technicianId, [FromQuery] int limit = 50)
    {
        var logs = await _verificationLogService.GetByTechnicianIdAsync(technicianId, limit);
        return Ok(logs);
    }

    [HttpGet("device/{deviceId}")]
    public async Task<IActionResult> GetByDevice(int deviceId, [FromQuery] int limit = 50)
    {
        var logs = await _verificationLogService.GetByDeviceIdAsync(deviceId, limit);
        return Ok(logs);
    }

    [HttpGet("statistics")]
    [Authorize(Roles = SystemRoles.SuperAdmin)]
    public async Task<IActionResult> GetStatistics([FromQuery] int? technicianId = null,
        [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
    {
        var stats = await _verificationLogService.GetStatisticsAsync(technicianId, fromDate, toDate);
        return Ok(stats);
    }
}

