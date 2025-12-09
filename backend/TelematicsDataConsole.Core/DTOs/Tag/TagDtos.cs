namespace TelematicsDataConsole.Core.DTOs.Tag;

public class TagDto
{
    public int TagId { get; set; }
    public string TagName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public short Scope { get; set; }
    public string ScopeText => Scope switch
    {
        0 => "Global",
        1 => "Reseller",
        2 => "User",
        _ => "Unknown"
    };
    public int? ResellerId { get; set; }
    public string? ResellerName { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string? Color { get; set; }
    public short Status { get; set; }
    public string StatusText => Status switch
    {
        0 => "Inactive",
        1 => "Active",
        _ => "Unknown"
    };
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateTagDto
{
    public string TagName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public short Scope { get; set; } = 0;
    public int? ResellerId { get; set; }
    public int? UserId { get; set; }
    public string? Color { get; set; }
    public short Status { get; set; } = 1;
}

public class UpdateTagDto
{
    public string? TagName { get; set; }
    public string? Description { get; set; }
    public short? Scope { get; set; }
    public int? ResellerId { get; set; }
    public int? UserId { get; set; }
    public string? Color { get; set; }
    public short? Status { get; set; }
}

public class TagItemDto
{
    public int TagItemId { get; set; }
    public int TagId { get; set; }
    public string? TagName { get; set; }
    public short EntityType { get; set; }
    public string EntityTypeName => EntityType switch
    {
        1 => "Device",
        2 => "Technician",
        3 => "Reseller",
        4 => "User",
        _ => "Unknown"
    };
    public long EntityId { get; set; }
    public string? EntityIdentifier { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateTagItemDto
{
    public int TagId { get; set; }
    public short EntityType { get; set; }
    public long EntityId { get; set; }
    public string? EntityIdentifier { get; set; }
}

public class BulkAddTagItemsDto
{
    public int TagId { get; set; }
    public short EntityType { get; set; }
    public List<BulkTagItemEntry> Items { get; set; } = new();
}

public class BulkTagItemEntry
{
    public long EntityId { get; set; }
    public string? EntityIdentifier { get; set; }
}

