using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TelematicsDataConsole.API.Authorization;
using TelematicsDataConsole.Core.DTOs.Technician;
using TelematicsDataConsole.Core.Entities;
using TelematicsDataConsole.Core.Interfaces.Services;

namespace TelematicsDataConsole.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TechniciansController : ControllerBase
{
    private readonly ITechnicianService _technicianService;
    private readonly ILogger<TechniciansController> _logger;

    public TechniciansController(ITechnicianService technicianService, ILogger<TechniciansController> logger)
    {
        _technicianService = technicianService;
        _logger = logger;
    }

    /// <summary>
    /// Get all technicians with filtering and pagination
    /// </summary>
    [HttpGet]
    [RequirePermission(Permissions.TechnicianView)]
    public async Task<IActionResult> GetAll([FromQuery] TechnicianFilterDto filter)
    {
        // If reseller admin, only show their technicians
        if (User.IsInRole(SystemRoles.ResellerAdmin))
        {
            var resellerId = User.FindFirst("ResellerId")?.Value;
            if (int.TryParse(resellerId, out var rid))
                filter.ResellerId = rid;
        }

        var result = await _technicianService.GetAllAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Get technician by ID
    /// </summary>
    [HttpGet("{id}")]
    [RequirePermission(Permissions.TechnicianView)]
    public async Task<IActionResult> GetById(int id)
    {
        var technician = await _technicianService.GetByIdAsync(id);
        if (technician == null)
            return NotFound(new { message = "Technician not found" });

        // Check reseller admin access
        if (User.IsInRole(SystemRoles.ResellerAdmin))
        {
            var resellerId = User.FindFirst("ResellerId")?.Value;
            if (technician.ResellerId?.ToString() != resellerId)
                return Forbid();
        }

        return Ok(technician);
    }

    /// <summary>
    /// Create a new technician
    /// </summary>
    [HttpPost]
    [RequirePermission(Permissions.TechnicianCreate)]
    public async Task<IActionResult> Create([FromBody] CreateTechnicianDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // If reseller admin, force their reseller ID
        if (User.IsInRole(SystemRoles.ResellerAdmin))
        {
            var resellerId = User.FindFirst("ResellerId")?.Value;
            if (int.TryParse(resellerId, out var rid))
                dto.ResellerId = rid;
        }

        var userId = GetCurrentUserId();
        var technician = await _technicianService.CreateAsync(dto, userId);

        _logger.LogInformation("Technician created: {TechnicianId} by user {UserId}", 
            technician.TechnicianId, userId);

        return CreatedAtAction(nameof(GetById), new { id = technician.TechnicianId }, technician);
    }

    /// <summary>
    /// Update a technician
    /// </summary>
    [HttpPut("{id}")]
    [RequirePermission(Permissions.TechnicianEdit)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTechnicianDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existing = await _technicianService.GetByIdAsync(id);
        if (existing == null)
            return NotFound(new { message = "Technician not found" });

        // Check reseller admin access
        if (User.IsInRole(SystemRoles.ResellerAdmin))
        {
            var resellerId = User.FindFirst("ResellerId")?.Value;
            if (existing.ResellerId?.ToString() != resellerId)
                return Forbid();
        }

        var userId = GetCurrentUserId();
        var technician = await _technicianService.UpdateAsync(id, dto, userId);

        _logger.LogInformation("Technician updated: {TechnicianId} by user {UserId}", id, userId);

        return Ok(technician);
    }

    /// <summary>
    /// Deactivate a technician
    /// </summary>
    [HttpPost("{id}/deactivate")]
    [RequirePermission(Permissions.TechnicianEdit)]
    public async Task<IActionResult> Deactivate(int id)
    {
        var userId = GetCurrentUserId();
        var result = await _technicianService.DeactivateAsync(id, userId);

        if (!result)
            return NotFound(new { message = "Technician not found" });

        _logger.LogInformation("Technician deactivated: {TechnicianId} by user {UserId}", id, userId);
        return Ok(new { message = "Technician deactivated successfully" });
    }

    /// <summary>
    /// Activate a technician
    /// </summary>
    [HttpPost("{id}/activate")]
    [RequirePermission(Permissions.TechnicianEdit)]
    public async Task<IActionResult> Activate(int id)
    {
        var userId = GetCurrentUserId();
        var result = await _technicianService.ActivateAsync(id, userId);

        if (!result)
            return NotFound(new { message = "Technician not found" });

        _logger.LogInformation("Technician activated: {TechnicianId} by user {UserId}", id, userId);
        return Ok(new { message = "Technician activated successfully" });
    }

    /// <summary>
    /// Delete a technician (hard delete - removes technician and associated user completely)
    /// </summary>
    [HttpDelete("{id}")]
    [RequirePermission(Permissions.TechnicianEdit)]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();
        var result = await _technicianService.DeleteAsync(id, userId);

        if (!result)
            return NotFound(new { message = "Technician not found" });

        _logger.LogInformation("Technician deleted: {TechnicianId} by user {UserId}", id, userId);
        return Ok(new { message = "Technician deleted successfully" });
    }

    /// <summary>
    /// Get technician statistics
    /// </summary>
    [HttpGet("{id}/stats")]
    [Authorize]
    public async Task<IActionResult> GetStats(int id, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        // Allow technicians to view their own stats
        var technicianIdClaim = User.FindFirst("TechnicianId")?.Value;
        var isSelf = int.TryParse(technicianIdClaim, out var techId) && techId == id;

        // Check if user has permission to view other technicians' stats
        if (!isSelf)
        {
            var hasPermission = User.Claims.Any(c => c.Type == "permission" && c.Value == Permissions.TechnicianView);
            var isSuperAdmin = User.IsInRole(SystemRoles.SuperAdmin);
            var isResellerAdmin = User.IsInRole(SystemRoles.ResellerAdmin);

            if (!hasPermission && !isSuperAdmin && !isResellerAdmin)
            {
                return Forbid();
            }
        }

        var stats = await _technicianService.GetStatsAsync(id, from, to);
        return Ok(stats);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var id) ? id : 0;
    }
}

