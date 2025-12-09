using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TelematicsDataConsole.API.Authorization;
using TelematicsDataConsole.Core.DTOs.Device;
using TelematicsDataConsole.Core.DTOs.ImeiRestriction;
using TelematicsDataConsole.Core.Entities;
using TelematicsDataConsole.Core.Interfaces.Services;

namespace TelematicsDataConsole.API.Controllers;

[ApiController]
[Route("api/imei/restrictions")]
[Authorize]
public class ImeiRestrictionsController : ControllerBase
{
    private readonly IImeiRestrictionService _restrictionService;
    private readonly IExternalDeviceService _externalDeviceService;
    private readonly ILogger<ImeiRestrictionsController> _logger;

    public ImeiRestrictionsController(
        IImeiRestrictionService restrictionService,
        IExternalDeviceService externalDeviceService,
        ILogger<ImeiRestrictionsController> logger)
    {
        _restrictionService = restrictionService;
        _externalDeviceService = externalDeviceService;
        _logger = logger;
    }

    /// <summary>
    /// Get restrictions for a technician
    /// </summary>
    [HttpGet("technician/{technicianId}")]
    [RequirePermission(Permissions.ImeiRestrictionManage)]
    public async Task<IActionResult> GetByTechnician(int technicianId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _restrictionService.GetByTechnicianAsync(technicianId, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Get active restrictions for a technician
    /// </summary>
    [HttpGet("technician/{technicianId}/active")]
    [RequirePermission(Permissions.ImeiRestrictionManage)]
    public async Task<IActionResult> GetActiveRestrictions(int technicianId)
    {
        var restrictions = await _restrictionService.GetActiveRestrictionsAsync(technicianId);
        return Ok(restrictions);
    }

    /// <summary>
    /// Get restriction by ID
    /// </summary>
    [HttpGet("{id}")]
    [RequirePermission(Permissions.ImeiRestrictionManage)]
    public async Task<IActionResult> GetById(int id)
    {
        var restriction = await _restrictionService.GetByIdAsync(id);
        if (restriction == null)
            return NotFound(new { message = "Restriction not found" });

        return Ok(restriction);
    }

    /// <summary>
    /// Create a new IMEI restriction
    /// </summary>
    [HttpPost]
    [RequirePermission(Permissions.ImeiRestrictionManage)]
    public async Task<IActionResult> Create([FromBody] CreateImeiRestrictionDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Validate that either DeviceId or TagId is provided
        if (!dto.DeviceId.HasValue && !dto.TagId.HasValue)
        {
            return BadRequest(new { message = "Either DeviceId or TagId must be provided" });
        }

        var userId = GetCurrentUserId();
        var restriction = await _restrictionService.CreateAsync(dto, userId);

        _logger.LogInformation("IMEI restriction created: {RestrictionId} for technician {TechnicianId} by user {UserId}",
            restriction.RestrictionId, dto.TechnicianId, userId);

        return CreatedAtAction(nameof(GetById), new { id = restriction.RestrictionId }, restriction);
    }

    /// <summary>
    /// Update an IMEI restriction
    /// </summary>
    [HttpPut("{id}")]
    [RequirePermission(Permissions.ImeiRestrictionManage)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateImeiRestrictionDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();

        try
        {
            var restriction = await _restrictionService.UpdateAsync(id, dto, userId);
            _logger.LogInformation("IMEI restriction updated: {RestrictionId} by user {UserId}", id, userId);
            return Ok(restriction);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Restriction not found" });
        }
    }

    /// <summary>
    /// Delete an IMEI restriction
    /// </summary>
    [HttpDelete("{id}")]
    [RequirePermission(Permissions.ImeiRestrictionManage)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _restrictionService.DeleteAsync(id);
        if (!result)
            return NotFound(new { message = "Restriction not found" });

        _logger.LogInformation("IMEI restriction deleted: {RestrictionId} by user {UserId}", id, GetCurrentUserId());
        return Ok(new { message = "Restriction deleted successfully" });
    }

    /// <summary>
    /// Check if a device is restricted for a technician
    /// </summary>
    [HttpGet("check")]
    [RequirePermission(Permissions.ImeiRestrictionManage)]
    public async Task<IActionResult> CheckRestriction([FromQuery] int technicianId, [FromQuery] int deviceId)
    {
        var isRestricted = await _restrictionService.IsDeviceRestrictedAsync(technicianId, deviceId);
        return Ok(new { isRestricted });
    }

    /// <summary>
    /// Search external devices for IMEI restriction
    /// </summary>
    [HttpGet("devices/search")]
    [RequirePermission(Permissions.ImeiRestrictionManage)]
    public async Task<IActionResult> SearchDevices([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var filter = new DeviceSearchFilter
            {
                Search = search,
                Page = page,
                PageSize = pageSize
            };
            var result = await _externalDeviceService.SearchDevicesAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search external devices");
            return StatusCode(500, new { error = "Failed to connect to external device database", details = ex.Message });
        }
    }

    /// <summary>
    /// Get external device by ID
    /// </summary>
    [HttpGet("devices/{deviceId}")]
    [RequirePermission(Permissions.ImeiRestrictionManage)]
    public async Task<IActionResult> GetDevice(long deviceId)
    {
        var device = await _externalDeviceService.GetByIdAsync(deviceId);
        if (device == null)
            return NotFound(new { message = "Device not found" });
        return Ok(device);
    }

    /// <summary>
    /// Get external device by IMEI
    /// </summary>
    [HttpGet("devices/imei/{imei}")]
    [RequirePermission(Permissions.ImeiRestrictionManage)]
    public async Task<IActionResult> GetDeviceByImei(string imei)
    {
        var device = await _externalDeviceService.GetByImeiAsync(imei);
        if (device == null)
            return NotFound(new { message = "Device not found" });
        return Ok(device);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var id) ? id : 0;
    }
}

