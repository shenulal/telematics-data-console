using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TelematicsDataConsole.Core.DTOs.Auth;
using TelematicsDataConsole.Core.Interfaces.Services;

namespace TelematicsDataConsole.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticate user and get JWT token
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _authService.LoginAsync(request, ipAddress, userAgent);

        if (!result.Success)
        {
            _logger.LogWarning("Failed login attempt for {Username} from {IpAddress}", 
                request.UsernameOrEmail, ipAddress);
            return Unauthorized(new { message = result.Message });
        }

        _logger.LogInformation("Successful login for user {Username}", request.UsernameOrEmail);
        return Ok(result);
    }

    /// <summary>
    /// Refresh JWT token
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken);
        
        if (!result.Success)
            return Unauthorized(new { message = result.Message });

        return Ok(result);
    }

    /// <summary>
    /// Logout current user
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        await _authService.LogoutAsync(userId);
        return Ok(new { message = "Logged out successfully" });
    }

    /// <summary>
    /// Change password for current user
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = await _authService.ChangePasswordAsync(userId, request);

            if (!result)
                return NotFound(new { message = "User not found" });

            return Ok(new { message = "Password changed successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get current user profile
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var technicianId = User.FindFirst("TechnicianId")?.Value;
        var resellerId = User.FindFirst("ResellerId")?.Value;
        var roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
        var permissions = User.FindAll("Permission").Select(c => c.Value).ToList();

        return Ok(new
        {
            userId,
            username,
            email,
            technicianId = string.IsNullOrEmpty(technicianId) ? null : technicianId,
            resellerId = string.IsNullOrEmpty(resellerId) ? null : resellerId,
            roles,
            permissions
        });
    }

    /// <summary>
    /// Debug endpoint to see all claims in the JWT token
    /// </summary>
    [HttpGet("debug-claims")]
    [Authorize]
    public IActionResult DebugClaims()
    {
        var allClaims = User.Claims.Select(c => new { Type = c.Type, Value = c.Value }).ToList();
        return Ok(new
        {
            claimCount = allClaims.Count,
            claims = allClaims
        });
    }
}

