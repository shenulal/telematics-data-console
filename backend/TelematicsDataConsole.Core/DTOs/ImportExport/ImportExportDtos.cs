using System.ComponentModel.DataAnnotations;

namespace TelematicsDataConsole.Core.DTOs.ImportExport;

// ============ EXPORT DTOs ============

public class ExportTagDto
{
    public int TagId { get; set; }
    public string TagName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public short Scope { get; set; }
    public string? Color { get; set; }
    public short Status { get; set; }
    public List<ExportTagItemDto> Items { get; set; } = new();
}

public class ExportTagItemDto
{
    public short EntityType { get; set; }
    public long EntityId { get; set; }
    public string? EntityIdentifier { get; set; }
}

/// <summary>
/// Detailed export of tag items with enriched data
/// </summary>
public class ExportTagItemDetailDto
{
    public int TagItemId { get; set; }
    public int TagId { get; set; }
    public string TagName { get; set; } = string.Empty;
    public short EntityType { get; set; }
    public string EntityTypeName { get; set; } = string.Empty;
    public long EntityId { get; set; }
    public string? EntityIdentifier { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ExportTechnicianDto
{
    public int TechnicianId { get; set; }
    public int UserId { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public string? EmployeeCode { get; set; }
    public string? Skillset { get; set; }
    public string? Certification { get; set; }
    public string? WorkRegion { get; set; }
    public int DailyLimit { get; set; }
    public int? ResellerId { get; set; }
    public string? ResellerName { get; set; }
    public short Status { get; set; }
}

public class ExportResellerDto
{
    public int ResellerId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public string? Phone { get; set; }
    public string? AddressLine1 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public short Status { get; set; }
}

public class ExportUserDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? AliasName { get; set; }
    public string? Mobile { get; set; }
    public int? ResellerId { get; set; }
    public string? ResellerName { get; set; }
    public short Status { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class ExportRoleDto
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public List<string> Permissions { get; set; } = new();
}

// ============ IMPORT DTOs ============

public class ImportTagDto
{
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string TagName { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string? Description { get; set; }
    
    public short Scope { get; set; } = 0;
    
    [StringLength(7)]
    public string? Color { get; set; }
    
    public short Status { get; set; } = 1;
    
    public List<ImportTagItemDto> Items { get; set; } = new();
}

public class ImportTagItemDto
{
    public short EntityType { get; set; } = 1;
    public long EntityId { get; set; }
    public string? EntityIdentifier { get; set; }
}

/// <summary>
/// DTO for importing tag items using IMEI (DeviceId will be looked up)
/// </summary>
public class ImportTagItemByIdentifierDto
{
    /// <summary>
    /// Entity Type: 1 = Device, 2 = Technician, 3 = Reseller, 4 = User
    /// </summary>
    public short EntityType { get; set; } = 1;

    /// <summary>
    /// For devices: IMEI number
    /// For technicians: Username or EmployeeCode
    /// For resellers: CompanyName
    /// For users: Username or Email
    /// </summary>
    [Required]
    public string Identifier { get; set; } = string.Empty;
}

/// <summary>
/// Bulk import request for tag items
/// </summary>
public class BulkImportTagItemsDto
{
    public int TagId { get; set; }
    public short EntityType { get; set; } = 1;
    public List<ImportTagItemByIdentifierDto> Items { get; set; } = new();
}

public class ImportTechnicianDto
{
    [Required]
    public int UserId { get; set; }

    public string? EmployeeCode { get; set; }

    [StringLength(500)]
    public string? Skillset { get; set; }

    [StringLength(200)]
    public string? Certification { get; set; }

    public string? WorkRegion { get; set; }

    public int DailyLimit { get; set; } = 100;
    public int? ResellerId { get; set; }
    public short Status { get; set; } = 1;
}

public class ImportResellerDto
{
    [Required]
    [StringLength(100)]
    public string CompanyName { get; set; } = string.Empty;

    public string? DisplayName { get; set; }
    public string? ContactPerson { get; set; }

    [EmailAddress]
    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(20)]
    public string? Mobile { get; set; }

    public string? Phone { get; set; }
    public string? AddressLine1 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }

    public short Status { get; set; } = 1;
}

public class ImportUserDto
{
    [Required]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    public string? FullName { get; set; }
    public string? AliasName { get; set; }
    public string? Mobile { get; set; }

    public int? ResellerId { get; set; }
    public short Status { get; set; } = 1;
    public List<string> Roles { get; set; } = new();

    // Password is only used for new users
    public string? Password { get; set; }
}

public class ImportRoleDto
{
    [Required]
    [StringLength(50)]
    public string RoleName { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Description { get; set; }

    public List<string> Permissions { get; set; } = new();
}

// ============ RESULT DTOs ============

public class ImportResultDto
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<ImportErrorDto> Errors { get; set; } = new();
}

public class ImportErrorDto
{
    public int RowNumber { get; set; }
    public string? Identifier { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

