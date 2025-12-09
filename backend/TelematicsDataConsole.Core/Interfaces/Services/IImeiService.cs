using TelematicsDataConsole.Core.DTOs.Imei;

namespace TelematicsDataConsole.Core.Interfaces.Services;

public interface IImeiService
{
    Task<ImeiAccessResult> CheckAccessAsync(int technicianId, string imei);
    Task<ImeiDataResult> GetDeviceDataAsync(int technicianId, string imei);
    Task<VerificationResult> VerifyDeviceAsync(int technicianId, VerificationRequest request);
    Task<IEnumerable<VerificationHistoryDto>> GetVerificationHistoryAsync(int technicianId, int? days = 30);

    // Admin verification methods (for Super Admin, Reseller Admin, Supervisor)
    Task<ImeiAccessResult> CheckAdminAccessAsync(int userId, int? resellerId, string imei);
    Task<ImeiDataResult> GetDeviceDataForAdminAsync(int userId, int? resellerId, string imei);
    Task<VerificationResult> VerifyDeviceForAdminAsync(int userId, int? resellerId, VerificationRequest request);
}

public class ImeiAccessResult
{
    public bool HasAccess { get; set; }
    public string? Message { get; set; }
    public string? RestrictionReason { get; set; }
    public int? DeviceId { get; set; }
}

public class ImeiDataResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public DeviceDataDto? Data { get; set; }
}

public class VerificationResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int? VerificationId { get; set; }
}

