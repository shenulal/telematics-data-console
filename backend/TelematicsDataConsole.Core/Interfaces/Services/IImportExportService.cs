using TelematicsDataConsole.Core.DTOs.ImportExport;

namespace TelematicsDataConsole.Core.Interfaces.Services;

public interface IImportExportService
{
    // Tags
    Task<List<ExportTagDto>> ExportTagsAsync(bool includeItems = true);
    Task<byte[]> ExportTagsToExcelAsync(bool includeItems = true);
    Task<ImportResultDto> ImportTagsAsync(List<ImportTagDto> tags, bool updateExisting = false);
    Task<ImportResultDto> ImportTagsFromExcelAsync(Stream fileStream, bool updateExisting = false);

    // Technicians
    Task<List<ExportTechnicianDto>> ExportTechniciansAsync();
    Task<byte[]> ExportTechniciansToExcelAsync();
    Task<ImportResultDto> ImportTechniciansAsync(List<ImportTechnicianDto> technicians, bool updateExisting = false);
    Task<ImportResultDto> ImportTechniciansFromExcelAsync(Stream fileStream, bool updateExisting = false);

    // Resellers
    Task<List<ExportResellerDto>> ExportResellersAsync();
    Task<byte[]> ExportResellersToExcelAsync();
    Task<ImportResultDto> ImportResellersAsync(List<ImportResellerDto> resellers, bool updateExisting = false);
    Task<ImportResultDto> ImportResellersFromExcelAsync(Stream fileStream, bool updateExisting = false);

    // Users
    Task<List<ExportUserDto>> ExportUsersAsync();
    Task<byte[]> ExportUsersToExcelAsync();
    Task<ImportResultDto> ImportUsersAsync(List<ImportUserDto> users, bool updateExisting = false);
    Task<ImportResultDto> ImportUsersFromExcelAsync(Stream fileStream, bool updateExisting = false);

    // Roles
    Task<List<ExportRoleDto>> ExportRolesAsync();
    Task<byte[]> ExportRolesToExcelAsync();
    Task<ImportResultDto> ImportRolesAsync(List<ImportRoleDto> roles, bool updateExisting = false);
    Task<ImportResultDto> ImportRolesFromExcelAsync(Stream fileStream, bool updateExisting = false);
}

