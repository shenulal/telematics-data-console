using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TelematicsDataConsole.Core.DTOs.Role;
using TelematicsDataConsole.Core.Entities;
using TelematicsDataConsole.Core.Interfaces.Services;

namespace TelematicsDataConsole.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{SystemRoles.SuperAdmin},{SystemRoles.ResellerAdmin}")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;
    private readonly ILogger<RolesController> _logger;

    public RolesController(IRoleService roleService, ILogger<RolesController> logger)
    {
        _roleService = roleService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var resellerId = GetCurrentResellerId();
        var roles = await _roleService.GetAllAsync(resellerId);
        return Ok(roles);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var role = await _roleService.GetByIdAsync(id);
        if (role == null)
            return NotFound(new { message = "Role not found" });

        // Check access for reseller admin
        var resellerId = GetCurrentResellerId();
        if (resellerId.HasValue && !role.IsSystemRole && role.ResellerId != resellerId)
            return Forbid();

        return Ok(role);
    }

    [HttpGet("my-permissions")]
    public async Task<IActionResult> GetMyPermissions()
    {
        var userId = GetCurrentUserId();
        var permissions = await _roleService.GetUserPermissionsAsync(userId);
        return Ok(permissions);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoleDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userId = GetCurrentUserId();
            var resellerId = GetCurrentResellerId();

            // Validate permissions for reseller admin
            if (resellerId.HasValue)
            {
                var userPermissions = await _roleService.GetUserPermissionsAsync(userId);
                var userPermissionIds = userPermissions.Select(p => p.PermissionId).ToHashSet();

                // Filter to only permissions the user has
                var validPermissionIds = dto.PermissionIds.Where(id => userPermissionIds.Contains(id)).ToList();
                if (validPermissionIds.Count != dto.PermissionIds.Count)
                {
                    _logger.LogWarning("User {UserId} attempted to assign permissions they don't have", userId);
                }
                dto.PermissionIds = validPermissionIds;
            }

            var role = await _roleService.CreateAsync(dto, userId, resellerId);
            _logger.LogInformation("Role created: {RoleId} by {UserId}", role.RoleId, userId);
            return CreatedAtAction(nameof(GetById), new { id = role.RoleId }, role);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRoleDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userId = GetCurrentUserId();
            var resellerId = GetCurrentResellerId();

            // Check access for reseller admin
            if (!await _roleService.CanAccessRoleAsync(id, userId, resellerId))
                return Forbid();

            // Validate permissions for reseller admin
            if (resellerId.HasValue && dto.PermissionIds != null)
            {
                var userPermissions = await _roleService.GetUserPermissionsAsync(userId);
                var userPermissionIds = userPermissions.Select(p => p.PermissionId).ToHashSet();

                // Filter to only permissions the user has
                var validPermissionIds = dto.PermissionIds.Where(pid => userPermissionIds.Contains(pid)).ToList();
                if (validPermissionIds.Count != dto.PermissionIds.Count)
                {
                    _logger.LogWarning("User {UserId} attempted to assign permissions they don't have", userId);
                }
                dto.PermissionIds = validPermissionIds;
            }

            var role = await _roleService.UpdateAsync(id, dto, userId);
            _logger.LogInformation("Role updated: {RoleId}", id);
            return Ok(role);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Role not found" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var resellerId = GetCurrentResellerId();

            // Check access for reseller admin
            if (!await _roleService.CanAccessRoleAsync(id, userId, resellerId))
                return Forbid();

            var result = await _roleService.DeleteAsync(id);
            if (!result)
                return NotFound(new { message = "Role not found" });

            _logger.LogInformation("Role deleted: {RoleId}", id);
            return Ok(new { message = "Role deleted successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/permissions")]
    public async Task<IActionResult> AssignPermissions(int id, [FromBody] List<int> permissionIds)
    {
        var userId = GetCurrentUserId();
        var resellerId = GetCurrentResellerId();

        // Check access for reseller admin
        if (!await _roleService.CanAccessRoleAsync(id, userId, resellerId))
            return Forbid();

        // Validate permissions for reseller admin
        if (resellerId.HasValue)
        {
            var userPermissions = await _roleService.GetUserPermissionsAsync(userId);
            var userPermissionIds = userPermissions.Select(p => p.PermissionId).ToHashSet();

            // Filter to only permissions the user has
            permissionIds = permissionIds.Where(pid => userPermissionIds.Contains(pid)).ToList();
        }

        var result = await _roleService.AssignPermissionsAsync(id, permissionIds, userId);

        if (!result)
            return NotFound(new { message = "Role not found" });

        _logger.LogInformation("Permissions assigned to role: {RoleId}", id);
        return Ok(new { message = "Permissions assigned successfully" });
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var id) ? id : 0;
    }

    private int? GetCurrentResellerId()
    {
        // Check if user is SuperAdmin - they have no reseller restriction
        if (User.IsInRole(SystemRoles.SuperAdmin))
            return null;

        var resellerIdClaim = User.FindFirst("ResellerId")?.Value;
        return int.TryParse(resellerIdClaim, out var id) ? id : null;
    }
}

