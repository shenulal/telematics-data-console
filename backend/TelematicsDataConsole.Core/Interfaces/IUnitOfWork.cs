using TelematicsDataConsole.Core.Entities;

namespace TelematicsDataConsole.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<Role> Roles { get; }
    IRepository<Permission> Permissions { get; }
    IRepository<UserRole> UserRoles { get; }
    IRepository<RolePermission> RolePermissions { get; }
    IRepository<Technician> Technicians { get; }
    IRepository<Reseller> Resellers { get; }
    IRepository<ImeiRestriction> ImeiRestrictions { get; }
    IRepository<Tag> Tags { get; }
    IRepository<TagItem> TagItems { get; }
    IRepository<VerificationLog> VerificationLogs { get; }
    IRepository<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}

