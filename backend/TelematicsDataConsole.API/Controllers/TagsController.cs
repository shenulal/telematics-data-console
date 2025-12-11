using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TelematicsDataConsole.Core.DTOs.Tag;
using TelematicsDataConsole.Core.Entities;
using TelematicsDataConsole.Core.Interfaces.Services;

namespace TelematicsDataConsole.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TagsController : ControllerBase
{
    private readonly ITagService _tagService;
    private readonly ILogger<TagsController> _logger;

    public TagsController(ITagService tagService, ILogger<TagsController> logger)
    {
        _tagService = tagService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] short? scope = null, [FromQuery] short? status = null)
    {
        var result = await _tagService.GetAllAsync(page, pageSize, search, scope, status);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var tag = await _tagService.GetByIdAsync(id);
        if (tag == null)
            return NotFound(new { message = "Tag not found" });
        return Ok(tag);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTagDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        var resellerId = GetCurrentResellerId();

        // Validate scope based on user role
        if (User.IsInRole(SystemRoles.SuperAdmin))
        {
            // SuperAdmin can create any scope
        }
        else if (User.IsInRole(SystemRoles.ResellerAdmin))
        {
            // Reseller Admin can only create Reseller (1) or User (2) scope
            if (dto.Scope == 0) // Global
                return BadRequest(new { message = "Reseller Admin can only create tags with Reseller or User scope" });

            // Set reseller ID for Reseller scope tags
            if (dto.Scope == 1) // Reseller
                dto.ResellerId = resellerId;
            else if (dto.Scope == 2) // User
                dto.UserId = userId;
        }
        else
        {
            // Regular users can only create User scope (2)
            if (dto.Scope != 2)
                return BadRequest(new { message = "You can only create tags with User scope" });
            dto.Scope = 2;
            dto.UserId = userId;
        }

        var tag = await _tagService.CreateAsync(dto, userId);
        _logger.LogInformation("Tag created: {TagId} by {UserId}", tag.TagId, userId);
        return CreatedAtAction(nameof(GetById), new { id = tag.TagId }, tag);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTagDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userId = GetCurrentUserId();

            // Check if non-SuperAdmin is trying to edit a Global tag
            if (!User.IsInRole(SystemRoles.SuperAdmin))
            {
                var existingTag = await _tagService.GetByIdAsync(id);
                if (existingTag != null && existingTag.Scope == 0) // Global scope
                {
                    return Forbid();
                }
            }

            var tag = await _tagService.UpdateAsync(id, dto, userId);
            _logger.LogInformation("Tag updated: {TagId} by {UserId}", id, userId);
            return Ok(tag);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Tag not found" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        // Check if non-SuperAdmin is trying to delete a Global tag
        if (!User.IsInRole(SystemRoles.SuperAdmin))
        {
            var existingTag = await _tagService.GetByIdAsync(id);
            if (existingTag != null && existingTag.Scope == 0) // Global scope
            {
                return Forbid();
            }
        }

        var userId = GetCurrentUserId();
        var result = await _tagService.DeleteAsync(id, userId);
        if (!result)
            return NotFound(new { message = "Tag not found" });

        _logger.LogInformation("Tag deleted: {TagId}", id);
        return Ok(new { message = "Tag deleted successfully" });
    }

    [HttpGet("reseller/{resellerId}")]
    public async Task<IActionResult> GetByReseller(int resellerId)
    {
        var tags = await _tagService.GetByResellerIdAsync(resellerId);
        return Ok(tags);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUser(int userId)
    {
        var tags = await _tagService.GetByUserIdAsync(userId);
        return Ok(tags);
    }

    // Tag Items
    /// <summary>
    /// Get items for a tag, optionally filtered by entity type
    /// </summary>
    [HttpGet("{tagId}/items")]
    public async Task<IActionResult> GetTagItems(int tagId, [FromQuery] short? entityType = null)
    {
        var items = await _tagService.GetTagItemsAsync(tagId, entityType);
        return Ok(items);
    }

    /// <summary>
    /// Add an item to a tag
    /// </summary>
    [HttpPost("{tagId}/items")]
    [Authorize(Roles = SystemRoles.SuperAdmin)]
    public async Task<IActionResult> AddTagItem(int tagId, [FromBody] CreateTagItemDto dto)
    {
        try
        {
            dto.TagId = tagId;
            var item = await _tagService.AddTagItemAsync(dto);
            return Ok(item);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Bulk add items to a tag
    /// </summary>
    [HttpPost("{tagId}/items/bulk")]
    [Authorize(Roles = SystemRoles.SuperAdmin)]
    public async Task<IActionResult> BulkAddTagItems(int tagId, [FromBody] BulkAddTagItemsDto dto)
    {
        dto.TagId = tagId;
        var items = await _tagService.BulkAddTagItemsAsync(dto);
        return Ok(items);
    }

    /// <summary>
    /// Remove a tag item by ID
    /// </summary>
    [HttpDelete("items/{tagItemId}")]
    [Authorize(Roles = SystemRoles.SuperAdmin)]
    public async Task<IActionResult> RemoveTagItem(int tagItemId)
    {
        var result = await _tagService.RemoveTagItemAsync(tagItemId);
        if (!result)
            return NotFound(new { message = "Tag item not found" });
        return Ok(new { message = "Tag item removed successfully" });
    }

    /// <summary>
    /// Remove a tag item by entity type and entity ID
    /// </summary>
    [HttpDelete("{tagId}/items/{entityType}/{entityId}")]
    [Authorize(Roles = SystemRoles.SuperAdmin)]
    public async Task<IActionResult> RemoveTagItemByEntity(int tagId, short entityType, long entityId)
    {
        var result = await _tagService.RemoveTagItemByEntityAsync(tagId, entityType, entityId);
        if (!result)
            return NotFound(new { message = "Tag item not found" });
        return Ok(new { message = "Tag item removed successfully" });
    }

    /// <summary>
    /// Get all tags assigned to a specific entity
    /// </summary>
    [HttpGet("entity/{entityType}/{entityId}")]
    public async Task<IActionResult> GetTagsByEntity(short entityType, long entityId)
    {
        var tags = await _tagService.GetTagsByEntityAsync(entityType, entityId);
        return Ok(tags);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var id) ? id : 0;
    }

    private int? GetCurrentResellerId()
    {
        var resellerIdClaim = User.FindFirst("ResellerId")?.Value;
        return int.TryParse(resellerIdClaim, out var id) ? id : null;
    }
}

