using Microsoft.EntityFrameworkCore;
using TelematicsDataConsole.Core.DTOs.Dashboard;
using TelematicsDataConsole.Core.Interfaces.Services;
using TelematicsDataConsole.Infrastructure.Data;

namespace TelematicsDataConsole.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    private static string GetHourLabel(int hour)
    {
        if (hour == 0) return "12 AM";
        if (hour == 12) return "12 PM";
        return hour < 12 ? $"{hour} AM" : $"{hour - 12} PM";
    }

    private static VerificationSummaryDto CalculateVerificationSummary(
        List<Core.Entities.VerificationLog> verifications,
        DateTime today, DateTime weekStart, DateTime monthStart)
    {
        var daysWithData = verifications
            .Where(v => v.VerifiedAt >= monthStart)
            .Select(v => v.VerifiedAt.Date)
            .Distinct()
            .Count();

        var avgPerDay = daysWithData > 0
            ? (double)verifications.Count(v => v.VerifiedAt >= monthStart) / daysWithData
            : 0;

        return new VerificationSummaryDto
        {
            TotalVerificationsToday = verifications.Count(v => v.VerifiedAt >= today),
            TotalVerificationsThisWeek = verifications.Count(v => v.VerifiedAt >= weekStart),
            TotalVerificationsThisMonth = verifications.Count(v => v.VerifiedAt >= monthStart),
            TotalVerificationsAllTime = verifications.Count,
            UniqueDevicesToday = verifications.Where(v => v.VerifiedAt >= today).Select(v => v.DeviceId).Distinct().Count(),
            UniqueDevicesThisWeek = verifications.Where(v => v.VerifiedAt >= weekStart).Select(v => v.DeviceId).Distinct().Count(),
            UniqueDevicesThisMonth = verifications.Where(v => v.VerifiedAt >= monthStart).Select(v => v.DeviceId).Distinct().Count(),
            UniqueDevicesAllTime = verifications.Select(v => v.DeviceId).Distinct().Count(),
            AverageVerificationsPerDay = Math.Round(avgPerDay, 1)
        };
    }

    private static List<HourlyVerificationDto> CalculateHourlyBreakdown(
        List<Core.Entities.VerificationLog> verifications, DateTime today)
    {
        var todayVerifications = verifications.Where(v => v.VerifiedAt >= today).ToList();
        return Enumerable.Range(0, 24)
            .Select(hour => new HourlyVerificationDto
            {
                Hour = hour,
                HourLabel = GetHourLabel(hour),
                Count = todayVerifications.Count(v => v.VerifiedAt.Hour == hour)
            })
            .ToList();
    }

    public async Task<SuperAdminDashboardDto> GetSuperAdminDashboardAsync()
    {
        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var trendStart = today.AddDays(-30);

        var resellers = await _context.Resellers.ToListAsync();
        var users = await _context.Users.ToListAsync();
        var technicians = await _context.Technicians.Include(t => t.User).ToListAsync();
        var verifications = await _context.VerificationLogs.ToListAsync();

        // Top resellers by verification count this month
        var topResellers = resellers
            .Select(r => new ResellerSummaryDto
            {
                ResellerId = r.ResellerId,
                CompanyName = r.CompanyName,
                TechnicianCount = technicians.Count(t => t.ResellerId == r.ResellerId),
                VerificationsThisMonth = verifications.Count(v =>
                    technicians.Any(t => t.ResellerId == r.ResellerId && t.TechnicianId == v.TechnicianId)
                    && v.VerifiedAt >= monthStart)
            })
            .OrderByDescending(r => r.VerificationsThisMonth)
            .Take(5)
            .ToList();

        // Top technicians with reseller info
        var topTechnicians = technicians
            .Select(t => {
                var reseller = resellers.FirstOrDefault(r => r.ResellerId == t.ResellerId);
                return new TechnicianSummaryDto
                {
                    TechnicianId = t.TechnicianId,
                    Name = t.User?.FullName ?? t.User?.Username ?? $"Technician {t.TechnicianId}",
                    ResellerId = t.ResellerId,
                    ResellerName = reseller?.CompanyName ?? "N/A",
                    VerificationsToday = verifications.Count(v => v.TechnicianId == t.TechnicianId && v.VerifiedAt >= today),
                    VerificationsThisWeek = verifications.Count(v => v.TechnicianId == t.TechnicianId && v.VerifiedAt >= weekStart),
                    VerificationsThisMonth = verifications.Count(v => v.TechnicianId == t.TechnicianId && v.VerifiedAt >= monthStart),
                    LastVerificationAt = verifications.Where(v => v.TechnicianId == t.TechnicianId).Max(v => (DateTime?)v.VerifiedAt)
                };
            })
            .OrderByDescending(t => t.VerificationsThisMonth)
            .Take(10)
            .ToList();

        // Verification trend (last 30 days)
        var trend = Enumerable.Range(0, 30)
            .Select(i => trendStart.AddDays(i))
            .Select(date => new DailyVerificationDto
            {
                Date = date,
                Count = verifications.Count(v => v.VerifiedAt.Date == date)
            })
            .ToList();

        return new SuperAdminDashboardDto
        {
            TotalResellers = resellers.Count,
            ActiveResellers = resellers.Count(r => r.Status == 1),
            TotalUsers = users.Count,
            ActiveUsers = users.Count(u => u.Status == 1),
            TotalTechnicians = technicians.Count,
            ActiveTechnicians = technicians.Count(t => t.Status == 1),
            VerificationSummary = CalculateVerificationSummary(verifications, today, weekStart, monthStart),
            TopResellers = topResellers,
            TopTechnicians = topTechnicians,
            VerificationTrend = trend,
            HourlyBreakdown = CalculateHourlyBreakdown(verifications, today)
        };
    }

    public async Task<ResellerAdminDashboardDto> GetResellerAdminDashboardAsync(int resellerId)
    {
        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var trendStart = today.AddDays(-30);

        var reseller = await _context.Resellers.FindAsync(resellerId);
        var technicians = await _context.Technicians
            .Include(t => t.User)
            .Where(t => t.ResellerId == resellerId)
            .ToListAsync();
        var technicianIds = technicians.Select(t => t.TechnicianId).ToList();
        var verifications = await _context.VerificationLogs
            .Where(v => technicianIds.Contains(v.TechnicianId))
            .ToListAsync();
        var users = await _context.Users.Where(u => u.ResellerId == resellerId).CountAsync();

        var topTechnicians = technicians
            .Select(t => new TechnicianSummaryDto
            {
                TechnicianId = t.TechnicianId,
                Name = t.User?.FullName ?? t.User?.Username ?? $"Technician {t.TechnicianId}",
                ResellerId = resellerId,
                ResellerName = reseller?.CompanyName ?? "N/A",
                VerificationsToday = verifications.Count(v => v.TechnicianId == t.TechnicianId && v.VerifiedAt >= today),
                VerificationsThisWeek = verifications.Count(v => v.TechnicianId == t.TechnicianId && v.VerifiedAt >= weekStart),
                VerificationsThisMonth = verifications.Count(v => v.TechnicianId == t.TechnicianId && v.VerifiedAt >= monthStart),
                LastVerificationAt = verifications.Where(v => v.TechnicianId == t.TechnicianId).Max(v => (DateTime?)v.VerifiedAt)
            })
            .OrderByDescending(t => t.VerificationsThisMonth)
            .Take(10)
            .ToList();

        var trend = Enumerable.Range(0, 30)
            .Select(i => trendStart.AddDays(i))
            .Select(date => new DailyVerificationDto
            {
                Date = date,
                Count = verifications.Count(v => v.VerifiedAt.Date == date)
            })
            .ToList();

        return new ResellerAdminDashboardDto
        {
            ResellerId = resellerId,
            ResellerName = reseller?.CompanyName ?? "Unknown",
            TotalTechnicians = technicians.Count,
            ActiveTechnicians = technicians.Count(t => t.Status == 1),
            TotalUsers = users,
            VerificationSummary = CalculateVerificationSummary(verifications, today, weekStart, monthStart),
            TopTechnicians = topTechnicians,
            VerificationTrend = trend,
            HourlyBreakdown = CalculateHourlyBreakdown(verifications, today)
        };
    }

    public async Task<SupervisorDashboardDto> GetSupervisorDashboardAsync(int? resellerId = null)
    {
        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var trendStart = today.AddDays(-14);

        var techniciansQuery = _context.Technicians.Include(t => t.User).Include(t => t.Reseller).AsQueryable();
        if (resellerId.HasValue)
            techniciansQuery = techniciansQuery.Where(t => t.ResellerId == resellerId);

        var technicians = await techniciansQuery.ToListAsync();
        var technicianIds = technicians.Select(t => t.TechnicianId).ToList();
        var verifications = await _context.VerificationLogs
            .Where(v => technicianIds.Contains(v.TechnicianId))
            .ToListAsync();

        var techStats = technicians
            .Select(t => new TechnicianSummaryDto
            {
                TechnicianId = t.TechnicianId,
                Name = t.User?.FullName ?? t.User?.Username ?? $"Technician {t.TechnicianId}",
                ResellerId = t.ResellerId,
                ResellerName = t.Reseller?.CompanyName ?? "N/A",
                VerificationsToday = verifications.Count(v => v.TechnicianId == t.TechnicianId && v.VerifiedAt >= today),
                VerificationsThisWeek = verifications.Count(v => v.TechnicianId == t.TechnicianId && v.VerifiedAt >= weekStart),
                VerificationsThisMonth = verifications.Count(v => v.TechnicianId == t.TechnicianId && v.VerifiedAt >= monthStart),
                LastVerificationAt = verifications.Where(v => v.TechnicianId == t.TechnicianId).Max(v => (DateTime?)v.VerifiedAt)
            })
            .OrderByDescending(t => t.VerificationsToday)
            .ToList();

        var trend = Enumerable.Range(0, 14)
            .Select(i => trendStart.AddDays(i))
            .Select(date => new DailyVerificationDto
            {
                Date = date,
                Count = verifications.Count(v => v.VerifiedAt.Date == date)
            })
            .ToList();

        return new SupervisorDashboardDto
        {
            TotalTechnicians = technicians.Count,
            ActiveTechnicians = technicians.Count(t => t.Status == 1),
            VerificationSummary = CalculateVerificationSummary(verifications, today, weekStart, monthStart),
            TechnicianStats = techStats,
            VerificationTrend = trend,
            HourlyBreakdown = CalculateHourlyBreakdown(verifications, today)
        };
    }

    public async Task<TechnicianDashboardDto> GetTechnicianDashboardAsync(int technicianId)
    {
        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var trendStart = today.AddDays(-14);

        var technician = await _context.Technicians
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TechnicianId == technicianId);

        if (technician == null)
            return new TechnicianDashboardDto { TechnicianId = technicianId };

        var verifications = await _context.VerificationLogs
            .Where(v => v.TechnicianId == technicianId)
            .OrderByDescending(v => v.VerifiedAt)
            .ToListAsync();

        var verificationsToday = verifications.Count(v => v.VerifiedAt >= today);
        var dailyLimit = technician.DailyLimit;

        var recentVerifications = verifications
            .Take(10)
            .Select(v => new RecentVerificationDto
            {
                VerificationId = v.VerificationId,
                DeviceId = v.DeviceId,
                VerifiedAt = v.VerifiedAt
            })
            .ToList();

        var trend = Enumerable.Range(0, 14)
            .Select(i => trendStart.AddDays(i))
            .Select(date => new DailyVerificationDto
            {
                Date = date,
                Count = verifications.Count(v => v.VerifiedAt.Date == date)
            })
            .ToList();

        return new TechnicianDashboardDto
        {
            TechnicianId = technicianId,
            TechnicianName = technician.User?.FullName ?? technician.User?.Username ?? $"Technician {technicianId}",
            DailyLimit = dailyLimit,
            VerificationSummary = CalculateVerificationSummary(verifications, today, weekStart, monthStart),
            RemainingToday = Math.Max(0, dailyLimit - verificationsToday),
            LastVerificationAt = verifications.FirstOrDefault()?.VerifiedAt,
            RecentVerifications = recentVerifications,
            VerificationTrend = trend,
            HourlyBreakdown = CalculateHourlyBreakdown(verifications, today)
        };
    }
}

