using Auth.Models.Config;
using Auth.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Auth.Tests.TestSupport;

/// <summary>
/// Builds the services under test over one <see cref="FakeRepositoryManager"/>. Collaborating services are the real
/// implementations wherever they share that manager, so the interaction between them is exercised rather than stubbed;
/// the permission service is faked because its merge logic is covered on its own.
/// </summary>
public static class ServiceFactory
{
    public static ActionService Action(FakeRepositoryManager manager)
        => new(manager, NullLogger<ActionService>.Instance);

    public static ResourceService Resource(FakeRepositoryManager manager)
        => new(manager, NullLogger<ResourceService>.Instance, Action(manager));

    public static PermissionService Permission(FakeRepositoryManager manager, ILogger<PermissionService>? logger = null)
        => new(manager, logger ?? NullLogger<PermissionService>.Instance);

    public static RoleService Role(FakeRepositoryManager manager, IPermissionService permissionService)
        => new(manager, NullLogger<RoleService>.Instance, permissionService);

    public static UserService User(
        FakeRepositoryManager manager,
        AuthOptions? authOptions = null,
        IPermissionService? permissionService = null,
        IRoleService? roleService = null)
    {
        permissionService ??= new FakePermissionService();
        roleService ??= Role(manager, permissionService);

        return new UserService(manager, NullLogger<UserService>.Instance, Options.Create(authOptions ?? new AuthOptions()), roleService, permissionService);
    }
}
