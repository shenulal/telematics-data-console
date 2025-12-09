using TelematicsDataConsole.Core.DTOs;
using TelematicsDataConsole.Core.DTOs.Reseller;

namespace TelematicsDataConsole.Core.Interfaces.Services;

public interface IResellerService
{
    Task<PagedResult<ResellerDto>> GetAllAsync(ResellerFilterDto filter);
    Task<ResellerDto?> GetByIdAsync(int id);
    Task<ResellerDto> CreateAsync(CreateResellerDto dto, int createdBy);
    Task<ResellerDto> UpdateAsync(int id, UpdateResellerDto dto, int updatedBy);
    Task<bool> DeactivateAsync(int id, int updatedBy);
    Task<bool> ActivateAsync(int id, int updatedBy);
    Task<ResellerStatsDto> GetStatsAsync(int resellerId, DateTime? from = null, DateTime? to = null);
    Task<ResellerStatusUpdateResultDto> UpdateStatusWithCascadeAsync(int id, short newStatus, int updatedBy);
}

