using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TelematicsDataConsole.API.Authorization;

/// <summary>
/// Attribute to require specific permission for an action
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class RequirePermissionAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _permission;

    public RequirePermissionAttribute(string permission)
    {
        _permission = permission;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Check if user has the required permission
        var hasPermission = user.Claims
            .Where(c => c.Type == "Permission")
            .Any(c => c.Value == _permission);

        // Check if user is an admin (handles variations: SUPERADMIN, Super Admin, Admin)
        var isAdmin = IsSuperAdmin(user);

        if (!hasPermission && !isAdmin)
        {
            context.Result = new ForbidResult();
        }
    }

    /// <summary>
    /// Check if user is Super Admin (handles role name variations)
    /// </summary>
    private static bool IsSuperAdmin(ClaimsPrincipal user)
    {
        var roles = user.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        return roles.Any(r =>
        {
            var normalized = r.ToUpperInvariant().Replace(" ", "");
            return normalized == "SUPERADMIN" || normalized == "ADMIN";
        });
    }
}

/// <summary>
/// Policy-based permission requirement
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}

/// <summary>
/// Handler for permission-based authorization
/// </summary>
public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var user = context.User;

        // Check if user is Super Admin (handles variations)
        if (IsSuperAdmin(user))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Check for specific permission
        var hasPermission = user.Claims
            .Where(c => c.Type == "Permission")
            .Any(c => c.Value == requirement.Permission);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Check if user is Super Admin (handles role name variations)
    /// </summary>
    private static bool IsSuperAdmin(ClaimsPrincipal user)
    {
        var roles = user.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        return roles.Any(r =>
        {
            var normalized = r.ToUpperInvariant().Replace(" ", "");
            return normalized == "SUPERADMIN" || normalized == "ADMIN";
        });
    }
}
