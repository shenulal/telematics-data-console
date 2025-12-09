using Microsoft.EntityFrameworkCore.Storage;
using TelematicsDataConsole.Core.Entities;
using TelematicsDataConsole.Core.Interfaces;

namespace TelematicsDataConsole.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;

    private IRepository<User>? _users;
    private IRepository<Role>? _roles;
    private IRepository<Permission>? _permissions;
    private IRepository<UserRole>? _userRoles;
    private IRepository<RolePermission>? _rolePermissions;
    private IRepository<Technician>? _technicians;
    private IRepository<Reseller>? _resellers;
    private IRepository<ImeiRestriction>? _imeiRestrictions;
    private IRepository<Tag>? _tags;
    private IRepository<TagItem>? _tagItems;
    private IRepository<VerificationLog>? _verificationLogs;
    private IRepository<AuditLog>? _auditLogs;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IRepository<User> Users => _users ??= new Repository<User>(_context);
    public IRepository<Role> Roles => _roles ??= new Repository<Role>(_context);
    public IRepository<Permission> Permissions => _permissions ??= new Repository<Permission>(_context);
    public IRepository<UserRole> UserRoles => _userRoles ??= new Repository<UserRole>(_context);
    public IRepository<RolePermission> RolePermissions => _rolePermissions ??= new Repository<RolePermission>(_context);
    public IRepository<Technician> Technicians => _technicians ??= new Repository<Technician>(_context);
    public IRepository<Reseller> Resellers => _resellers ??= new Repository<Reseller>(_context);
    public IRepository<ImeiRestriction> ImeiRestrictions => _imeiRestrictions ??= new Repository<ImeiRestriction>(_context);
    public IRepository<Tag> Tags => _tags ??= new Repository<Tag>(_context);
    public IRepository<TagItem> TagItems => _tagItems ??= new Repository<TagItem>(_context);
    public IRepository<VerificationLog> VerificationLogs => _verificationLogs ??= new Repository<VerificationLog>(_context);
    public IRepository<AuditLog> AuditLogs => _auditLogs ??= new Repository<AuditLog>(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}

