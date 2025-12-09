using TelematicsDataConsole.Core.DTOs;
using TelematicsDataConsole.Core.DTOs.ImeiRestriction;

namespace TelematicsDataConsole.Core.Interfaces.Services;

public interface IImeiRestrictionService
{
    Task<PagedResult<ImeiRestrictionDto>> GetByTechnicianAsync(int technicianId, int page = 1, int pageSize = 20);
    Task<ImeiRestrictionDto?> GetByIdAsync(int id);
    Task<ImeiRestrictionDto> CreateAsync(CreateImeiRestrictionDto dto, int createdBy);
    Task<ImeiRestrictionDto> UpdateAsync(int id, UpdateImeiRestrictionDto dto, int updatedBy);
    Task<bool> DeleteAsync(int id);
    Task<bool> IsDeviceRestrictedAsync(int technicianId, int deviceId);
    Task<IEnumerable<ImeiRestrictionDto>> GetActiveRestrictionsAsync(int technicianId);
}

