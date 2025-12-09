using Microsoft.EntityFrameworkCore;
using TelematicsDataConsole.Core.DTOs.Role;
using TelematicsDataConsole.Core.Entities;
using TelematicsDataConsole.Core.Interfaces.Services;
using TelematicsDataConsole.Infrastructure.Data;

namespace TelematicsDataConsole.Infrastructure.Services;

public class PermissionService : IPermissionService
{
    private readonly ApplicationDbContext _context;

    public PermissionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PermissionDto>> GetAllAsync(string? module = null)
    {
        var query = _context.Permissions.AsQueryable();

        if (!string.IsNullOrEmpty(module))
            query = query.Where(p => p.Module == module);

        return await query
            .OrderBy(p => p.Module).ThenBy(p => p.PermissionName)
            .Select(p => new PermissionDto
            {
                PermissionId = p.PermissionId,
                PermissionName = p.PermissionName,
                Description = p.Description,
                Module = p.Module
            })
            .ToListAsync();
    }

    public async Task<PermissionDto?> GetByIdAsync(int id)
    {
        return await _context.Permissions
            .Where(p => p.PermissionId == id)
            .Select(p => new PermissionDto
            {
                PermissionId = p.PermissionId,
                PermissionName = p.PermissionName,
                Description = p.Description,
                Module = p.Module
            })
            .FirstOrDefaultAsync();
    }

    public async Task<PermissionDto> CreateAsync(CreatePermissionDto dto)
    {
        if (await _context.Permissions.AnyAsync(p => p.PermissionName == dto.PermissionName))
            throw new InvalidOperationException("Permission name already exists");

        var permission = new Permission
        {
            PermissionName = dto.PermissionName,
            Description = dto.Description,
            Module = dto.Module
        };

        await _context.Permissions.AddAsync(permission);
        await _context.SaveChangesAsync();

        return (await GetByIdAsync(permission.PermissionId))!;
    }

    public async Task<PermissionDto> UpdateAsync(int id, UpdatePermissionDto dto)
    {
        var permission = await _context.Permissions.FindAsync(id)
            ?? throw new KeyNotFoundException("Permission not found");

        if (dto.PermissionName != null && dto.PermissionName != permission.PermissionName)
        {
            if (await _context.Permissions.AnyAsync(p => p.PermissionName == dto.PermissionName && p.PermissionId != id))
                throw new InvalidOperationException("Permission name already exists");
            permission.PermissionName = dto.PermissionName;
        }

        if (dto.Description != null) permission.Description = dto.Description;
        if (dto.Module != null) permission.Module = dto.Module;

        await _context.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var permission = await _context.Permissions.FindAsync(id);
        if (permission == null) return false;

        _context.Permissions.Remove(permission);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<string>> GetModulesAsync()
    {
        return await _context.Permissions
            .Where(p => p.Module != null)
            .Select(p => p.Module!)
            .Distinct()
            .OrderBy(m => m)
            .ToListAsync();
    }
}

