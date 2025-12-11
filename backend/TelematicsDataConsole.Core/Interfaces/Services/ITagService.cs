using TelematicsDataConsole.Core.DTOs;
using TelematicsDataConsole.Core.DTOs.Tag;

namespace TelematicsDataConsole.Core.Interfaces.Services;

public interface ITagService
{
    Task<PagedResult<TagDto>> GetAllAsync(int page = 1, int pageSize = 20, string? search = null, short? scope = null, short? status = null);
    Task<TagDto?> GetByIdAsync(int id);
    Task<TagDto> CreateAsync(CreateTagDto dto, int createdBy);
    Task<TagDto> UpdateAsync(int id, UpdateTagDto dto, int updatedBy);
    Task<bool> DeleteAsync(int id, int deletedBy = 0);
    Task<List<TagDto>> GetByResellerIdAsync(int resellerId);
    Task<List<TagDto>> GetByUserIdAsync(int userId);

    // Tag Items
    Task<List<TagItemDto>> GetTagItemsAsync(int tagId, short? entityType = null);
    Task<TagItemDto> AddTagItemAsync(CreateTagItemDto dto);
    Task<List<TagItemDto>> BulkAddTagItemsAsync(BulkAddTagItemsDto dto);
    Task<bool> RemoveTagItemAsync(int tagItemId);
    Task<bool> RemoveTagItemByEntityAsync(int tagId, short entityType, long entityId);
    Task<List<TagDto>> GetTagsByEntityAsync(short entityType, long entityId);
}

