using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using TelematicsDataConsole.Core.DTOs.ImportExport;
using TelematicsDataConsole.Core.Entities;
using TelematicsDataConsole.Core.Interfaces.Services;
using TelematicsDataConsole.Infrastructure.Data;

namespace TelematicsDataConsole.Infrastructure.Services;

public class ImportExportService : IImportExportService
{
    private readonly ApplicationDbContext _context;

    public ImportExportService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ============ TAGS ============
    
    public async Task<List<ExportTagDto>> ExportTagsAsync(bool includeItems = true)
    {
        var query = _context.Tags.AsNoTracking();
        
        if (includeItems)
            query = query.Include(t => t.TagItems);
        
        var tags = await query.ToListAsync();
        
        return tags.Select(t => new ExportTagDto
        {
            TagId = t.TagId,
            TagName = t.TagName,
            Description = t.Description,
            Scope = t.Scope,
            Color = t.Color,
            Status = t.Status,
            Items = includeItems ? t.TagItems.Select(ti => new ExportTagItemDto
            {
                EntityType = ti.EntityType,
                EntityId = ti.EntityId,
                EntityIdentifier = ti.EntityIdentifier
            }).ToList() : new List<ExportTagItemDto>()
        }).ToList();
    }

    public async Task<ImportResultDto> ImportTagsAsync(List<ImportTagDto> tags, bool updateExisting = false)
    {
        var result = new ImportResultDto { TotalRows = tags.Count };
        
        for (int i = 0; i < tags.Count; i++)
        {
            var dto = tags[i];
            try
            {
                var existing = await _context.Tags.Include(t => t.TagItems)
                    .FirstOrDefaultAsync(t => t.TagName == dto.TagName);
                
                if (existing != null)
                {
                    if (!updateExisting)
                    {
                        result.FailedCount++;
                        result.Errors.Add(new ImportErrorDto { RowNumber = i + 1, Identifier = dto.TagName, ErrorMessage = "Tag already exists" });
                        continue;
                    }
                    
                    existing.Description = dto.Description;
                    existing.Scope = dto.Scope;
                    existing.Color = dto.Color;
                    existing.Status = dto.Status;
                    
                    // Update items
                    existing.TagItems.Clear();
                    foreach (var item in dto.Items)
                    {
                        existing.TagItems.Add(new TagItem
                        {
                            EntityType = item.EntityType,
                            EntityId = item.EntityId,
                            EntityIdentifier = item.EntityIdentifier,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
                else
                {
                    var tag = new Tag
                    {
                        TagName = dto.TagName,
                        Description = dto.Description,
                        Scope = dto.Scope,
                        Color = dto.Color ?? "#3B82F6",
                        Status = dto.Status,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    foreach (var item in dto.Items)
                    {
                        tag.TagItems.Add(new TagItem
                        {
                            EntityType = item.EntityType,
                            EntityId = item.EntityId,
                            EntityIdentifier = item.EntityIdentifier,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                    
                    _context.Tags.Add(tag);
                }
                
                await _context.SaveChangesAsync();
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.Errors.Add(new ImportErrorDto { RowNumber = i + 1, Identifier = dto.TagName, ErrorMessage = ex.Message });
            }
        }
        
        return result;
    }

    // ============ TECHNICIANS ============

    public async Task<List<ExportTechnicianDto>> ExportTechniciansAsync()
    {
        var technicians = await _context.Technicians
            .Include(t => t.User)
            .Include(t => t.Reseller)
            .AsNoTracking()
            .ToListAsync();

        return technicians.Select(t => new ExportTechnicianDto
        {
            TechnicianId = t.TechnicianId,
            UserId = t.UserId,
            Username = t.User?.Username,
            Email = t.User?.Email,
            FullName = t.User?.FullName,
            EmployeeCode = t.EmployeeCode,
            Skillset = t.Skillset,
            Certification = t.Certification,
            WorkRegion = t.WorkRegion,
            DailyLimit = t.DailyLimit,
            ResellerId = t.ResellerId,
            ResellerName = t.Reseller?.CompanyName,
            Status = t.Status
        }).ToList();
    }

    public async Task<ImportResultDto> ImportTechniciansAsync(List<ImportTechnicianDto> technicians, bool updateExisting = false)
    {
        var result = new ImportResultDto { TotalRows = technicians.Count };

        for (int i = 0; i < technicians.Count; i++)
        {
            var dto = technicians[i];
            try
            {
                var existing = await _context.Technicians
                    .FirstOrDefaultAsync(t => t.UserId == dto.UserId);

                if (existing != null)
                {
                    if (!updateExisting)
                    {
                        result.FailedCount++;
                        result.Errors.Add(new ImportErrorDto { RowNumber = i + 1, Identifier = dto.UserId.ToString(), ErrorMessage = "Technician already exists" });
                        continue;
                    }

                    existing.EmployeeCode = dto.EmployeeCode;
                    existing.Skillset = dto.Skillset;
                    existing.Certification = dto.Certification;
                    existing.WorkRegion = dto.WorkRegion;
                    existing.DailyLimit = dto.DailyLimit;
                    existing.ResellerId = dto.ResellerId;
                    existing.Status = dto.Status;
                }
                else
                {
                    _context.Technicians.Add(new Technician
                    {
                        UserId = dto.UserId,
                        EmployeeCode = dto.EmployeeCode,
                        Skillset = dto.Skillset,
                        Certification = dto.Certification,
                        WorkRegion = dto.WorkRegion,
                        DailyLimit = dto.DailyLimit,
                        ResellerId = dto.ResellerId,
                        Status = dto.Status,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.Errors.Add(new ImportErrorDto { RowNumber = i + 1, Identifier = dto.UserId.ToString(), ErrorMessage = ex.Message });
            }
        }

        return result;
    }

    // ============ RESELLERS ============

    public async Task<List<ExportResellerDto>> ExportResellersAsync()
    {
        var resellers = await _context.Resellers.AsNoTracking().ToListAsync();

        return resellers.Select(r => new ExportResellerDto
        {
            ResellerId = r.ResellerId,
            CompanyName = r.CompanyName,
            DisplayName = r.DisplayName,
            ContactPerson = r.ContactPerson,
            Email = r.Email,
            Mobile = r.Mobile,
            Phone = r.Phone,
            AddressLine1 = r.AddressLine1,
            City = r.City,
            State = r.State,
            Country = r.Country,
            Status = r.Status
        }).ToList();
    }

    public async Task<ImportResultDto> ImportResellersAsync(List<ImportResellerDto> resellers, bool updateExisting = false)
    {
        var result = new ImportResultDto { TotalRows = resellers.Count };

        for (int i = 0; i < resellers.Count; i++)
        {
            var dto = resellers[i];
            try
            {
                var existing = await _context.Resellers.FirstOrDefaultAsync(r => r.CompanyName == dto.CompanyName);

                if (existing != null)
                {
                    if (!updateExisting)
                    {
                        result.FailedCount++;
                        result.Errors.Add(new ImportErrorDto { RowNumber = i + 1, Identifier = dto.CompanyName, ErrorMessage = "Reseller already exists" });
                        continue;
                    }

                    existing.DisplayName = dto.DisplayName;
                    existing.ContactPerson = dto.ContactPerson;
                    existing.Email = dto.Email;
                    existing.Mobile = dto.Mobile;
                    existing.Phone = dto.Phone;
                    existing.AddressLine1 = dto.AddressLine1;
                    existing.City = dto.City;
                    existing.State = dto.State;
                    existing.Country = dto.Country;
                    existing.Status = dto.Status;
                }
                else
                {
                    _context.Resellers.Add(new Reseller
                    {
                        CompanyName = dto.CompanyName,
                        DisplayName = dto.DisplayName,
                        ContactPerson = dto.ContactPerson,
                        Email = dto.Email,
                        Mobile = dto.Mobile,
                        Phone = dto.Phone,
                        AddressLine1 = dto.AddressLine1,
                        City = dto.City,
                        State = dto.State,
                        Country = dto.Country,
                        Status = dto.Status,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.Errors.Add(new ImportErrorDto { RowNumber = i + 1, Identifier = dto.CompanyName, ErrorMessage = ex.Message });
            }
        }

        return result;
    }

    // ============ USERS ============

    public async Task<List<ExportUserDto>> ExportUsersAsync()
    {
        var users = await _context.Users
            .Include(u => u.Reseller)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .AsNoTracking()
            .ToListAsync();

        return users.Select(u => new ExportUserDto
        {
            UserId = u.UserId,
            Username = u.Username,
            Email = u.Email,
            FullName = u.FullName,
            AliasName = u.AliasName,
            Mobile = u.Mobile,
            ResellerId = u.ResellerId,
            ResellerName = u.Reseller?.CompanyName,
            Status = u.Status,
            Roles = u.UserRoles.Select(ur => ur.Role.RoleName).ToList()
        }).ToList();
    }

    public async Task<ImportResultDto> ImportUsersAsync(List<ImportUserDto> users, bool updateExisting = false)
    {
        var result = new ImportResultDto { TotalRows = users.Count };

        for (int i = 0; i < users.Count; i++)
        {
            var dto = users[i];
            try
            {
                var existing = await _context.Users.Include(u => u.UserRoles)
                    .FirstOrDefaultAsync(u => u.Email == dto.Email || u.Username == dto.Username);

                if (existing != null)
                {
                    if (!updateExisting)
                    {
                        result.FailedCount++;
                        result.Errors.Add(new ImportErrorDto { RowNumber = i + 1, Identifier = dto.Email, ErrorMessage = "User already exists" });
                        continue;
                    }

                    existing.FullName = dto.FullName;
                    existing.AliasName = dto.AliasName;
                    existing.Mobile = dto.Mobile;
                    existing.ResellerId = dto.ResellerId;
                    existing.Status = dto.Status;

                    // Update roles
                    existing.UserRoles.Clear();
                    foreach (var roleName in dto.Roles)
                    {
                        var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
                        if (role != null)
                        {
                            existing.UserRoles.Add(new UserRole { UserId = existing.UserId, RoleId = role.RoleId });
                        }
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(dto.Password))
                    {
                        result.FailedCount++;
                        result.Errors.Add(new ImportErrorDto { RowNumber = i + 1, Identifier = dto.Email, ErrorMessage = "Password required for new users" });
                        continue;
                    }

                    var user = new User
                    {
                        Username = dto.Username,
                        Email = dto.Email,
                        FullName = dto.FullName,
                        AliasName = dto.AliasName,
                        Mobile = dto.Mobile,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                        ResellerId = dto.ResellerId,
                        Status = dto.Status,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    // Add roles
                    foreach (var roleName in dto.Roles)
                    {
                        var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
                        if (role != null)
                        {
                            _context.UserRoles.Add(new UserRole { UserId = user.UserId, RoleId = role.RoleId });
                        }
                    }
                }

                await _context.SaveChangesAsync();
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.Errors.Add(new ImportErrorDto { RowNumber = i + 1, Identifier = dto.Email, ErrorMessage = ex.Message });
            }
        }

        return result;
    }

    // ============ ROLES ============

    public async Task<List<ExportRoleDto>> ExportRolesAsync()
    {
        var roles = await _context.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .Where(r => !r.IsSystemRole)
            .AsNoTracking()
            .ToListAsync();

        return roles.Select(r => new ExportRoleDto
        {
            RoleId = r.RoleId,
            RoleName = r.RoleName,
            Description = r.Description,
            IsSystemRole = r.IsSystemRole,
            Permissions = r.RolePermissions.Select(rp => rp.Permission.PermissionName).ToList()
        }).ToList();
    }

    public async Task<ImportResultDto> ImportRolesAsync(List<ImportRoleDto> roles, bool updateExisting = false)
    {
        var result = new ImportResultDto { TotalRows = roles.Count };

        for (int i = 0; i < roles.Count; i++)
        {
            var dto = roles[i];
            try
            {
                var existing = await _context.Roles.Include(r => r.RolePermissions)
                    .FirstOrDefaultAsync(r => r.RoleName == dto.RoleName);

                if (existing != null)
                {
                    if (existing.IsSystemRole)
                    {
                        result.FailedCount++;
                        result.Errors.Add(new ImportErrorDto { RowNumber = i + 1, Identifier = dto.RoleName, ErrorMessage = "Cannot modify system role" });
                        continue;
                    }

                    if (!updateExisting)
                    {
                        result.FailedCount++;
                        result.Errors.Add(new ImportErrorDto { RowNumber = i + 1, Identifier = dto.RoleName, ErrorMessage = "Role already exists" });
                        continue;
                    }

                    existing.Description = dto.Description;

                    // Update permissions
                    existing.RolePermissions.Clear();
                    foreach (var permName in dto.Permissions)
                    {
                        var perm = await _context.Permissions.FirstOrDefaultAsync(p => p.PermissionName == permName);
                        if (perm != null)
                        {
                            existing.RolePermissions.Add(new RolePermission { RoleId = existing.RoleId, PermissionId = perm.PermissionId });
                        }
                    }
                }
                else
                {
                    var role = new Role
                    {
                        RoleName = dto.RoleName,
                        Description = dto.Description,
                        IsSystemRole = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Roles.Add(role);
                    await _context.SaveChangesAsync();

                    // Add permissions
                    foreach (var permName in dto.Permissions)
                    {
                        var perm = await _context.Permissions.FirstOrDefaultAsync(p => p.PermissionName == permName);
                        if (perm != null)
                        {
                            _context.RolePermissions.Add(new RolePermission { RoleId = role.RoleId, PermissionId = perm.PermissionId });
                        }
                    }
                }

                await _context.SaveChangesAsync();
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.Errors.Add(new ImportErrorDto { RowNumber = i + 1, Identifier = dto.RoleName, ErrorMessage = ex.Message });
            }
        }

        return result;
    }

    // ============ EXCEL EXPORT METHODS ============

    public async Task<byte[]> ExportTagsToExcelAsync(bool includeItems = true)
    {
        var tags = await ExportTagsAsync(includeItems);
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Tags");

        // Headers
        ws.Cell(1, 1).Value = "TagName";
        ws.Cell(1, 2).Value = "Description";
        ws.Cell(1, 3).Value = "Scope";
        ws.Cell(1, 4).Value = "Color";
        ws.Cell(1, 5).Value = "Status";
        ws.Row(1).Style.Font.Bold = true;

        int row = 2;
        foreach (var tag in tags)
        {
            ws.Cell(row, 1).Value = tag.TagName;
            ws.Cell(row, 2).Value = tag.Description;
            ws.Cell(row, 3).Value = tag.Scope;
            ws.Cell(row, 4).Value = tag.Color;
            ws.Cell(row, 5).Value = tag.Status;
            row++;
        }

        // Tag Items sheet if included
        if (includeItems)
        {
            var wsItems = workbook.Worksheets.Add("TagItems");
            wsItems.Cell(1, 1).Value = "TagName";
            wsItems.Cell(1, 2).Value = "EntityType";
            wsItems.Cell(1, 3).Value = "EntityId";
            wsItems.Cell(1, 4).Value = "EntityIdentifier";
            wsItems.Row(1).Style.Font.Bold = true;

            int itemRow = 2;
            foreach (var tag in tags)
            {
                foreach (var item in tag.Items)
                {
                    wsItems.Cell(itemRow, 1).Value = tag.TagName;
                    wsItems.Cell(itemRow, 2).Value = item.EntityType;
                    wsItems.Cell(itemRow, 3).Value = item.EntityId;
                    wsItems.Cell(itemRow, 4).Value = item.EntityIdentifier;
                    itemRow++;
                }
            }
            wsItems.Columns().AdjustToContents();
        }

        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportTechniciansToExcelAsync()
    {
        var technicians = await ExportTechniciansAsync();
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Technicians");

        ws.Cell(1, 1).Value = "UserId";
        ws.Cell(1, 2).Value = "Username";
        ws.Cell(1, 3).Value = "Email";
        ws.Cell(1, 4).Value = "FullName";
        ws.Cell(1, 5).Value = "EmployeeCode";
        ws.Cell(1, 6).Value = "Skillset";
        ws.Cell(1, 7).Value = "Certification";
        ws.Cell(1, 8).Value = "WorkRegion";
        ws.Cell(1, 9).Value = "DailyLimit";
        ws.Cell(1, 10).Value = "ResellerId";
        ws.Cell(1, 11).Value = "ResellerName";
        ws.Cell(1, 12).Value = "Status";
        ws.Row(1).Style.Font.Bold = true;

        int row = 2;
        foreach (var t in technicians)
        {
            ws.Cell(row, 1).Value = t.UserId;
            ws.Cell(row, 2).Value = t.Username;
            ws.Cell(row, 3).Value = t.Email;
            ws.Cell(row, 4).Value = t.FullName;
            ws.Cell(row, 5).Value = t.EmployeeCode;
            ws.Cell(row, 6).Value = t.Skillset;
            ws.Cell(row, 7).Value = t.Certification;
            ws.Cell(row, 8).Value = t.WorkRegion;
            ws.Cell(row, 9).Value = t.DailyLimit;
            ws.Cell(row, 10).Value = t.ResellerId;
            ws.Cell(row, 11).Value = t.ResellerName;
            ws.Cell(row, 12).Value = t.Status;
            row++;
        }

        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportResellersToExcelAsync()
    {
        var resellers = await ExportResellersAsync();
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Resellers");

        ws.Cell(1, 1).Value = "CompanyName";
        ws.Cell(1, 2).Value = "DisplayName";
        ws.Cell(1, 3).Value = "ContactPerson";
        ws.Cell(1, 4).Value = "Email";
        ws.Cell(1, 5).Value = "Mobile";
        ws.Cell(1, 6).Value = "Phone";
        ws.Cell(1, 7).Value = "AddressLine1";
        ws.Cell(1, 8).Value = "City";
        ws.Cell(1, 9).Value = "State";
        ws.Cell(1, 10).Value = "Country";
        ws.Cell(1, 11).Value = "Status";
        ws.Row(1).Style.Font.Bold = true;

        int row = 2;
        foreach (var r in resellers)
        {
            ws.Cell(row, 1).Value = r.CompanyName;
            ws.Cell(row, 2).Value = r.DisplayName;
            ws.Cell(row, 3).Value = r.ContactPerson;
            ws.Cell(row, 4).Value = r.Email;
            ws.Cell(row, 5).Value = r.Mobile;
            ws.Cell(row, 6).Value = r.Phone;
            ws.Cell(row, 7).Value = r.AddressLine1;
            ws.Cell(row, 8).Value = r.City;
            ws.Cell(row, 9).Value = r.State;
            ws.Cell(row, 10).Value = r.Country;
            ws.Cell(row, 11).Value = r.Status;
            row++;
        }

        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportUsersToExcelAsync()
    {
        var users = await ExportUsersAsync();
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Users");

        ws.Cell(1, 1).Value = "Username";
        ws.Cell(1, 2).Value = "Email";
        ws.Cell(1, 3).Value = "FullName";
        ws.Cell(1, 4).Value = "AliasName";
        ws.Cell(1, 5).Value = "Mobile";
        ws.Cell(1, 6).Value = "ResellerId";
        ws.Cell(1, 7).Value = "ResellerName";
        ws.Cell(1, 8).Value = "Status";
        ws.Cell(1, 9).Value = "Roles";
        ws.Row(1).Style.Font.Bold = true;

        int row = 2;
        foreach (var u in users)
        {
            ws.Cell(row, 1).Value = u.Username;
            ws.Cell(row, 2).Value = u.Email;
            ws.Cell(row, 3).Value = u.FullName;
            ws.Cell(row, 4).Value = u.AliasName;
            ws.Cell(row, 5).Value = u.Mobile;
            ws.Cell(row, 6).Value = u.ResellerId;
            ws.Cell(row, 7).Value = u.ResellerName;
            ws.Cell(row, 8).Value = u.Status;
            ws.Cell(row, 9).Value = string.Join(", ", u.Roles);
            row++;
        }

        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportRolesToExcelAsync()
    {
        var roles = await ExportRolesAsync();
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Roles");

        ws.Cell(1, 1).Value = "RoleName";
        ws.Cell(1, 2).Value = "Description";
        ws.Cell(1, 3).Value = "Permissions";
        ws.Row(1).Style.Font.Bold = true;

        int row = 2;
        foreach (var r in roles)
        {
            ws.Cell(row, 1).Value = r.RoleName;
            ws.Cell(row, 2).Value = r.Description;
            ws.Cell(row, 3).Value = string.Join(", ", r.Permissions);
            row++;
        }

        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // ============ EXCEL IMPORT METHODS ============

    public async Task<ImportResultDto> ImportTagsFromExcelAsync(Stream fileStream, bool updateExisting = false)
    {
        using var workbook = new XLWorkbook(fileStream);
        var ws = workbook.Worksheet("Tags");
        var tags = new List<ImportTagDto>();

        // Read tag items if sheet exists
        var tagItems = new Dictionary<string, List<ImportTagItemDto>>();
        if (workbook.Worksheets.TryGetWorksheet("TagItems", out var wsItems))
        {
            var itemRows = wsItems.RangeUsed()?.RowsUsed().Skip(1) ?? Enumerable.Empty<IXLRangeRow>();
            foreach (var row in itemRows)
            {
                var tagName = row.Cell(1).GetString();
                if (!tagItems.ContainsKey(tagName))
                    tagItems[tagName] = new List<ImportTagItemDto>();

                tagItems[tagName].Add(new ImportTagItemDto
                {
                    EntityType = (short)row.Cell(2).GetDouble(),
                    EntityId = (long)row.Cell(3).GetDouble(),
                    EntityIdentifier = row.Cell(4).GetString()
                });
            }
        }

        var rows = ws.RangeUsed()?.RowsUsed().Skip(1) ?? Enumerable.Empty<IXLRangeRow>();
        foreach (var row in rows)
        {
            var tagName = row.Cell(1).GetString();
            tags.Add(new ImportTagDto
            {
                TagName = tagName,
                Description = row.Cell(2).GetString(),
                Scope = (short)row.Cell(3).GetDouble(),
                Color = row.Cell(4).GetString(),
                Status = (short)row.Cell(5).GetDouble(),
                Items = tagItems.GetValueOrDefault(tagName, new List<ImportTagItemDto>())
            });
        }

        return await ImportTagsAsync(tags, updateExisting);
    }

    public async Task<ImportResultDto> ImportTechniciansFromExcelAsync(Stream fileStream, bool updateExisting = false)
    {
        using var workbook = new XLWorkbook(fileStream);
        var ws = workbook.Worksheet("Technicians");
        var technicians = new List<ImportTechnicianDto>();

        var rows = ws.RangeUsed()?.RowsUsed().Skip(1) ?? Enumerable.Empty<IXLRangeRow>();
        foreach (var row in rows)
        {
            technicians.Add(new ImportTechnicianDto
            {
                UserId = (int)row.Cell(1).GetDouble(),
                EmployeeCode = row.Cell(5).GetString(),
                Skillset = row.Cell(6).GetString(),
                Certification = row.Cell(7).GetString(),
                WorkRegion = row.Cell(8).GetString(),
                DailyLimit = row.Cell(9).IsEmpty() ? 100 : (int)row.Cell(9).GetDouble(),
                ResellerId = row.Cell(10).IsEmpty() ? null : (int)row.Cell(10).GetDouble(),
                Status = (short)row.Cell(12).GetDouble()
            });
        }

        return await ImportTechniciansAsync(technicians, updateExisting);
    }

    public async Task<ImportResultDto> ImportResellersFromExcelAsync(Stream fileStream, bool updateExisting = false)
    {
        using var workbook = new XLWorkbook(fileStream);
        var ws = workbook.Worksheet("Resellers");
        var resellers = new List<ImportResellerDto>();

        var rows = ws.RangeUsed()?.RowsUsed().Skip(1) ?? Enumerable.Empty<IXLRangeRow>();
        foreach (var row in rows)
        {
            resellers.Add(new ImportResellerDto
            {
                CompanyName = row.Cell(1).GetString(),
                DisplayName = row.Cell(2).GetString(),
                ContactPerson = row.Cell(3).GetString(),
                Email = row.Cell(4).GetString(),
                Mobile = row.Cell(5).GetString(),
                Phone = row.Cell(6).GetString(),
                AddressLine1 = row.Cell(7).GetString(),
                City = row.Cell(8).GetString(),
                State = row.Cell(9).GetString(),
                Country = row.Cell(10).GetString(),
                Status = (short)row.Cell(11).GetDouble()
            });
        }

        return await ImportResellersAsync(resellers, updateExisting);
    }

    public async Task<ImportResultDto> ImportUsersFromExcelAsync(Stream fileStream, bool updateExisting = false)
    {
        using var workbook = new XLWorkbook(fileStream);
        var ws = workbook.Worksheet("Users");
        var users = new List<ImportUserDto>();

        var rows = ws.RangeUsed()?.RowsUsed().Skip(1) ?? Enumerable.Empty<IXLRangeRow>();
        foreach (var row in rows)
        {
            var rolesStr = row.Cell(9).GetString();
            users.Add(new ImportUserDto
            {
                Username = row.Cell(1).GetString(),
                Email = row.Cell(2).GetString(),
                FullName = row.Cell(3).GetString(),
                AliasName = row.Cell(4).GetString(),
                Mobile = row.Cell(5).GetString(),
                ResellerId = row.Cell(6).IsEmpty() ? null : (int)row.Cell(6).GetDouble(),
                Status = (short)row.Cell(8).GetDouble(),
                Roles = string.IsNullOrEmpty(rolesStr) ? new List<string>() : rolesStr.Split(',').Select(r => r.Trim()).ToList()
            });
        }

        return await ImportUsersAsync(users, updateExisting);
    }

    public async Task<ImportResultDto> ImportRolesFromExcelAsync(Stream fileStream, bool updateExisting = false)
    {
        using var workbook = new XLWorkbook(fileStream);
        var ws = workbook.Worksheet("Roles");
        var roles = new List<ImportRoleDto>();

        var rows = ws.RangeUsed()?.RowsUsed().Skip(1) ?? Enumerable.Empty<IXLRangeRow>();
        foreach (var row in rows)
        {
            var permsStr = row.Cell(3).GetString();
            roles.Add(new ImportRoleDto
            {
                RoleName = row.Cell(1).GetString(),
                Description = row.Cell(2).GetString(),
                Permissions = string.IsNullOrEmpty(permsStr) ? new List<string>() : permsStr.Split(',').Select(p => p.Trim()).ToList()
            });
        }

        return await ImportRolesAsync(roles, updateExisting);
    }
}
