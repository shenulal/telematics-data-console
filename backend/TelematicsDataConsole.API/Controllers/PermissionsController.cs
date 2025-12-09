using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TelematicsDataConsole.Core.DTOs.Role;
using TelematicsDataConsole.Core.Entities;
using TelematicsDataConsole.Core.Interfaces.Services;

namespace TelematicsDataConsole.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = SystemRoles.SuperAdmin)]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissionService;
    private readonly ILogger<PermissionsController> _logger;

    public PermissionsController(IPermissionService permissionService, ILogger<PermissionsController> logger)
    {
        _permissionService = permissionService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? module = null)
    {
        var permissions = await _permissionService.GetAllAsync(module);
        return Ok(permissions);
    }

    [HttpGet("modules")]
    public async Task<IActionResult> GetModules()
    {
        var modules = await _permissionService.GetModulesAsync();
        return Ok(modules);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var permission = await _permissionService.GetByIdAsync(id);
        if (permission == null)
            return NotFound(new { message = "Permission not found" });
        return Ok(permission);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePermissionDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var permission = await _permissionService.CreateAsync(dto);
            _logger.LogInformation("Permission created: {PermissionId}", permission.PermissionId);
            return CreatedAtAction(nameof(GetById), new { id = permission.PermissionId }, permission);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePermissionDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var permission = await _permissionService.UpdateAsync(id, dto);
            _logger.LogInformation("Permission updated: {PermissionId}", id);
            return Ok(permission);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Permission not found" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _permissionService.DeleteAsync(id);
        if (!result)
            return NotFound(new { message = "Permission not found" });

        _logger.LogInformation("Permission deleted: {PermissionId}", id);
        return Ok(new { message = "Permission deleted successfully" });
    }
}

