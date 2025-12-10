using TelematicsDataConsole.Core.DTOs;
using TelematicsDataConsole.Core.DTOs.User;

namespace TelematicsDataConsole.Core.Interfaces.Services;

public interface IUserService
{
    Task<PagedResult<UserDto>> GetAllAsync(int page = 1, int pageSize = 20, string? search = null, short? status = null, int? resellerId = null, bool excludeSuperAdmin = false);
    Task<UserDto?> GetByIdAsync(int id);
    Task<UserDto?> GetByUsernameAsync(string username);
    Task<UserDto> CreateAsync(CreateUserDto dto, int createdBy);
    Task<UserDto> UpdateAsync(int id, UpdateUserDto dto, int updatedBy);
    Task<bool> DeleteAsync(int id);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto);
    Task<bool> ResetPasswordAsync(int userId, ResetPasswordDto dto, int adminId);
    Task<List<UserDto>> GetByResellerIdAsync(int resellerId);
    Task<bool> IsSuperAdminAsync(int userId);
}

