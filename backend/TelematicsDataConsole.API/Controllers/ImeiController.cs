using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TelematicsDataConsole.API.Authorization;
using TelematicsDataConsole.Core.DTOs.Imei;
using TelematicsDataConsole.Core.Entities;
using TelematicsDataConsole.Core.Interfaces.Services;

namespace TelematicsDataConsole.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ImeiController : ControllerBase
{
    private readonly IImeiService _imeiService;
    private readonly IVzoneApiService _vzoneApiService;
    private readonly ILogger<ImeiController> _logger;

    public ImeiController(IImeiService imeiService, IVzoneApiService vzoneApiService, ILogger<ImeiController> logger)
    {
        _imeiService = imeiService;
        _vzoneApiService = vzoneApiService;
        _logger = logger;
    }

    /// <summary>
    /// Check if technician has access to the IMEI
    /// </summary>
    [HttpGet("{imei}/check-access")]
    [RequirePermission(Permissions.ImeiViewData)]
    public async Task<IActionResult> CheckAccess(string imei)
    {
        // Debug: Log all claims
        var allClaims = User.Claims.Select(c => $"{c.Type}={c.Value}").ToList();
        _logger.LogInformation("All claims in JWT: [{Claims}]", string.Join(", ", allClaims));

        // Check if user is admin (Super Admin, Reseller Admin, Supervisor)
        if (IsAdminUser())
        {
            var userId = GetCurrentUserId();
            var resellerId = GetResellerId();

            var result = await _imeiService.CheckAdminAccessAsync(userId, resellerId, imei);

            if (!result.HasAccess)
            {
                _logger.LogWarning("Access denied for user {UserId} to IMEI {Imei}: {Message}",
                    userId, imei, result.Message);
                return StatusCode(403, new { message = result.Message, reason = result.RestrictionReason });
            }

            return Ok(new { hasAccess = true, deviceId = result.DeviceId });
        }

        // Original technician flow
        var technicianId = GetTechnicianId();
        if (technicianId == 0)
            return BadRequest(new { message = "Technician ID not found in token" });

        var techResult = await _imeiService.CheckAccessAsync(technicianId, imei);

        if (!techResult.HasAccess)
        {
            _logger.LogWarning("Access denied for technician {TechnicianId} to IMEI {Imei}: {Message}",
                technicianId, imei, techResult.Message);
            return StatusCode(403, new { message = techResult.Message, reason = techResult.RestrictionReason });
        }

        return Ok(new { hasAccess = true, deviceId = techResult.DeviceId });
    }

    /// <summary>
    /// Get device data by IMEI for verification
    /// </summary>
    [HttpGet("{imei}/verify")]
    [RequirePermission(Permissions.ImeiVerify)]
    public async Task<IActionResult> GetDeviceData(string imei)
    {
        // Check if user is admin (Super Admin, Reseller Admin, Supervisor)
        if (IsAdminUser())
        {
            var userId = GetCurrentUserId();
            var resellerId = GetResellerId();

            var result = await _imeiService.GetDeviceDataForAdminAsync(userId, resellerId, imei);

            if (!result.Success)
            {
                if (result.Message?.Contains("Restricted") == true || result.Message?.Contains("authorized") == true)
                {
                    return StatusCode(403, new { message = result.Message });
                }
                return NotFound(new { message = result.Message });
            }

            _logger.LogInformation("Admin user {UserId} accessed IMEI {Imei}", userId, imei);
            return Ok(result.Data);
        }

        // Original technician flow
        var technicianId = GetTechnicianId();
        if (technicianId == 0)
            return BadRequest(new { message = "Technician ID not found in token" });

        var techResult = await _imeiService.GetDeviceDataAsync(technicianId, imei);

        if (!techResult.Success)
        {
            if (techResult.Message?.Contains("Restricted") == true || techResult.Message?.Contains("authorized") == true)
            {
                return StatusCode(403, new { message = techResult.Message });
            }
            return NotFound(new { message = techResult.Message });
        }

        _logger.LogInformation("Technician {TechnicianId} accessed IMEI {Imei}", technicianId, imei);
        return Ok(techResult.Data);
    }

    /// <summary>
    /// Submit verification for a device
    /// </summary>
    [HttpPost("verification")]
    [RequirePermission(Permissions.ImeiVerify)]
    public async Task<IActionResult> SubmitVerification([FromBody] VerificationRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Check if user is admin (Super Admin, Reseller Admin, Supervisor)
        if (IsAdminUser())
        {
            var userId = GetCurrentUserId();
            var resellerId = GetResellerId();

            var result = await _imeiService.VerifyDeviceForAdminAsync(userId, resellerId, request);

            if (!result.Success)
            {
                if (result.Message?.Contains("Restricted") == true)
                {
                    return StatusCode(403, new { message = result.Message });
                }
                return BadRequest(new { message = result.Message });
            }

            _logger.LogInformation("Admin user {UserId} verified IMEI {Imei}, VerificationId: {VerificationId}",
                userId, request.Imei, result.VerificationId);

            return Ok(new
            {
                success = true,
                verificationId = result.VerificationId,
                message = "Verification completed successfully"
            });
        }

        // Original technician flow
        var technicianId = GetTechnicianId();
        if (technicianId == 0)
            return BadRequest(new { message = "Technician ID not found in token" });

        var techResult = await _imeiService.VerifyDeviceAsync(technicianId, request);

        if (!techResult.Success)
        {
            if (techResult.Message?.Contains("Restricted") == true)
            {
                return StatusCode(403, new { message = techResult.Message });
            }
            return BadRequest(new { message = techResult.Message });
        }

        _logger.LogInformation("Technician {TechnicianId} verified IMEI {Imei}, VerificationId: {VerificationId}",
            technicianId, request.Imei, techResult.VerificationId);

        return Ok(new {
            success = true,
            verificationId = techResult.VerificationId,
            message = "Verification completed successfully"
        });
    }

    /// <summary>
    /// Get verification history for current technician with date range filtering
    /// </summary>
    [HttpGet("history")]
    [RequirePermission(Permissions.ImeiVerify)]
    public async Task<IActionResult> GetVerificationHistory(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var technicianId = GetTechnicianId();
        if (technicianId == 0)
            return BadRequest(new { message = "Technician ID not found in token" });

        var filter = new VerificationHistoryFilterDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            Page = page,
            PageSize = pageSize
        };

        var history = await _imeiService.GetVerificationHistoryAsync(technicianId, filter);
        return Ok(history);
    }

    /// <summary>
    /// Get live device data from Vzone API
    /// </summary>
    [HttpGet("{imei}/live")]
    [RequirePermission(Permissions.ImeiVerify)]
    public async Task<IActionResult> GetLiveDeviceData(string imei)
    {
        // First check access
        if (IsAdminUser())
        {
            var userId = GetCurrentUserId();
            var resellerId = GetResellerId();
            var accessResult = await _imeiService.CheckAdminAccessAsync(userId, resellerId, imei);

            if (!accessResult.HasAccess)
            {
                _logger.LogWarning("Access denied for admin {UserId} to IMEI {Imei}: {Message}",
                    userId, imei, accessResult.Message);
                return StatusCode(403, new { message = accessResult.Message });
            }
        }
        else
        {
            var technicianId = GetTechnicianId();
            if (technicianId == 0)
                return BadRequest(new { message = "Technician ID not found in token" });

            var accessResult = await _imeiService.CheckAccessAsync(technicianId, imei);
            if (!accessResult.HasAccess)
            {
                return StatusCode(403, new { message = accessResult.Message });
            }
        }

        // Fetch live data from Vzone API
        var liveData = await _vzoneApiService.GetLiveDeviceDataAsync(imei);
        if (liveData == null)
        {
            return NotFound(new { message = "Unable to fetch live data for this device" });
        }

        _logger.LogInformation("Live data fetched for IMEI {Imei}", imei);
        return Ok(liveData);
    }

    private int GetTechnicianId()
    {
        var technicianIdClaim = User.FindFirst("TechnicianId")?.Value;
        return int.TryParse(technicianIdClaim, out var id) ? id : 0;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var id) ? id : 0;
    }

    /// <summary>
    /// Get all roles from the user's claims (case-insensitive)
    /// </summary>
    private List<string> GetUserRoles()
    {
        return User.Claims
            .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();
    }

    /// <summary>
    /// Check if user has a specific role (case-insensitive, handles variations)
    /// </summary>
    private bool HasRole(string role)
    {
        var roles = GetUserRoles();
        var normalizedTarget = role.ToUpperInvariant().Replace(" ", "");
        return roles.Any(r => r.ToUpperInvariant().Replace(" ", "") == normalizedTarget);
    }

    /// <summary>
    /// Check if user is Super Admin (handles variations like "SUPERADMIN", "Super Admin", "Admin")
    /// </summary>
    private bool IsSuperAdminRole()
    {
        var roles = GetUserRoles();
        return roles.Any(r =>
        {
            var normalized = r.ToUpperInvariant().Replace(" ", "");
            return normalized == "SUPERADMIN" || normalized == "ADMIN";
        });
    }

    /// <summary>
    /// Check if user is Reseller Admin (handles variations)
    /// </summary>
    private bool IsResellerAdminRole()
    {
        var roles = GetUserRoles();
        return roles.Any(r =>
        {
            var normalized = r.ToUpperInvariant().Replace(" ", "");
            return normalized == "RESELLERADMIN" || normalized == "RESELLER";
        });
    }

    private int? GetResellerId()
    {
        // Super Admin has no reseller - null means can access any IMEI
        if (IsSuperAdminRole())
        {
            return null;
        }

        var resellerIdClaim = User.FindFirst("ResellerId")?.Value;
        return int.TryParse(resellerIdClaim, out var id) ? id : null;
    }

    private bool IsAdminUser()
    {
        // Get all roles for logging
        var roles = GetUserRoles();
        _logger.LogInformation("User roles from JWT: [{Roles}]", string.Join(", ", roles));

        // Check if user is Super Admin, Reseller Admin, or Supervisor (case-insensitive)
        var isSuperAdmin = IsSuperAdminRole();
        var isResellerAdmin = IsResellerAdminRole();
        var isSupervisor = HasRole("SUPERVISOR");
        var isTechnician = HasRole("TECHNICIAN");

        // Log role detection for debugging
        _logger.LogInformation("Role check: SuperAdmin={IsSuperAdmin}, ResellerAdmin={IsResellerAdmin}, Supervisor={IsSupervisor}, Technician={IsTechnician}",
            isSuperAdmin, isResellerAdmin, isSupervisor, isTechnician);

        // If user has technician role and is also an admin, use technician flow
        // unless they explicitly want to use admin flow (for now, we prioritize technician)
        if (isTechnician && GetTechnicianId() != 0)
        {
            _logger.LogInformation("User has Technician role with TechnicianId, using technician flow");
            return false;
        }

        var isAdmin = isSuperAdmin || isResellerAdmin || isSupervisor;
        _logger.LogInformation("IsAdminUser result: {IsAdmin}", isAdmin);
        return isAdmin;
    }
}

