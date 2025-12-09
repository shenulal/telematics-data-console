using TelematicsDataConsole.Core.DTOs;
using TelematicsDataConsole.Core.DTOs.VerificationLog;

namespace TelematicsDataConsole.Core.Interfaces.Services;

public interface IVerificationLogService
{
    Task<PagedResult<VerificationLogDto>> GetAllAsync(VerificationLogFilterDto filter);
    Task<VerificationLogDto?> GetByIdAsync(int id);
    Task<VerificationLogDto> CreateAsync(CreateVerificationLogDto dto);
    Task<List<VerificationLogDto>> GetByTechnicianIdAsync(int technicianId, int limit = 50);
    Task<List<VerificationLogDto>> GetByDeviceIdAsync(int deviceId, int limit = 50);
    Task<VerificationStatisticsDto> GetStatisticsAsync(int? technicianId = null, DateTime? fromDate = null, DateTime? toDate = null);
}

