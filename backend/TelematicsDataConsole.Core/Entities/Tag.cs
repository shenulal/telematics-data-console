namespace TelematicsDataConsole.Core.Entities;

public class Tag
{
    public int TagId { get; set; }
    public string TagName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public short Scope { get; set; } = 1;
    public int? ResellerId { get; set; }
    public int? UserId { get; set; }
    public string? Color { get; set; }
    public short Status { get; set; } = 1;
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Reseller? Reseller { get; set; }
    public virtual User? User { get; set; }
    public virtual ICollection<TagItem> TagItems { get; set; } = new List<TagItem>();
    public virtual ICollection<ImeiRestriction> ImeiRestrictions { get; set; } = new List<ImeiRestriction>();
}

public enum TagScope : short
{
    Global = 0,
    Reseller = 1,
    User = 2
}

