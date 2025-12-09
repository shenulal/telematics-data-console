using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TelematicsDataConsole.Core.DTOs.ImportExport;
using TelematicsDataConsole.Core.Interfaces.Services;

namespace TelematicsDataConsole.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ImportExportController : ControllerBase
{
    private readonly IImportExportService _importExportService;
    private readonly ILogger<ImportExportController> _logger;

    public ImportExportController(IImportExportService importExportService, ILogger<ImportExportController> logger)
    {
        _importExportService = importExportService;
        _logger = logger;
    }

    // ============ TAGS ============

    [HttpGet("tags/export")]
    [Authorize(Roles = "SUPERADMIN,RESELLER ADMIN")]
    public async Task<ActionResult<List<ExportTagDto>>> ExportTags([FromQuery] bool includeItems = true)
    {
        var tags = await _importExportService.ExportTagsAsync(includeItems);
        return Ok(tags);
    }

    [HttpGet("tags/export/excel")]
    [Authorize(Roles = "SUPERADMIN,RESELLER ADMIN")]
    public async Task<IActionResult> ExportTagsExcel([FromQuery] bool includeItems = true)
    {
        var bytes = await _importExportService.ExportTagsToExcelAsync(includeItems);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"tags_export_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpPost("tags/import")]
    [Authorize(Roles = "SUPERADMIN,RESELLER ADMIN")]
    public async Task<ActionResult<ImportResultDto>> ImportTags([FromBody] List<ImportTagDto> tags, [FromQuery] bool updateExisting = false)
    {
        if (tags == null || tags.Count == 0)
            return BadRequest("No tags provided");

        var result = await _importExportService.ImportTagsAsync(tags, updateExisting);
        return Ok(result);
    }

    [HttpPost("tags/import/excel")]
    [Authorize(Roles = "SUPERADMIN,RESELLER ADMIN")]
    public async Task<ActionResult<ImportResultDto>> ImportTagsExcel(IFormFile file, [FromQuery] bool updateExisting = false)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided");

        using var stream = file.OpenReadStream();
        var result = await _importExportService.ImportTagsFromExcelAsync(stream, updateExisting);
        return Ok(result);
    }

    // ============ TECHNICIANS ============

    [HttpGet("technicians/export")]
    [Authorize(Roles = "SUPERADMIN,RESELLER ADMIN")]
    public async Task<ActionResult<List<ExportTechnicianDto>>> ExportTechnicians()
    {
        var technicians = await _importExportService.ExportTechniciansAsync();
        return Ok(technicians);
    }

    [HttpGet("technicians/export/excel")]
    [Authorize(Roles = "SUPERADMIN,RESELLER ADMIN")]
    public async Task<IActionResult> ExportTechniciansExcel()
    {
        var bytes = await _importExportService.ExportTechniciansToExcelAsync();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"technicians_export_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpPost("technicians/import")]
    [Authorize(Roles = "SUPERADMIN,RESELLER ADMIN")]
    public async Task<ActionResult<ImportResultDto>> ImportTechnicians([FromBody] List<ImportTechnicianDto> technicians, [FromQuery] bool updateExisting = false)
    {
        if (technicians == null || technicians.Count == 0)
            return BadRequest("No technicians provided");

        var result = await _importExportService.ImportTechniciansAsync(technicians, updateExisting);
        return Ok(result);
    }

    [HttpPost("technicians/import/excel")]
    [Authorize(Roles = "SUPERADMIN,RESELLER ADMIN")]
    public async Task<ActionResult<ImportResultDto>> ImportTechniciansExcel(IFormFile file, [FromQuery] bool updateExisting = false)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided");

        using var stream = file.OpenReadStream();
        var result = await _importExportService.ImportTechniciansFromExcelAsync(stream, updateExisting);
        return Ok(result);
    }

    // ============ RESELLERS ============

    [HttpGet("resellers/export")]
    [Authorize(Roles = "SUPERADMIN")]
    public async Task<ActionResult<List<ExportResellerDto>>> ExportResellers()
    {
        var resellers = await _importExportService.ExportResellersAsync();
        return Ok(resellers);
    }

    [HttpGet("resellers/export/excel")]
    [Authorize(Roles = "SUPERADMIN")]
    public async Task<IActionResult> ExportResellersExcel()
    {
        var bytes = await _importExportService.ExportResellersToExcelAsync();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"resellers_export_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpPost("resellers/import")]
    [Authorize(Roles = "SUPERADMIN")]
    public async Task<ActionResult<ImportResultDto>> ImportResellers([FromBody] List<ImportResellerDto> resellers, [FromQuery] bool updateExisting = false)
    {
        if (resellers == null || resellers.Count == 0)
            return BadRequest("No resellers provided");

        var result = await _importExportService.ImportResellersAsync(resellers, updateExisting);
        return Ok(result);
    }

    [HttpPost("resellers/import/excel")]
    [Authorize(Roles = "SUPERADMIN")]
    public async Task<ActionResult<ImportResultDto>> ImportResellersExcel(IFormFile file, [FromQuery] bool updateExisting = false)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided");

        using var stream = file.OpenReadStream();
        var result = await _importExportService.ImportResellersFromExcelAsync(stream, updateExisting);
        return Ok(result);
    }

    // ============ USERS ============

    [HttpGet("users/export")]
    [Authorize(Roles = "SUPERADMIN")]
    public async Task<ActionResult<List<ExportUserDto>>> ExportUsers()
    {
        var users = await _importExportService.ExportUsersAsync();
        return Ok(users);
    }

    [HttpGet("users/export/excel")]
    [Authorize(Roles = "SUPERADMIN")]
    public async Task<IActionResult> ExportUsersExcel()
    {
        var bytes = await _importExportService.ExportUsersToExcelAsync();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"users_export_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpPost("users/import")]
    [Authorize(Roles = "SUPERADMIN")]
    public async Task<ActionResult<ImportResultDto>> ImportUsers([FromBody] List<ImportUserDto> users, [FromQuery] bool updateExisting = false)
    {
        if (users == null || users.Count == 0)
            return BadRequest("No users provided");

        var result = await _importExportService.ImportUsersAsync(users, updateExisting);
        return Ok(result);
    }

    [HttpPost("users/import/excel")]
    [Authorize(Roles = "SUPERADMIN")]
    public async Task<ActionResult<ImportResultDto>> ImportUsersExcel(IFormFile file, [FromQuery] bool updateExisting = false)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided");

        using var stream = file.OpenReadStream();
        var result = await _importExportService.ImportUsersFromExcelAsync(stream, updateExisting);
        return Ok(result);
    }

    // ============ ROLES ============

    [HttpGet("roles/export")]
    [Authorize(Roles = "SUPERADMIN")]
    public async Task<ActionResult<List<ExportRoleDto>>> ExportRoles()
    {
        var roles = await _importExportService.ExportRolesAsync();
        return Ok(roles);
    }

    [HttpGet("roles/export/excel")]
    [Authorize(Roles = "SUPERADMIN")]
    public async Task<IActionResult> ExportRolesExcel()
    {
        var bytes = await _importExportService.ExportRolesToExcelAsync();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"roles_export_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpPost("roles/import")]
    [Authorize(Roles = "SUPERADMIN")]
    public async Task<ActionResult<ImportResultDto>> ImportRoles([FromBody] List<ImportRoleDto> roles, [FromQuery] bool updateExisting = false)
    {
        if (roles == null || roles.Count == 0)
            return BadRequest("No roles provided");

        var result = await _importExportService.ImportRolesAsync(roles, updateExisting);
        return Ok(result);
    }

    [HttpPost("roles/import/excel")]
    [Authorize(Roles = "SUPERADMIN")]
    public async Task<ActionResult<ImportResultDto>> ImportRolesExcel(IFormFile file, [FromQuery] bool updateExisting = false)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided");

        using var stream = file.OpenReadStream();
        var result = await _importExportService.ImportRolesFromExcelAsync(stream, updateExisting);
        return Ok(result);
    }
}

