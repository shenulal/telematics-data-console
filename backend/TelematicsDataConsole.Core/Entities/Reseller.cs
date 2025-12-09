namespace TelematicsDataConsole.Core.Entities;

public class Reseller
{
    public int ResellerId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public string? Phone { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public short Status { get; set; } = 1;
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<User> Users { get; set; } = new List<User>();
    public virtual ICollection<Technician> Technicians { get; set; } = new List<Technician>();
    public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();
}

public enum ResellerStatus : short
{
    Inactive = 0,
    Active = 1,
    Suspended = 2
}

