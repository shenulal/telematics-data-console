using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TelematicsDataConsole.Core.Entities;
using TelematicsDataConsole.Core.Interfaces.Services;
using System.Security.Claims;

namespace TelematicsDataConsole.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard for Super Admin
    /// </summary>
    [HttpGet("superadmin")]
    [Authorize(Roles = SystemRoles.SuperAdmin)]
    public async Task<IActionResult> GetSuperAdminDashboard()
    {
        var dashboard = await _dashboardService.GetSuperAdminDashboardAsync();
        return Ok(dashboard);
    }

    /// <summary>
    /// Get dashboard for Reseller Admin
    /// </summary>
    [HttpGet("reselleradmin")]
    [Authorize(Roles = $"{SystemRoles.SuperAdmin},{SystemRoles.ResellerAdmin}")]
    public async Task<IActionResult> GetResellerAdminDashboard([FromQuery] int? resellerId = null)
    {
        // Get reseller ID from token if not provided
        var resellerIdToUse = resellerId;
        if (!resellerIdToUse.HasValue)
        {
            var resellerClaim = User.FindFirst("ResellerId")?.Value;
            if (!string.IsNullOrEmpty(resellerClaim) && int.TryParse(resellerClaim, out var id))
            {
                resellerIdToUse = id;
            }
        }

        if (!resellerIdToUse.HasValue)
        {
            return BadRequest(new { message = "Reseller ID is required" });
        }

        // Check access - Reseller Admin can only see their own reseller
        if (User.IsInRole(SystemRoles.ResellerAdmin) && !User.IsInRole(SystemRoles.SuperAdmin))
        {
            var userResellerId = User.FindFirst("ResellerId")?.Value;
            if (userResellerId != resellerIdToUse.ToString())
            {
                return Forbid();
            }
        }

        var dashboard = await _dashboardService.GetResellerAdminDashboardAsync(resellerIdToUse.Value);
        return Ok(dashboard);
    }

    /// <summary>
    /// Get dashboard for Supervisor
    /// </summary>
    [HttpGet("supervisor")]
    [Authorize(Roles = $"{SystemRoles.SuperAdmin},{SystemRoles.ResellerAdmin},{SystemRoles.Supervisor}")]
    public async Task<IActionResult> GetSupervisorDashboard([FromQuery] int? resellerId = null)
    {
        var resellerIdToUse = resellerId;
        
        // Non-SuperAdmin users can only see their own reseller's data
        if (!User.IsInRole(SystemRoles.SuperAdmin))
        {
            var userResellerId = User.FindFirst("ResellerId")?.Value;
            if (!string.IsNullOrEmpty(userResellerId) && int.TryParse(userResellerId, out var id))
            {
                resellerIdToUse = id;
            }
        }

        var dashboard = await _dashboardService.GetSupervisorDashboardAsync(resellerIdToUse);
        return Ok(dashboard);
    }

    /// <summary>
    /// Get dashboard for Technician
    /// </summary>
    [HttpGet("technician")]
    [Authorize]
    public async Task<IActionResult> GetTechnicianDashboard([FromQuery] int? technicianId = null)
    {
        var technicianIdToUse = technicianId;

        // Get technician ID from token if not provided
        if (!technicianIdToUse.HasValue)
        {
            var techClaim = User.FindFirst("TechnicianId")?.Value;
            if (!string.IsNullOrEmpty(techClaim) && int.TryParse(techClaim, out var id))
            {
                technicianIdToUse = id;
            }
        }

        if (!technicianIdToUse.HasValue)
        {
            return BadRequest(new { message = "Technician ID is required" });
        }

        // Technicians can only see their own dashboard
        if (User.IsInRole(SystemRoles.Technician) && !User.IsInRole(SystemRoles.SuperAdmin) && !User.IsInRole(SystemRoles.ResellerAdmin))
        {
            var userTechId = User.FindFirst("TechnicianId")?.Value;
            if (userTechId != technicianIdToUse.ToString())
            {
                return Forbid();
            }
        }

        var dashboard = await _dashboardService.GetTechnicianDashboardAsync(technicianIdToUse.Value);
        return Ok(dashboard);
    }

    /// <summary>
    /// Get appropriate dashboard based on user's role
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        if (User.IsInRole(SystemRoles.SuperAdmin))
        {
            return await GetSuperAdminDashboard();
        }
        else if (User.IsInRole(SystemRoles.ResellerAdmin))
        {
            return await GetResellerAdminDashboard();
        }
        else if (User.IsInRole(SystemRoles.Supervisor))
        {
            return await GetSupervisorDashboard();
        }
        else if (User.IsInRole(SystemRoles.Technician))
        {
            return await GetTechnicianDashboard();
        }

        return BadRequest(new { message = "No dashboard available for your role" });
    }
}

