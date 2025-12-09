using TelematicsDataConsole.Core.DTOs;
using TelematicsDataConsole.Core.DTOs.Technician;

namespace TelematicsDataConsole.Core.Interfaces.Services;

public interface ITechnicianService
{
    Task<PagedResult<TechnicianDto>> GetAllAsync(TechnicianFilterDto filter);
    Task<TechnicianDto?> GetByIdAsync(int id);
    Task<TechnicianDto?> GetByUserIdAsync(int userId);
    Task<TechnicianDto> CreateAsync(CreateTechnicianDto dto, int createdBy);
    Task<TechnicianDto> UpdateAsync(int id, UpdateTechnicianDto dto, int updatedBy);
    Task<bool> DeactivateAsync(int id, int updatedBy);
    Task<bool> ActivateAsync(int id, int updatedBy);
    Task<IEnumerable<TechnicianDto>> GetByResellerAsync(int resellerId);
    Task<TechnicianStatsDto> GetStatsAsync(int technicianId, DateTime? from = null, DateTime? to = null);
}

