using Microsoft.EntityFrameworkCore;
using TelematicsDataConsole.Core.DTOs;
using TelematicsDataConsole.Core.DTOs.Reseller;
using TelematicsDataConsole.Core.Entities;
using TelematicsDataConsole.Core.Interfaces.Services;
using TelematicsDataConsole.Infrastructure.Data;

namespace TelematicsDataConsole.Infrastructure.Services;

public class ResellerService : IResellerService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public ResellerService(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<PagedResult<ResellerDto>> GetAllAsync(ResellerFilterDto filter)
    {
        var query = _context.Resellers.Include(r => r.Technicians).AsQueryable();

        if (filter.Status.HasValue)
            query = query.Where(r => r.Status == filter.Status);

        if (!string.IsNullOrEmpty(filter.Country))
            query = query.Where(r => r.Country == filter.Country);

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            query = query.Where(r =>
                r.CompanyName.Contains(filter.SearchTerm) ||
                (r.Email != null && r.Email.Contains(filter.SearchTerm)) ||
                (r.ContactPerson != null && r.ContactPerson.Contains(filter.SearchTerm)));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(r => new ResellerDto
            {
                ResellerId = r.ResellerId,
                CompanyName = r.CompanyName,
                DisplayName = r.DisplayName,
                ContactPerson = r.ContactPerson,
                Email = r.Email,
                Mobile = r.Mobile,
                Phone = r.Phone,
                City = r.City,
                State = r.State,
                Country = r.Country,
                Status = r.Status,
                TechnicianCount = r.Technicians.Count,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<ResellerDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<ResellerDto?> GetByIdAsync(int id)
    {
        return await _context.Resellers
            .Include(r => r.Technicians)
            .Where(r => r.ResellerId == id)
            .Select(r => new ResellerDto
            {
                ResellerId = r.ResellerId,
                CompanyName = r.CompanyName,
                DisplayName = r.DisplayName,
                ContactPerson = r.ContactPerson,
                Email = r.Email,
                Mobile = r.Mobile,
                Phone = r.Phone,
                City = r.City,
                State = r.State,
                Country = r.Country,
                Status = r.Status,
                TechnicianCount = r.Technicians.Count,
                CreatedAt = r.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ResellerDto> CreateAsync(CreateResellerDto dto, int createdBy)
    {
        var reseller = new Reseller
        {
            CompanyName = dto.CompanyName,
            DisplayName = dto.DisplayName,
            ContactPerson = dto.ContactPerson,
            Email = dto.Email,
            Mobile = dto.Mobile,
            Phone = dto.Phone,
            AddressLine1 = dto.AddressLine1,
            AddressLine2 = dto.AddressLine2,
            City = dto.City,
            State = dto.State,
            Country = dto.Country,
            PostalCode = dto.PostalCode,
            Status = (short)ResellerStatus.Active,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };

        await _context.Resellers.AddAsync(reseller);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(createdBy, AuditActions.Create, "Reseller", reseller.ResellerId.ToString(), null, dto);

        return (await GetByIdAsync(reseller.ResellerId))!;
    }

    public async Task<ResellerDto> UpdateAsync(int id, UpdateResellerDto dto, int updatedBy)
    {
        var reseller = await _context.Resellers.FindAsync(id)
            ?? throw new KeyNotFoundException("Reseller not found");

        reseller.CompanyName = dto.CompanyName;
        reseller.DisplayName = dto.DisplayName;
        reseller.ContactPerson = dto.ContactPerson;
        reseller.Email = dto.Email;
        reseller.Mobile = dto.Mobile;
        reseller.Phone = dto.Phone;
        reseller.AddressLine1 = dto.AddressLine1;
        reseller.AddressLine2 = dto.AddressLine2;
        reseller.City = dto.City;
        reseller.State = dto.State;
        reseller.Country = dto.Country;
        reseller.PostalCode = dto.PostalCode;
        if (dto.Status.HasValue) reseller.Status = dto.Status.Value;
        reseller.UpdatedBy = updatedBy;
        reseller.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _auditService.LogAsync(updatedBy, AuditActions.Update, "Reseller", id.ToString());

        return (await GetByIdAsync(id))!;
    }

    public async Task<bool> DeactivateAsync(int id, int updatedBy)
    {
        var reseller = await _context.Resellers.FindAsync(id);
        if (reseller == null) return false;
        reseller.Status = (short)ResellerStatus.Inactive;
        reseller.UpdatedBy = updatedBy;
        reseller.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ActivateAsync(int id, int updatedBy)
    {
        var reseller = await _context.Resellers.FindAsync(id);
        if (reseller == null) return false;
        reseller.Status = (short)ResellerStatus.Active;
        reseller.UpdatedBy = updatedBy;
        reseller.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<ResellerStatsDto> GetStatsAsync(int resellerId, DateTime? from = null, DateTime? to = null)
    {
        var reseller = await _context.Resellers.Include(r => r.Technicians).FirstOrDefaultAsync(r => r.ResellerId == resellerId)
            ?? throw new KeyNotFoundException();

        var technicianIds = reseller.Technicians.Select(t => t.TechnicianId).ToList();
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        var verifications = await _context.VerificationLogs.Where(v => technicianIds.Contains(v.TechnicianId)).ToListAsync();

        return new ResellerStatsDto
        {
            ResellerId = resellerId,
            TotalTechnicians = reseller.Technicians.Count,
            ActiveTechnicians = reseller.Technicians.Count(t => t.Status == (short)TechnicianStatus.Active),
            TotalVerifications = verifications.Count,
            VerificationsThisMonth = verifications.Count(v => v.VerifiedAt >= monthStart)
        };
    }

    public async Task<ResellerStatusUpdateResultDto> UpdateStatusWithCascadeAsync(int id, short newStatus, int updatedBy)
    {
        var reseller = await _context.Resellers.FindAsync(id)
            ?? throw new KeyNotFoundException("Reseller not found");

        var oldStatus = reseller.Status;
        reseller.Status = newStatus;
        reseller.UpdatedBy = updatedBy;
        reseller.UpdatedAt = DateTime.UtcNow;

        // Cascade status update to all related entities
        var usersUpdated = await _context.Users
            .Where(u => u.ResellerId == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.Status, newStatus)
                .SetProperty(u => u.UpdatedAt, DateTime.UtcNow));

        var techniciansUpdated = await _context.Technicians
            .Where(t => t.ResellerId == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.Status, newStatus)
                .SetProperty(t => t.UpdatedAt, DateTime.UtcNow)
                .SetProperty(t => t.UpdatedBy, updatedBy));

        var tagsUpdated = await _context.Tags
            .Where(t => t.ResellerId == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.Status, newStatus)
                .SetProperty(t => t.UpdatedAt, DateTime.UtcNow)
                .SetProperty(t => t.UpdatedBy, updatedBy));

        // Update custom roles (non-system roles) associated with users of this reseller
        var userIds = await _context.Users.Where(u => u.ResellerId == id).Select(u => u.UserId).ToListAsync();
        var rolesUpdated = await _context.Roles
            .Where(r => !r.IsSystemRole && _context.UserRoles.Any(ur => userIds.Contains(ur.UserId) && ur.RoleId == r.RoleId))
            .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.UpdatedAt, DateTime.UtcNow));

        await _context.SaveChangesAsync();

        var statusText = newStatus switch
        {
            0 => "INACTIVE",
            1 => "ACTIVE",
            2 => "SUSPENDED",
            _ => "UNKNOWN"
        };

        await _auditService.LogAsync(updatedBy, AuditActions.Update, "Reseller", id.ToString(),
            $"Status changed from {oldStatus} to {newStatus} with cascade update");

        return new ResellerStatusUpdateResultDto
        {
            ResellerId = id,
            CompanyName = reseller.CompanyName,
            NewStatus = newStatus,
            UsersUpdated = usersUpdated,
            TechniciansUpdated = techniciansUpdated,
            TagsUpdated = tagsUpdated,
            RolesUpdated = rolesUpdated,
            Message = $"Reseller '{reseller.CompanyName}' status changed to {statusText}. " +
                      $"Updated {usersUpdated} users, {techniciansUpdated} technicians, {tagsUpdated} tags, and {rolesUpdated} custom roles."
        };
    }
}

