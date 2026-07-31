using Auth.Data.Contracts;
using Microsoft.EntityFrameworkCore.Storage;

namespace Auth.Tests.TestSupport;

public sealed class FakeRepositoryManager : IRepositoryManager
{
    public FakeUserRepository UserRepository { get; } = new();
    public FakeRoleRepository RoleRepository { get; } = new();
    public FakePermissionRepository PermissionRepository { get; } = new();
    public FakeResourceRepository ResourceRepository { get; } = new();
    public FakeActionRepository ActionRepository { get; } = new();

    public IUserRepository User => UserRepository;
    public IRoleRepository Role => RoleRepository;
    public IPermissionRepository Permission => PermissionRepository;
    public IResourceRepository Resource => ResourceRepository;
    public IActionRepository Action => ActionRepository;

    public int SaveCount { get; private set; }
    public List<FakeDbContextTransaction> Transactions { get; } = [];
    public List<object> DetachedEntities { get; } = [];

    /// <summary>
    /// Lets a test fail a chosen save attempt (1-based). Returning an exception throws it and leaves the staged writes
    /// staged, as a failed <c>SaveChanges</c> does; the callback runs before the throw so it can also seed the store to
    /// stand in for a row another instance committed in the meantime.
    /// </summary>
    public Func<int, Exception?>? OnSave { get; set; }

    public Task<IDbContextTransaction> BeginTransactionAsync()
    {
        var transaction = new FakeDbContextTransaction();
        Transactions.Add(transaction);

        return Task.FromResult<IDbContextTransaction>(transaction);
    }

    public Task SaveAsync()
    {
        SaveCount++;

        var failure = OnSave?.Invoke(SaveCount);
        if (failure != null) throw failure;

        UserRepository.Flush();
        RoleRepository.Flush();
        PermissionRepository.Flush();
        ResourceRepository.Flush();
        ActionRepository.Flush();

        FixUpPermissionNavigations();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stands in for the navigation fix-up a change tracker performs: a permission written with only its foreign keys
    /// set comes back from a later read with <c>Role</c>, <c>Resource</c> and <c>Action</c> populated, which is what the
    /// permission merge reads.
    /// </summary>
    private void FixUpPermissionNavigations()
    {
        foreach (var permission in PermissionRepository.Stored)
        {
            permission.Role ??= RoleRepository.Stored.FirstOrDefault(r => r.Uuid == permission.RoleUuid)!;

            if (permission.ResourceUuid.HasValue)
                permission.Resource ??= ResourceRepository.Stored.FirstOrDefault(r => r.Uuid == permission.ResourceUuid.Value);

            if (permission.ActionUuid.HasValue)
                permission.Action ??= ActionRepository.Stored.FirstOrDefault(a => a.Uuid == permission.ActionUuid.Value);
        }
    }

    public void Detach(object entity)
    {
        DetachedEntities.Add(entity);

        _ = UserRepository.Detach(entity)
            || RoleRepository.Detach(entity)
            || PermissionRepository.Detach(entity)
            || ResourceRepository.Detach(entity)
            || ActionRepository.Detach(entity);
    }

    public FakeDbContextTransaction SingleTransaction() => Assert.Single(Transactions);
}
