using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TelematicsDataConsole.Core.DTOs.User;
using TelematicsDataConsole.Core.Entities;
using TelematicsDataConsole.Core.Interfaces.Services;

namespace TelematicsDataConsole.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{SystemRoles.SuperAdmin},{SystemRoles.ResellerAdmin}")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, 
        [FromQuery] string? search = null, [FromQuery] short? status = null)
    {
        var result = await _userService.GetAllAsync(page, pageSize, search, status);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null)
            return NotFound(new { message = "User not found" });
        return Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userId = GetCurrentUserId();
            var user = await _userService.CreateAsync(dto, userId);
            _logger.LogInformation("User created: {UserId} by {CreatedBy}", user.UserId, userId);
            return CreatedAtAction(nameof(GetById), new { id = user.UserId }, user);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userId = GetCurrentUserId();
            var user = await _userService.UpdateAsync(id, dto, userId);
            _logger.LogInformation("User updated: {UserId} by {UpdatedBy}", id, userId);
            return Ok(user);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "User not found" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _userService.DeleteAsync(id);
        if (!result)
            return NotFound(new { message = "User not found" });

        _logger.LogInformation("User deleted: {UserId}", id);
        return Ok(new { message = "User deleted successfully" });
    }

    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordDto dto)
    {
        var adminId = GetCurrentUserId();
        var result = await _userService.ResetPasswordAsync(id, dto, adminId);
        
        if (!result)
            return NotFound(new { message = "User not found" });

        _logger.LogInformation("Password reset for user: {UserId} by admin: {AdminId}", id, adminId);
        return Ok(new { message = "Password reset successfully" });
    }

    [HttpPost("change-password")]
    [AllowAnonymous]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _userService.ChangePasswordAsync(userId, dto);
            
            if (!result)
                return NotFound(new { message = "User not found" });

            return Ok(new { message = "Password changed successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("reseller/{resellerId}")]
    public async Task<IActionResult> GetByReseller(int resellerId)
    {
        var users = await _userService.GetByResellerIdAsync(resellerId);
        return Ok(users);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var id) ? id : 0;
    }
}

