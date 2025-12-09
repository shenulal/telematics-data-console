using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TelematicsDataConsole.API.Authorization;
using TelematicsDataConsole.Core.DTOs.Reseller;
using TelematicsDataConsole.Core.Entities;
using TelematicsDataConsole.Core.Interfaces.Services;

namespace TelematicsDataConsole.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = SystemRoles.SuperAdmin)]
public class ResellersController : ControllerBase
{
    private readonly IResellerService _resellerService;
    private readonly ITechnicianService _technicianService;
    private readonly ILogger<ResellersController> _logger;

    public ResellersController(
        IResellerService resellerService, 
        ITechnicianService technicianService,
        ILogger<ResellersController> logger)
    {
        _resellerService = resellerService;
        _technicianService = technicianService;
        _logger = logger;
    }

    /// <summary>
    /// Get all resellers with filtering and pagination
    /// </summary>
    [HttpGet]
    [RequirePermission(Permissions.ResellerView)]
    public async Task<IActionResult> GetAll([FromQuery] ResellerFilterDto filter)
    {
        var result = await _resellerService.GetAllAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Get reseller by ID
    /// </summary>
    [HttpGet("{id}")]
    [RequirePermission(Permissions.ResellerView)]
    public async Task<IActionResult> GetById(int id)
    {
        var reseller = await _resellerService.GetByIdAsync(id);
        if (reseller == null)
            return NotFound(new { message = "Reseller not found" });

        return Ok(reseller);
    }

    /// <summary>
    /// Create a new reseller
    /// </summary>
    [HttpPost]
    [RequirePermission(Permissions.ResellerCreate)]
    public async Task<IActionResult> Create([FromBody] CreateResellerDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        var reseller = await _resellerService.CreateAsync(dto, userId);

        _logger.LogInformation("Reseller created: {ResellerId} by user {UserId}", 
            reseller.ResellerId, userId);

        return CreatedAtAction(nameof(GetById), new { id = reseller.ResellerId }, reseller);
    }

    /// <summary>
    /// Update a reseller
    /// </summary>
    [HttpPut("{id}")]
    [RequirePermission(Permissions.ResellerEdit)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateResellerDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();

        try
        {
            var reseller = await _resellerService.UpdateAsync(id, dto, userId);
            _logger.LogInformation("Reseller updated: {ResellerId} by user {UserId}", id, userId);
            return Ok(reseller);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Reseller not found" });
        }
    }

    /// <summary>
    /// Deactivate a reseller
    /// </summary>
    [HttpPost("{id}/deactivate")]
    [RequirePermission(Permissions.ResellerEdit)]
    public async Task<IActionResult> Deactivate(int id)
    {
        var userId = GetCurrentUserId();
        var result = await _resellerService.DeactivateAsync(id, userId);

        if (!result)
            return NotFound(new { message = "Reseller not found" });

        _logger.LogInformation("Reseller deactivated: {ResellerId} by user {UserId}", id, userId);
        return Ok(new { message = "Reseller deactivated successfully" });
    }

    /// <summary>
    /// Activate a reseller
    /// </summary>
    [HttpPost("{id}/activate")]
    [RequirePermission(Permissions.ResellerEdit)]
    public async Task<IActionResult> Activate(int id)
    {
        var userId = GetCurrentUserId();
        var result = await _resellerService.ActivateAsync(id, userId);

        if (!result)
            return NotFound(new { message = "Reseller not found" });

        _logger.LogInformation("Reseller activated: {ResellerId} by user {UserId}", id, userId);
        return Ok(new { message = "Reseller activated successfully" });
    }

    /// <summary>
    /// Update reseller status with cascade to all related entities (users, technicians, tags, roles)
    /// </summary>
    [HttpPut("{id}/status")]
    [RequirePermission(Permissions.ResellerEdit)]
    public async Task<IActionResult> UpdateStatusWithCascade(int id, [FromBody] UpdateResellerStatusDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();

        try
        {
            var result = await _resellerService.UpdateStatusWithCascadeAsync(id, dto.Status, userId);
            _logger.LogInformation("Reseller status updated with cascade: {ResellerId} to {Status} by user {UserId}",
                id, dto.Status, userId);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Reseller not found" });
        }
    }

    /// <summary>
    /// Get reseller statistics
    /// </summary>
    [HttpGet("{id}/stats")]
    [RequirePermission(Permissions.ResellerView)]
    public async Task<IActionResult> GetStats(int id, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        try
        {
            var stats = await _resellerService.GetStatsAsync(id, from, to);
            return Ok(stats);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Reseller not found" });
        }
    }

    /// <summary>
    /// Get technicians for a reseller
    /// </summary>
    [HttpGet("{id}/technicians")]
    [RequirePermission(Permissions.TechnicianView)]
    public async Task<IActionResult> GetTechnicians(int id)
    {
        var technicians = await _technicianService.GetByResellerAsync(id);
        return Ok(technicians);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var id) ? id : 0;
    }
}

