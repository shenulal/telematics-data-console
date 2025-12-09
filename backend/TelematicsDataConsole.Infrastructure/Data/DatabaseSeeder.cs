using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TelematicsDataConsole.Core.Entities;

namespace TelematicsDataConsole.Infrastructure.Data;

public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(ApplicationDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            // Check if database can be connected
            _logger.LogInformation("Checking database connection...");

            if (!await _context.Database.CanConnectAsync())
            {
                _logger.LogError("Cannot connect to database. Please check connection string.");
                throw new Exception("Cannot connect to database");
            }

            // Check if tables exist and have correct schema
            var tablesValid = await CheckTablesValidAsync();

            if (!tablesValid)
            {
                _logger.LogInformation("Tables do not exist or schema mismatch. Recreating database schema...");
                // Drop and recreate all tables based on the DbContext model
                await _context.Database.EnsureDeletedAsync();
                await _context.Database.EnsureCreatedAsync();
                _logger.LogInformation("Database schema created successfully.");
            }
            else
            {
                _logger.LogInformation("Database tables are valid.");
            }

            // Seed Roles
            if (!await _context.Roles.AnyAsync())
            {
                _logger.LogInformation("Seeding roles...");
                var roles = new List<Role>
                {
                    new() { RoleName = SystemRoles.SuperAdmin, Description = "Super Administrator with full access", IsSystemRole = true },
                    new() { RoleName = SystemRoles.ResellerAdmin, Description = "Reseller Administrator with access to manage their technicians", IsSystemRole = true },
                    new() { RoleName = SystemRoles.Technician, Description = "Technician with access to verify IMEI data", IsSystemRole = true },
                    new() { RoleName = SystemRoles.Supervisor, Description = "Supervisor with oversight capabilities", IsSystemRole = true }
                };
                _context.Roles.AddRange(roles);
                await _context.SaveChangesAsync();
            }

            // Seed Permissions
            if (!await _context.Permissions.AnyAsync())
            {
                _logger.LogInformation("Seeding permissions...");
                var permissions = new List<Permission>
                {
                    new() { PermissionName = "technician.view", Description = "View technicians", Module = "Technician" },
                    new() { PermissionName = "technician.create", Description = "Create technicians", Module = "Technician" },
                    new() { PermissionName = "technician.edit", Description = "Edit technicians", Module = "Technician" },
                    new() { PermissionName = "technician.delete", Description = "Delete technicians", Module = "Technician" },
                    new() { PermissionName = "reseller.view", Description = "View resellers", Module = "Reseller" },
                    new() { PermissionName = "reseller.create", Description = "Create resellers", Module = "Reseller" },
                    new() { PermissionName = "reseller.edit", Description = "Edit resellers", Module = "Reseller" },
                    new() { PermissionName = "reseller.delete", Description = "Delete resellers", Module = "Reseller" },
                    new() { PermissionName = "imei.view", Description = "View IMEI data", Module = "IMEI" },
                    new() { PermissionName = "imei.verify", Description = "Verify IMEI devices", Module = "IMEI" },
                    new() { PermissionName = "imei.restriction.manage", Description = "Manage IMEI restrictions", Module = "IMEI" },
                    new() { PermissionName = "audit.view", Description = "View audit logs", Module = "Audit" }
                };
                _context.Permissions.AddRange(permissions);
                await _context.SaveChangesAsync();

                // Assign all permissions to Admin
                var adminRole = await _context.Roles.FirstAsync(r => r.RoleName == "Admin");
                var allPermissions = await _context.Permissions.ToListAsync();
                foreach (var perm in allPermissions)
                {
                    _context.RolePermissions.Add(new RolePermission { RoleId = adminRole.RoleId, PermissionId = perm.PermissionId });
                }

                // Assign permissions to Reseller
                var resellerRole = await _context.Roles.FirstAsync(r => r.RoleName == "Reseller");
                var resellerPerms = new[] { "technician.view", "technician.create", "technician.edit", "imei.view", "imei.restriction.manage" };
                foreach (var permName in resellerPerms)
                {
                    var perm = allPermissions.First(p => p.PermissionName == permName);
                    _context.RolePermissions.Add(new RolePermission { RoleId = resellerRole.RoleId, PermissionId = perm.PermissionId });
                }

                // Assign permissions to Technician
                var techRole = await _context.Roles.FirstAsync(r => r.RoleName == "Technician");
                var techPerms = new[] { "imei.view", "imei.verify" };
                foreach (var permName in techPerms)
                {
                    var perm = allPermissions.First(p => p.PermissionName == permName);
                    _context.RolePermissions.Add(new RolePermission { RoleId = techRole.RoleId, PermissionId = perm.PermissionId });
                }

                await _context.SaveChangesAsync();
            }

            // Seed Reseller
            if (!await _context.Resellers.AnyAsync())
            {
                _logger.LogInformation("Seeding resellers...");
                var reseller = new Reseller
                {
                    CompanyName = "Demo Fleet Solutions",
                    DisplayName = "Demo Fleet",
                    ContactPerson = "John Smith",
                    Email = "john@demofleet.com",
                    Mobile = "+971501234567",
                    City = "Dubai",
                    Country = "UAE",
                    Status = 1,
                    CreatedBy = 1
                };
                _context.Resellers.Add(reseller);
                await _context.SaveChangesAsync();
            }

            // Fix users with placeholder passwords
            var usersWithPlaceholderPasswords = await _context.Users
                .Where(u => u.PasswordHash.Contains("PLACEHOLDER"))
                .ToListAsync();

            if (usersWithPlaceholderPasswords.Any())
            {
                _logger.LogInformation("Fixing {Count} users with placeholder passwords...", usersWithPlaceholderPasswords.Count);
                foreach (var user in usersWithPlaceholderPasswords)
                {
                    if (user.Username == "admin")
                        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
                    else
                        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Tech@123");
                }
                await _context.SaveChangesAsync();
                _logger.LogInformation("Password hashes updated successfully!");
            }

            // Seed Users
            if (!await _context.Users.AnyAsync())
            {
                _logger.LogInformation("Seeding users...");

                // Admin user - Password: Admin@123
                var adminUser = new User
                {
                    Username = "admin",
                    Email = "admin@telematicsdc.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    FullName = "System Administrator",
                    Status = 1,
                    CreatedBy = 1
                };
                _context.Users.Add(adminUser);
                await _context.SaveChangesAsync();

                var adminRole = await _context.Roles.FirstAsync(r => r.RoleName == SystemRoles.SuperAdmin);
                _context.UserRoles.Add(new UserRole { UserId = adminUser.UserId, RoleId = adminRole.RoleId });

                // Technician user - Password: Tech@123
                var reseller = await _context.Resellers.FirstAsync();
                var techUser = new User
                {
                    Username = "tech1",
                    Email = "tech1@demofleet.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Tech@123"),
                    FullName = "Ahmed Hassan",
                    ResellerId = reseller.ResellerId,
                    Status = 1,
                    CreatedBy = 1
                };
                _context.Users.Add(techUser);
                await _context.SaveChangesAsync();

                var techRole = await _context.Roles.FirstAsync(r => r.RoleName == SystemRoles.Technician);
                _context.UserRoles.Add(new UserRole { UserId = techUser.UserId, RoleId = techRole.RoleId });

                // Create Technician record
                var technician = new Technician
                {
                    UserId = techUser.UserId,
                    ResellerId = reseller.ResellerId,
                    EmployeeCode = "TECH-001",
                    Skillset = "GPS Installation, Vehicle Tracking",
                    WorkRegion = "Dubai",
                    DailyLimit = 50,
                    Status = 1,
                    CreatedBy = 1
                };
                _context.Technicians.Add(technician);

                // Reseller Admin user - Password: Reseller@123
                var resellerAdminUser = new User
                {
                    Username = "reselleradmin",
                    Email = "reselleradmin@demofleet.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Reseller@123"),
                    FullName = "Fatima Al Mansouri",
                    ResellerId = reseller.ResellerId,
                    Status = 1,
                    CreatedBy = 1
                };
                _context.Users.Add(resellerAdminUser);
                await _context.SaveChangesAsync();

                var resellerAdminRole = await _context.Roles.FirstAsync(r => r.RoleName == SystemRoles.ResellerAdmin);
                _context.UserRoles.Add(new UserRole { UserId = resellerAdminUser.UserId, RoleId = resellerAdminRole.RoleId });

                // Supervisor user - Password: Super@123
                var supervisorUser = new User
                {
                    Username = "supervisor1",
                    Email = "supervisor@demofleet.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Super@123"),
                    FullName = "Omar Khalid",
                    ResellerId = reseller.ResellerId,
                    Status = 1,
                    CreatedBy = 1
                };
                _context.Users.Add(supervisorUser);
                await _context.SaveChangesAsync();

                var supervisorRole = await _context.Roles.FirstAsync(r => r.RoleName == SystemRoles.Supervisor);
                _context.UserRoles.Add(new UserRole { UserId = supervisorUser.UserId, RoleId = supervisorRole.RoleId });

                await _context.SaveChangesAsync();
                _logger.LogInformation("Database seeded successfully!");
                _logger.LogInformation("Admin: admin / Admin@123");
                _logger.LogInformation("Technician: tech1 / Tech@123");
                _logger.LogInformation("Reseller Admin: reselleradmin / Reseller@123");
                _logger.LogInformation("Supervisor: supervisor1 / Super@123");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding database");
            throw;
        }
    }

    private async Task<bool> CheckTablesValidAsync()
    {
        try
        {
            // Check if tables exist and have correct schema by trying to query with all columns
            var sql = @"
                SELECT TOP 1 1 FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = 'RolePermissions' AND COLUMN_NAME = 'CreatedBy'";

            using var command = _context.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;
            await _context.Database.OpenConnectionAsync();
            var result = await command.ExecuteScalarAsync();
            await _context.Database.CloseConnectionAsync();

            // If CreatedBy column exists, schema is valid
            return result != null;
        }
        catch
        {
            return false;
        }
    }
}

