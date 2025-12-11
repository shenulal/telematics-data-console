using Microsoft.EntityFrameworkCore;
using TelematicsDataConsole.Core.DTOs;
using TelematicsDataConsole.Core.DTOs.ImeiRestriction;
using TelematicsDataConsole.Core.Entities;
using TelematicsDataConsole.Core.Interfaces.Services;
using TelematicsDataConsole.Infrastructure.Data;

namespace TelematicsDataConsole.Infrastructure.Services;

public class ImeiRestrictionService : IImeiRestrictionService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public ImeiRestrictionService(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<PagedResult<ImeiRestrictionDto>> GetByTechnicianAsync(int technicianId, int page = 1, int pageSize = 20)
    {
        var query = _context.ImeiRestrictions
            .Include(r => r.Technician).ThenInclude(t => t.User)
            .Include(r => r.Tag)
            .Where(r => r.TechnicianId == technicianId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => MapToDto(r))
            .ToListAsync();

        return new PagedResult<ImeiRestrictionDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ImeiRestrictionDto?> GetByIdAsync(int id)
    {
        var restriction = await _context.ImeiRestrictions
            .Include(r => r.Technician).ThenInclude(t => t.User)
            .Include(r => r.Tag)
            .FirstOrDefaultAsync(r => r.RestrictionId == id);

        return restriction != null ? MapToDto(restriction) : null;
    }

    public async Task<ImeiRestrictionDto> CreateAsync(CreateImeiRestrictionDto dto, int createdBy)
    {
        var restriction = new ImeiRestriction
        {
            TechnicianId = dto.TechnicianId,
            DeviceId = dto.DeviceId,
            TagId = dto.TagId,
            AccessType = dto.AccessType,
            Priority = dto.Priority,
            Reason = dto.Reason,
            IsPermanent = dto.IsPermanent,
            ValidFrom = dto.ValidFrom ?? DateTime.UtcNow,
            ValidUntil = dto.ValidUntil,
            Notes = dto.Notes,
            Status = (int)RestrictionStatus.Active,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };

        await _context.ImeiRestrictions.AddAsync(restriction);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(createdBy, AuditActions.Create, "ImeiRestriction", restriction.RestrictionId.ToString(), null, dto);

        return (await GetByIdAsync(restriction.RestrictionId))!;
    }

    public async Task<ImeiRestrictionDto> UpdateAsync(int id, UpdateImeiRestrictionDto dto, int updatedBy)
    {
        var restriction = await _context.ImeiRestrictions.FindAsync(id)
            ?? throw new KeyNotFoundException("Restriction not found");

        if (dto.DeviceId.HasValue) restriction.DeviceId = dto.DeviceId;
        if (dto.TagId.HasValue) restriction.TagId = dto.TagId;
        if (dto.AccessType.HasValue) restriction.AccessType = dto.AccessType;
        if (dto.Priority.HasValue) restriction.Priority = dto.Priority;
        if (dto.Reason != null) restriction.Reason = dto.Reason;
        if (dto.IsPermanent.HasValue) restriction.IsPermanent = dto.IsPermanent;
        if (dto.ValidFrom.HasValue) restriction.ValidFrom = dto.ValidFrom;
        if (dto.ValidUntil.HasValue) restriction.ValidUntil = dto.ValidUntil;
        if (dto.Notes != null) restriction.Notes = dto.Notes;
        if (dto.Status.HasValue) restriction.Status = dto.Status.Value;

        restriction.UpdatedBy = updatedBy;
        restriction.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _auditService.LogAsync(updatedBy, AuditActions.Update, "ImeiRestriction", id.ToString());

        return (await GetByIdAsync(id))!;
    }

    public async Task<bool> DeleteAsync(int id, int deletedBy = 0)
    {
        var restriction = await _context.ImeiRestrictions.FindAsync(id);
        if (restriction == null) return false;

        var oldValues = new { restriction.RestrictionId, restriction.TechnicianId, restriction.DeviceId, restriction.AccessType };
        _context.ImeiRestrictions.Remove(restriction);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(deletedBy, AuditActions.Delete, "ImeiRestriction", id.ToString(), oldValues, null);
        return true;
    }

    public async Task<bool> IsDeviceRestrictedAsync(int technicianId, int deviceId)
    {
        var now = DateTime.UtcNow;
        return await _context.ImeiRestrictions.AnyAsync(r =>
            r.TechnicianId == technicianId &&
            r.DeviceId == deviceId &&
            r.Status == (int)RestrictionStatus.Active &&
            r.AccessType == (short)AccessType.Deny &&
            (r.IsPermanent == true || (r.ValidFrom <= now && r.ValidUntil >= now)));
    }

    public async Task<IEnumerable<ImeiRestrictionDto>> GetActiveRestrictionsAsync(int technicianId)
    {
        var now = DateTime.UtcNow;
        return await _context.ImeiRestrictions
            .Include(r => r.Tag)
            .Where(r => r.TechnicianId == technicianId &&
                       r.Status == (int)RestrictionStatus.Active &&
                       (r.IsPermanent == true || (r.ValidFrom <= now && r.ValidUntil >= now)))
            .Select(r => MapToDto(r))
            .ToListAsync();
    }

    private static ImeiRestrictionDto MapToDto(ImeiRestriction r) => new()
    {
        RestrictionId = r.RestrictionId,
        TechnicianId = r.TechnicianId,
        TechnicianName = r.Technician?.User?.FullName ?? r.Technician?.User?.Username,
        DeviceId = r.DeviceId,
        TagId = r.TagId,
        TagName = r.Tag?.TagName,
        AccessType = r.AccessType,
        Priority = r.Priority,
        Reason = r.Reason,
        IsPermanent = r.IsPermanent,
        ValidFrom = r.ValidFrom,
        ValidUntil = r.ValidUntil,
        Notes = r.Notes,
        Status = r.Status,
        CreatedAt = r.CreatedAt
    };
}

