using TelematicsDataConsole.Core.DTOs.Dashboard;

namespace TelematicsDataConsole.Core.Interfaces.Services;

public interface IDashboardService
{
    /// <summary>
    /// Get dashboard statistics for Super Admin
    /// </summary>
    Task<SuperAdminDashboardDto> GetSuperAdminDashboardAsync();

    /// <summary>
    /// Get dashboard statistics for Reseller Admin
    /// </summary>
    Task<ResellerAdminDashboardDto> GetResellerAdminDashboardAsync(int resellerId);

    /// <summary>
    /// Get dashboard statistics for Supervisor
    /// </summary>
    Task<SupervisorDashboardDto> GetSupervisorDashboardAsync(int? resellerId = null);

    /// <summary>
    /// Get dashboard statistics for Technician
    /// </summary>
    Task<TechnicianDashboardDto> GetTechnicianDashboardAsync(int technicianId);
}

