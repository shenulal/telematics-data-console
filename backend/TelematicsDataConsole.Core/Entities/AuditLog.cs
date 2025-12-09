namespace TelematicsDataConsole.Core.Entities;

public class AuditLog
{
    public int AuditId { get; set; }
    public int? UserId { get; set; }
    public string? Username { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? RequestPath { get; set; }
    public string? RequestMethod { get; set; }
    public int? ResponseStatusCode { get; set; }
    public long? DurationMs { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public static class AuditActions
{
    public const string Login = "LOGIN";
    public const string Logout = "LOGOUT";
    public const string LoginFailed = "LOGIN_FAILED";
    public const string ImeiAccess = "IMEI_ACCESS";
    public const string ImeiAccessDenied = "IMEI_ACCESS_DENIED";
    public const string ImeiVerification = "IMEI_VERIFICATION";
    public const string Create = "CREATE";
    public const string Update = "UPDATE";
    public const string Delete = "DELETE";
}

