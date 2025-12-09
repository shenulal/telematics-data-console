using Microsoft.EntityFrameworkCore;
using TelematicsDataConsole.Core.DTOs;
using TelematicsDataConsole.Core.DTOs.Technician;
using TelematicsDataConsole.Core.Entities;
using TelematicsDataConsole.Core.Interfaces.Services;
using TelematicsDataConsole.Infrastructure.Data;

namespace TelematicsDataConsole.Infrastructure.Services;

public class TechnicianService : ITechnicianService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public TechnicianService(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<PagedResult<TechnicianDto>> GetAllAsync(TechnicianFilterDto filter)
    {
        var query = _context.Technicians
            .Include(t => t.User)
            .Include(t => t.Reseller)
            .AsQueryable();

        if (filter.ResellerId.HasValue)
            query = query.Where(t => t.ResellerId == filter.ResellerId);

        if (filter.Status.HasValue)
            query = query.Where(t => t.Status == filter.Status);

        if (!string.IsNullOrEmpty(filter.WorkRegion))
            query = query.Where(t => t.WorkRegion == filter.WorkRegion);

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            query = query.Where(t =>
                t.User.Username.Contains(filter.SearchTerm) ||
                t.User.Email.Contains(filter.SearchTerm) ||
                (t.User.FullName != null && t.User.FullName.Contains(filter.SearchTerm)) ||
                (t.EmployeeCode != null && t.EmployeeCode.Contains(filter.SearchTerm)));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(t => MapToDto(t))
            .ToListAsync();

        return new PagedResult<TechnicianDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<TechnicianDto?> GetByIdAsync(int id)
    {
        var technician = await _context.Technicians
            .Include(t => t.User)
            .Include(t => t.Reseller)
            .FirstOrDefaultAsync(t => t.TechnicianId == id);

        return technician != null ? MapToDto(technician) : null;
    }

    public async Task<TechnicianDto?> GetByUserIdAsync(int userId)
    {
        var technician = await _context.Technicians
            .Include(t => t.User)
            .Include(t => t.Reseller)
            .FirstOrDefaultAsync(t => t.UserId == userId);

        return technician != null ? MapToDto(technician) : null;
    }

    public async Task<TechnicianDto> CreateAsync(CreateTechnicianDto dto, int createdBy)
    {
        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FullName = dto.FullName,
            ResellerId = dto.ResellerId,
            Status = (short)UserStatus.Active,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = createdBy,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Assign Technician role
        var techRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == SystemRoles.Technician);
        if (techRole != null)
        {
            await _context.UserRoles.AddAsync(new UserRole { UserId = user.UserId, RoleId = techRole.RoleId });
        }

        var technician = new Technician
        {
            UserId = user.UserId,
            ResellerId = dto.ResellerId,
            EmployeeCode = dto.EmployeeCode,
            Skillset = dto.Skillset,
            Certification = dto.Certification,
            WorkRegion = dto.WorkRegion,
            DailyLimit = dto.DailyLimit,
            Status = (short)TechnicianStatus.Active,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };

        await _context.Technicians.AddAsync(technician);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(createdBy, AuditActions.Create, "Technician", technician.TechnicianId.ToString(), null, dto);

        return (await GetByIdAsync(technician.TechnicianId))!;
    }

    public async Task<TechnicianDto> UpdateAsync(int id, UpdateTechnicianDto dto, int updatedBy)
    {
        var technician = await _context.Technicians.Include(t => t.User).FirstOrDefaultAsync(t => t.TechnicianId == id)
            ?? throw new KeyNotFoundException("Technician not found");

        var oldValues = MapToDto(technician);

        if (dto.FullName != null) technician.User.FullName = dto.FullName;
        if (dto.ResellerId.HasValue) technician.ResellerId = dto.ResellerId;
        if (dto.EmployeeCode != null) technician.EmployeeCode = dto.EmployeeCode;
        if (dto.Skillset != null) technician.Skillset = dto.Skillset;
        if (dto.Certification != null) technician.Certification = dto.Certification;
        if (dto.WorkRegion != null) technician.WorkRegion = dto.WorkRegion;
        if (dto.DailyLimit.HasValue) technician.DailyLimit = dto.DailyLimit.Value;
        if (dto.Status.HasValue) technician.Status = dto.Status.Value;

        technician.UpdatedBy = updatedBy;
        technician.UpdatedAt = DateTime.UtcNow;
        technician.User.UpdatedBy = updatedBy;
        technician.User.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _auditService.LogAsync(updatedBy, AuditActions.Update, "Technician", id.ToString(), oldValues, dto);

        return (await GetByIdAsync(id))!;
    }

    public async Task<bool> DeactivateAsync(int id, int updatedBy) => await UpdateStatusAsync(id, TechnicianStatus.Inactive, updatedBy);
    public async Task<bool> ActivateAsync(int id, int updatedBy) => await UpdateStatusAsync(id, TechnicianStatus.Active, updatedBy);

    private async Task<bool> UpdateStatusAsync(int id, TechnicianStatus status, int updatedBy)
    {
        var technician = await _context.Technicians.FindAsync(id);
        if (technician == null) return false;
        technician.Status = (short)status;
        technician.UpdatedBy = updatedBy;
        technician.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<TechnicianDto>> GetByResellerAsync(int resellerId)
    {
        return await _context.Technicians
            .Include(t => t.User)
            .Where(t => t.ResellerId == resellerId)
            .Select(t => MapToDto(t))
            .ToListAsync();
    }

    public async Task<TechnicianStatsDto> GetStatsAsync(int technicianId, DateTime? from = null, DateTime? to = null)
    {
        var technician = await _context.Technicians.FindAsync(technicianId) ?? throw new KeyNotFoundException();
        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var verifications = await _context.VerificationLogs.Where(v => v.TechnicianId == technicianId).ToListAsync();

        return new TechnicianStatsDto
        {
            TechnicianId = technicianId,
            TotalVerifications = verifications.Count,
            VerificationsToday = verifications.Count(v => v.VerifiedAt.Date == today),
            VerificationsThisWeek = verifications.Count(v => v.VerifiedAt >= weekStart),
            VerificationsThisMonth = verifications.Count(v => v.VerifiedAt >= monthStart),
            LastVerificationAt = verifications.OrderByDescending(v => v.VerifiedAt).FirstOrDefault()?.VerifiedAt,
            RemainingDailyLimit = technician.DailyLimit - verifications.Count(v => v.VerifiedAt.Date == today)
        };
    }

    private static TechnicianDto MapToDto(Technician t) => new()
    {
        TechnicianId = t.TechnicianId,
        UserId = t.UserId,
        Username = t.User.Username,
        Email = t.User.Email,
        FullName = t.User.FullName,
        ResellerId = t.ResellerId,
        ResellerName = t.Reseller?.CompanyName,
        EmployeeCode = t.EmployeeCode,
        Skillset = t.Skillset,
        Certification = t.Certification,
        WorkRegion = t.WorkRegion,
        DailyLimit = t.DailyLimit,
        Status = t.Status,
        CreatedAt = t.CreatedAt,
        LastLoginAt = t.User.LastLoginAt
    };
}

