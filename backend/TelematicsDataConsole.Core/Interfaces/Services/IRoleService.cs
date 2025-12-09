using TelematicsDataConsole.Core.DTOs.Role;

namespace TelematicsDataConsole.Core.Interfaces.Services;

public interface IRoleService
{
    Task<List<RoleDto>> GetAllAsync(int? resellerId = null);
    Task<RoleDto?> GetByIdAsync(int id);
    Task<RoleDto?> GetByNameAsync(string name);
    Task<RoleDto> CreateAsync(CreateRoleDto dto, int createdBy, int? resellerId = null);
    Task<RoleDto> UpdateAsync(int id, UpdateRoleDto dto, int updatedBy);
    Task<bool> DeleteAsync(int id);
    Task<bool> AssignPermissionsAsync(int roleId, List<int> permissionIds, int updatedBy);
    Task<List<PermissionDto>> GetUserPermissionsAsync(int userId);
    Task<bool> CanAccessRoleAsync(int roleId, int userId, int? resellerId);
}

public interface IPermissionService
{
    Task<List<PermissionDto>> GetAllAsync(string? module = null);
    Task<PermissionDto?> GetByIdAsync(int id);
    Task<PermissionDto> CreateAsync(CreatePermissionDto dto);
    Task<PermissionDto> UpdateAsync(int id, UpdatePermissionDto dto);
    Task<bool> DeleteAsync(int id);
    Task<List<string>> GetModulesAsync();
}

