using Microsoft.EntityFrameworkCore;
using TelematicsDataConsole.Core.DTOs;
using TelematicsDataConsole.Core.DTOs.Tag;
using TelematicsDataConsole.Core.Entities;
using TelematicsDataConsole.Core.Interfaces.Services;
using TelematicsDataConsole.Infrastructure.Data;

namespace TelematicsDataConsole.Infrastructure.Services;

public class TagService : ITagService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public TagService(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<PagedResult<TagDto>> GetAllAsync(int page = 1, int pageSize = 20, string? search = null, short? scope = null, short? status = null)
    {
        var query = _context.Tags
            .Include(t => t.Reseller)
            .Include(t => t.User)
            .Include(t => t.TagItems)
            .AsQueryable();

        if (scope.HasValue)
            query = query.Where(t => t.Scope == scope.Value);

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(t => t.TagName.Contains(search) || (t.Description != null && t.Description.Contains(search)));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TagDto
            {
                TagId = t.TagId,
                TagName = t.TagName,
                Description = t.Description,
                Scope = t.Scope,
                ResellerId = t.ResellerId,
                ResellerName = t.Reseller != null ? t.Reseller.CompanyName : null,
                UserId = t.UserId,
                UserName = t.User != null ? t.User.Username : null,
                Color = t.Color,
                Status = t.Status,
                ItemCount = t.TagItems.Count,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToListAsync();

        return new PagedResult<TagDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<TagDto?> GetByIdAsync(int id)
    {
        return await _context.Tags
            .Include(t => t.Reseller)
            .Include(t => t.User)
            .Include(t => t.TagItems)
            .Where(t => t.TagId == id)
            .Select(t => new TagDto
            {
                TagId = t.TagId,
                TagName = t.TagName,
                Description = t.Description,
                Scope = t.Scope,
                ResellerId = t.ResellerId,
                ResellerName = t.Reseller != null ? t.Reseller.CompanyName : null,
                UserId = t.UserId,
                UserName = t.User != null ? t.User.Username : null,
                Color = t.Color,
                Status = t.Status,
                ItemCount = t.TagItems.Count,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<TagDto> CreateAsync(CreateTagDto dto, int createdBy)
    {
        var tag = new Tag
        {
            TagName = dto.TagName,
            Description = dto.Description,
            Scope = dto.Scope,
            ResellerId = dto.ResellerId,
            UserId = dto.UserId,
            Color = dto.Color,
            Status = dto.Status,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };

        await _context.Tags.AddAsync(tag);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(createdBy, AuditActions.Create, "Tag", tag.TagId.ToString(), null, dto);
        return (await GetByIdAsync(tag.TagId))!;
    }

    public async Task<TagDto> UpdateAsync(int id, UpdateTagDto dto, int updatedBy)
    {
        var tag = await _context.Tags.FindAsync(id)
            ?? throw new KeyNotFoundException("Tag not found");

        if (dto.TagName != null) tag.TagName = dto.TagName;
        if (dto.Description != null) tag.Description = dto.Description;
        if (dto.Scope.HasValue) tag.Scope = dto.Scope.Value;
        if (dto.ResellerId.HasValue) tag.ResellerId = dto.ResellerId;
        if (dto.UserId.HasValue) tag.UserId = dto.UserId;
        if (dto.Color != null) tag.Color = dto.Color;
        if (dto.Status.HasValue) tag.Status = dto.Status.Value;
        tag.UpdatedBy = updatedBy;
        tag.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _auditService.LogAsync(updatedBy, AuditActions.Update, "Tag", id.ToString());
        return (await GetByIdAsync(id))!;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var tag = await _context.Tags.FindAsync(id);
        if (tag == null) return false;

        _context.Tags.Remove(tag);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<TagDto>> GetByResellerIdAsync(int resellerId)
    {
        return await _context.Tags
            .Where(t => t.ResellerId == resellerId || t.Scope == (short)TagScope.Global)
            .Select(t => new TagDto
            {
                TagId = t.TagId,
                TagName = t.TagName,
                Description = t.Description,
                Scope = t.Scope,
                Color = t.Color,
                Status = t.Status
            })
            .ToListAsync();
    }

    public async Task<List<TagDto>> GetByUserIdAsync(int userId)
    {
        return await _context.Tags
            .Where(t => t.UserId == userId || t.Scope == (short)TagScope.Global)
            .Select(t => new TagDto
            {
                TagId = t.TagId,
                TagName = t.TagName,
                Description = t.Description,
                Scope = t.Scope,
                Color = t.Color,
                Status = t.Status
            })
            .ToListAsync();
    }

    public async Task<List<TagItemDto>> GetTagItemsAsync(int tagId, short? entityType = null)
    {
        var query = _context.TagItems
            .Include(ti => ti.Tag)
            .Where(ti => ti.TagId == tagId);

        if (entityType.HasValue)
            query = query.Where(ti => ti.EntityType == entityType.Value);

        return await query
            .Select(ti => new TagItemDto
            {
                TagItemId = ti.TagItemId,
                TagId = ti.TagId,
                TagName = ti.Tag.TagName,
                EntityType = ti.EntityType,
                EntityId = ti.EntityId,
                EntityIdentifier = ti.EntityIdentifier,
                CreatedAt = ti.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<TagItemDto> AddTagItemAsync(CreateTagItemDto dto)
    {
        // Check if item already exists
        var exists = await _context.TagItems.AnyAsync(ti =>
            ti.TagId == dto.TagId &&
            ti.EntityType == dto.EntityType &&
            ti.EntityId == dto.EntityId);

        if (exists)
            throw new InvalidOperationException("This item is already tagged");

        var tagItem = new TagItem
        {
            TagId = dto.TagId,
            EntityType = dto.EntityType,
            EntityId = dto.EntityId,
            EntityIdentifier = dto.EntityIdentifier
        };

        await _context.TagItems.AddAsync(tagItem);
        await _context.SaveChangesAsync();

        var tag = await _context.Tags.FindAsync(dto.TagId);
        return new TagItemDto
        {
            TagItemId = tagItem.TagItemId,
            TagId = tagItem.TagId,
            TagName = tag?.TagName,
            EntityType = tagItem.EntityType,
            EntityId = tagItem.EntityId,
            EntityIdentifier = tagItem.EntityIdentifier,
            CreatedAt = tagItem.CreatedAt
        };
    }

    public async Task<List<TagItemDto>> BulkAddTagItemsAsync(BulkAddTagItemsDto dto)
    {
        var existingItems = await _context.TagItems
            .Where(ti => ti.TagId == dto.TagId && ti.EntityType == dto.EntityType)
            .Select(ti => ti.EntityId)
            .ToListAsync();

        var items = dto.Items
            .Where(item => !existingItems.Contains(item.EntityId))
            .Select(item => new TagItem
            {
                TagId = dto.TagId,
                EntityType = dto.EntityType,
                EntityId = item.EntityId,
                EntityIdentifier = item.EntityIdentifier
            }).ToList();

        if (items.Any())
        {
            await _context.TagItems.AddRangeAsync(items);
            await _context.SaveChangesAsync();
        }

        var tag = await _context.Tags.FindAsync(dto.TagId);
        return items.Select(ti => new TagItemDto
        {
            TagItemId = ti.TagItemId,
            TagId = ti.TagId,
            TagName = tag?.TagName,
            EntityType = ti.EntityType,
            EntityId = ti.EntityId,
            EntityIdentifier = ti.EntityIdentifier,
            CreatedAt = ti.CreatedAt
        }).ToList();
    }

    public async Task<bool> RemoveTagItemAsync(int tagItemId)
    {
        var tagItem = await _context.TagItems.FindAsync(tagItemId);
        if (tagItem == null) return false;

        _context.TagItems.Remove(tagItem);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveTagItemByEntityAsync(int tagId, short entityType, long entityId)
    {
        var tagItem = await _context.TagItems.FirstOrDefaultAsync(ti =>
            ti.TagId == tagId && ti.EntityType == entityType && ti.EntityId == entityId);
        if (tagItem == null) return false;

        _context.TagItems.Remove(tagItem);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<TagDto>> GetTagsByEntityAsync(short entityType, long entityId)
    {
        return await _context.TagItems
            .Where(ti => ti.EntityType == entityType && ti.EntityId == entityId)
            .Include(ti => ti.Tag)
            .Select(ti => new TagDto
            {
                TagId = ti.Tag.TagId,
                TagName = ti.Tag.TagName,
                Description = ti.Tag.Description,
                Scope = ti.Tag.Scope,
                Color = ti.Tag.Color,
                Status = ti.Tag.Status
            })
            .ToListAsync();
    }
}

