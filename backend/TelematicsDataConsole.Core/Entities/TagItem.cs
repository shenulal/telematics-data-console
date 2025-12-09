namespace TelematicsDataConsole.Core.Entities;

public class TagItem
{
    public int TagItemId { get; set; }
    public int TagId { get; set; }

    /// <summary>
    /// Type of entity being tagged (Device, Technician, Reseller, etc.)
    /// </summary>
    public short EntityType { get; set; }

    /// <summary>
    /// The ID of the entity being tagged (could be DeviceId, TechnicianId, ResellerId, etc.)
    /// </summary>
    public long EntityId { get; set; }

    /// <summary>
    /// Optional identifier for the entity (e.g., IMEI for devices, Name for technicians)
    /// </summary>
    public string? EntityIdentifier { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Tag Tag { get; set; } = null!;
}

/// <summary>
/// Entity types that can be tagged
/// </summary>
public enum TagEntityType : short
{
    Device = 1,
    Technician = 2,
    Reseller = 3,
    User = 4
    // Future entity types can be added here
}

