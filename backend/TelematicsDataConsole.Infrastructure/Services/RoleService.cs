using Microsoft.EntityFrameworkCore;
using TelematicsDataConsole.Core.DTOs.Role;
using TelematicsDataConsole.Core.Entities;
using TelematicsDataConsole.Core.Interfaces.Services;
using TelematicsDataConsole.Infrastructure.Data;

namespace TelematicsDataConsole.Infrastructure.Services;

public class RoleService : IRoleService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public RoleService(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<List<RoleDto>> GetAllAsync(int? resellerId = null)
    {
        var query = _context.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .Include(r => r.Reseller)
            .AsQueryable();

        // Filter by reseller: show system roles + roles created by the reseller
        if (resellerId.HasValue)
        {
            query = query.Where(r => r.IsSystemRole || r.ResellerId == resellerId.Value);
        }

        return await query
            .OrderBy(r => r.RoleName)
            .Select(r => new RoleDto
            {
                RoleId = r.RoleId,
                RoleName = r.RoleName,
                Description = r.Description,
                IsSystemRole = r.IsSystemRole,
                ResellerId = r.ResellerId,
                ResellerName = r.Reseller != null ? r.Reseller.CompanyName : null,
                Permissions = r.RolePermissions.Select(rp => new PermissionDto
                {
                    PermissionId = rp.Permission.PermissionId,
                    PermissionName = rp.Permission.PermissionName,
                    Description = rp.Permission.Description,
                    Module = rp.Permission.Module
                }).ToList(),
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<RoleDto?> GetByIdAsync(int id)
    {
        return await _context.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .Include(r => r.Reseller)
            .Where(r => r.RoleId == id)
            .Select(r => new RoleDto
            {
                RoleId = r.RoleId,
                RoleName = r.RoleName,
                Description = r.Description,
                IsSystemRole = r.IsSystemRole,
                ResellerId = r.ResellerId,
                ResellerName = r.Reseller != null ? r.Reseller.CompanyName : null,
                Permissions = r.RolePermissions.Select(rp => new PermissionDto
                {
                    PermissionId = rp.Permission.PermissionId,
                    PermissionName = rp.Permission.PermissionName,
                    Description = rp.Permission.Description,
                    Module = rp.Permission.Module
                }).ToList(),
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<RoleDto?> GetByNameAsync(string name)
    {
        return await _context.Roles
            .Include(r => r.Reseller)
            .Where(r => r.RoleName == name)
            .Select(r => new RoleDto
            {
                RoleId = r.RoleId,
                RoleName = r.RoleName,
                Description = r.Description,
                IsSystemRole = r.IsSystemRole,
                ResellerId = r.ResellerId,
                ResellerName = r.Reseller != null ? r.Reseller.CompanyName : null,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<RoleDto> CreateAsync(CreateRoleDto dto, int createdBy, int? resellerId = null)
    {
        if (await _context.Roles.AnyAsync(r => r.RoleName == dto.RoleName))
            throw new InvalidOperationException("Role name already exists");

        var role = new Role
        {
            RoleName = dto.RoleName,
            Description = dto.Description,
            IsSystemRole = false,
            ResellerId = resellerId,
            CreatedBy = createdBy
        };

        await _context.Roles.AddAsync(role);
        await _context.SaveChangesAsync();

        if (dto.PermissionIds.Count > 0)
        {
            foreach (var permId in dto.PermissionIds)
            {
                await _context.RolePermissions.AddAsync(new RolePermission
                {
                    RoleId = role.RoleId,
                    PermissionId = permId,
                    CreatedBy = createdBy
                });
            }
            await _context.SaveChangesAsync();
        }

        await _auditService.LogAsync(createdBy, AuditActions.Create, "Role", role.RoleId.ToString(), null, dto);
        return (await GetByIdAsync(role.RoleId))!;
    }

    public async Task<RoleDto> UpdateAsync(int id, UpdateRoleDto dto, int updatedBy)
    {
        var role = await _context.Roles.Include(r => r.RolePermissions).FirstOrDefaultAsync(r => r.RoleId == id)
            ?? throw new KeyNotFoundException("Role not found");

        if (role.IsSystemRole)
            throw new InvalidOperationException("Cannot modify system roles");

        if (dto.RoleName != null && dto.RoleName != role.RoleName)
        {
            if (await _context.Roles.AnyAsync(r => r.RoleName == dto.RoleName && r.RoleId != id))
                throw new InvalidOperationException("Role name already exists");
            role.RoleName = dto.RoleName;
        }

        if (dto.Description != null) role.Description = dto.Description;
        role.UpdatedAt = DateTime.UtcNow;

        if (dto.PermissionIds != null)
        {
            _context.RolePermissions.RemoveRange(role.RolePermissions);
            foreach (var permId in dto.PermissionIds)
            {
                await _context.RolePermissions.AddAsync(new RolePermission
                {
                    RoleId = role.RoleId,
                    PermissionId = permId,
                    CreatedBy = updatedBy
                });
            }
        }

        await _context.SaveChangesAsync();
        await _auditService.LogAsync(updatedBy, AuditActions.Update, "Role", role.RoleId.ToString(), null, dto);
        return (await GetByIdAsync(id))!;
    }

    public async Task<bool> DeleteAsync(int id, int deletedBy = 0)
    {
        var role = await _context.Roles.FindAsync(id);
        if (role == null) return false;
        if (role.IsSystemRole) throw new InvalidOperationException("Cannot delete system roles");

        var oldValues = new { role.RoleId, role.RoleName, role.Description };
        _context.Roles.Remove(role);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(deletedBy, AuditActions.Delete, "Role", id.ToString(), oldValues, null);
        return true;
    }

    public async Task<bool> AssignPermissionsAsync(int roleId, List<int> permissionIds, int updatedBy)
    {
        var role = await _context.Roles.Include(r => r.RolePermissions).FirstOrDefaultAsync(r => r.RoleId == roleId);
        if (role == null) return false;

        _context.RolePermissions.RemoveRange(role.RolePermissions);
        foreach (var permId in permissionIds)
        {
            await _context.RolePermissions.AddAsync(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permId,
                CreatedBy = updatedBy
            });
        }
        role.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<PermissionDto>> GetUserPermissionsAsync(int userId)
    {
        var permissions = await _context.Users
            .Where(u => u.UserId == userId)
            .SelectMany(u => u.UserRoles)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission)
            .Distinct()
            .Select(p => new PermissionDto
            {
                PermissionId = p.PermissionId,
                PermissionName = p.PermissionName,
                Description = p.Description,
                Module = p.Module
            })
            .ToListAsync();

        return permissions;
    }

    public async Task<bool> CanAccessRoleAsync(int roleId, int userId, int? resellerId)
    {
        var role = await _context.Roles.FindAsync(roleId);
        if (role == null) return false;

        // System roles are accessible to all (for viewing)
        if (role.IsSystemRole) return true;

        // If user is not a reseller admin (no resellerId), they can access all roles
        if (!resellerId.HasValue) return true;

        // Reseller admin can only access roles created by their reseller
        return role.ResellerId == resellerId.Value;
    }
}

