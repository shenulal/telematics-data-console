using Microsoft.EntityFrameworkCore;
using TelematicsDataConsole.Core.Entities;

namespace TelematicsDataConsole.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Technician> Technicians => Set<Technician>();
    public DbSet<Reseller> Resellers => Set<Reseller>();
    public DbSet<ImeiRestriction> ImeiRestrictions => Set<ImeiRestriction>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<TagItem> TagItems => Set<TagItem>();
    public DbSet<VerificationLog> VerificationLogs => Set<VerificationLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.UserId);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(200).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.HasOne(e => e.Reseller).WithMany(r => r.Users).HasForeignKey(e => e.ResellerId);
        });

        // Role configuration
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(e => e.RoleId);
            entity.HasIndex(e => e.RoleName).IsUnique();
            entity.Property(e => e.RoleName).HasMaxLength(50).IsRequired();
        });

        // Permission configuration
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permissions");
            entity.HasKey(e => e.PermissionId);
            entity.HasIndex(e => e.PermissionName).IsUnique();
            entity.Property(e => e.PermissionName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Module).HasMaxLength(50);
        });

        // UserRole configuration
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles");
            entity.HasKey(e => e.UserRoleId);
            entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
            entity.HasOne(e => e.User).WithMany(u => u.UserRoles).HasForeignKey(e => e.UserId);
            entity.HasOne(e => e.Role).WithMany(r => r.UserRoles).HasForeignKey(e => e.RoleId);
        });

        // RolePermission configuration
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions");
            entity.HasKey(e => e.RolePermissionId);
            entity.HasIndex(e => new { e.RoleId, e.PermissionId }).IsUnique();
            entity.HasOne(e => e.Role).WithMany(r => r.RolePermissions).HasForeignKey(e => e.RoleId);
            entity.HasOne(e => e.Permission).WithMany(p => p.RolePermissions).HasForeignKey(e => e.PermissionId);
        });

        ConfigureTechnicianAndReseller(modelBuilder);
        ConfigureImeiAndTags(modelBuilder);
        ConfigureLogging(modelBuilder);
    }

    private static void ConfigureTechnicianAndReseller(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Technician>(entity =>
        {
            entity.ToTable("Technicians");
            entity.HasKey(e => e.TechnicianId);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.EmployeeCode).HasMaxLength(50);
            entity.HasOne(e => e.User).WithOne(u => u.Technician).HasForeignKey<Technician>(e => e.UserId);
            entity.HasOne(e => e.Reseller).WithMany(r => r.Technicians).HasForeignKey(e => e.ResellerId);
        });

        modelBuilder.Entity<Reseller>(entity =>
        {
            entity.ToTable("Resellers");
            entity.HasKey(e => e.ResellerId);
            entity.Property(e => e.CompanyName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(200);
        });
    }

    private static void ConfigureImeiAndTags(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ImeiRestriction>(entity =>
        {
            entity.ToTable("ImeiRestrictions");
            entity.HasKey(e => e.RestrictionId);
            entity.HasIndex(e => new { e.TechnicianId, e.DeviceId });
            entity.HasOne(e => e.Technician).WithMany(t => t.ImeiRestrictions).HasForeignKey(e => e.TechnicianId);
            entity.HasOne(e => e.Tag).WithMany(t => t.ImeiRestrictions).HasForeignKey(e => e.TagId);
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.ToTable("Tags");
            entity.HasKey(e => e.TagId);
            entity.Property(e => e.TagName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Color).HasMaxLength(20);
            entity.HasOne(e => e.Reseller).WithMany(r => r.Tags).HasForeignKey(e => e.ResellerId);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
        });

        modelBuilder.Entity<TagItem>(entity =>
        {
            entity.ToTable("TagItems");
            entity.HasKey(e => e.TagItemId);
            entity.HasIndex(e => new { e.TagId, e.EntityType, e.EntityId });
            entity.Property(e => e.EntityIdentifier).HasMaxLength(100);
            entity.HasOne(e => e.Tag).WithMany(t => t.TagItems).HasForeignKey(e => e.TagId);
        });
    }

    private static void ConfigureLogging(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VerificationLog>(entity =>
        {
            entity.ToTable("VerificationLogs");
            entity.HasKey(e => e.VerificationId);
            entity.HasIndex(e => e.DeviceId);
            entity.HasIndex(e => e.TechnicianId);
            entity.HasIndex(e => e.VerifiedAt);
            entity.HasIndex(e => new { e.TechnicianId, e.DeviceId, e.VerifiedAt });
            entity.HasOne(e => e.Technician).WithMany(t => t.VerificationLogs).HasForeignKey(e => e.TechnicianId);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(e => e.AuditId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
            entity.Property(e => e.Action).HasMaxLength(50).IsRequired();
            entity.Property(e => e.EntityType).HasMaxLength(100).IsRequired();
        });
    }
}

