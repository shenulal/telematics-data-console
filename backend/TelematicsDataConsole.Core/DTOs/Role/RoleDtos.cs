namespace TelematicsDataConsole.Core.DTOs.Role;

public class RoleDto
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public int? ResellerId { get; set; }
    public string? ResellerName { get; set; }
    public List<PermissionDto> Permissions { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateRoleDto
{
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<int> PermissionIds { get; set; } = new();
}

public class UpdateRoleDto
{
    public string? RoleName { get; set; }
    public string? Description { get; set; }
    public List<int>? PermissionIds { get; set; }
}

public class PermissionDto
{
    public int PermissionId { get; set; }
    public string PermissionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Module { get; set; }
}

public class CreatePermissionDto
{
    public string PermissionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Module { get; set; }
}

public class UpdatePermissionDto
{
    public string? PermissionName { get; set; }
    public string? Description { get; set; }
    public string? Module { get; set; }
}

