using Auth.Common.Exceptions;
using Auth.Common.Models.Dto;
using Auth.Models.Config;
using Auth.Models.Dto;
using Auth.Models.Entities;
using Auth.Services;
using Auth.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Auth.Tests.Services;

public class UserServiceAuthenticationTests
{
    private readonly FakeRepositoryManager _manager = new();
    private readonly FakePermissionService _permissions = new();

    private static User User(string username, bool enabled = true, bool systemUser = false) => new()
    {
        Username = username,
        Enabled = enabled,
        SystemUser = systemUser,
        Roles = [],
    };

    private static AuthenticationTokenClaimsDto Claims(
        string? username = "jane",
        string? preferredUsername = null,
        string[]? roles = null,
        string? firstName = null,
        string? lastName = null,
        string? email = null)
        => new()
        {
            SubjectId = "subject-1",
            Username = username,
            PreferredUsername = preferredUsername,
            Roles = roles ?? [],
            FirstName = firstName,
            LastName = lastName,
            Email = email,
        };

    private static DbUpdateException UsernameUniqueViolation() => new("duplicate key", new PostgresException(
        messageText: "duplicate key value violates unique constraint \"IX_user_username\"",
        severity: "ERROR",
        invariantSeverity: "ERROR",
        sqlState: PostgresErrorCodes.UniqueViolation,
        constraintName: "IX_user_username"));

    private UserService Service(AuthOptions? options = null, IRoleService? roleService = null)
        => ServiceFactory.User(_manager, options, _permissions, roleService);

    #region Identify

    [Fact]
    public async Task IdentifyByCertificate_FindsTheUserByFingerprint()
    {
        var user = User("jane");
        user.CertificateFingerprint = TestCertificates.Sha256Fingerprint;
        _manager.UserRepository.Seed(user);

        var identified = await Service().IdentifyUserAsync(new AuthenticationRequestDto { CertificateContent = TestCertificates.Base64 });

        Assert.Equal("jane", identified.Username);
    }

    [Fact]
    public async Task IdentifyByCertificate_ReportsAnUnknownFingerprintAsNotFound()
    {
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(
            () => Service().IdentifyUserAsync(new AuthenticationRequestDto { CertificateContent = TestCertificates.Base64 }));

        Assert.Equal("User to identify not found", exception.Message);
    }

    [Fact]
    public async Task IdentifyByCertificate_RejectsContentThatIsNotBase64()
    {
        var exception = await Assert.ThrowsAsync<InvalidFormatException>(
            () => Service().IdentifyUserAsync(new AuthenticationRequestDto { CertificateContent = "not base64 at all" }));

        Assert.Equal("Wrong format of user authentication certificate.", exception.Message);
        Assert.IsType<FormatException>(exception.InnerException);
    }

    [Fact]
    public async Task IdentifyByClaims_FindsTheUserByUsername()
    {
        _manager.UserRepository.Seed(User("jane"));

        var identified = await Service().IdentifyUserAsync(new AuthenticationRequestDto { AuthenticationTokenUserClaims = Claims() });

        Assert.Equal("jane", identified.Username);
    }

    [Fact]
    public async Task IdentifyByClaims_FallsBackToThePreferredUsername()
    {
        _manager.UserRepository.Seed(User("jane"));

        var identified = await Service().IdentifyUserAsync(new AuthenticationRequestDto
        {
            AuthenticationTokenUserClaims = Claims(username: null, preferredUsername: "jane"),
        });

        Assert.Equal("jane", identified.Username);
    }

    [Fact]
    public async Task IdentifyByClaims_RejectsATokenThatNamesNoUser()
    {
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => Service().IdentifyUserAsync(new AuthenticationRequestDto { AuthenticationTokenUserClaims = Claims(username: null) }));

        Assert.Equal("Username not found in authentication token claims.", exception.Message);
    }

    [Fact]
    public async Task IdentifyByClaims_ReportsAnUnknownUsernameAsNotFound()
    {
        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => Service().IdentifyUserAsync(new AuthenticationRequestDto { AuthenticationTokenUserClaims = Claims() }));
    }

    [Fact]
    public async Task IdentifyBySystemUsername_IsNotSupported()
    {
        var exception = await Assert.ThrowsAsync<InvalidActionException>(
            () => Service().IdentifyUserAsync(new AuthenticationRequestDto { SystemUsername = "core" }));

        Assert.Equal("Cannot identify user by system username", exception.Message);
    }

    [Fact]
    public async Task IdentifyWithAnEmptyRequest_ReportsNotFound()
    {
        await Assert.ThrowsAsync<EntityNotFoundException>(() => Service().IdentifyUserAsync(new AuthenticationRequestDto()));
    }

    #endregion

    #region Certificate authentication

    [Fact]
    public async Task AuthenticateByCertificate_RejectsACertificateWhoseChainDoesNotBuild()
    {
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => Service().AuthenticateUserAsync(new AuthenticationRequestDto { CertificateContent = TestCertificates.Base64 }));

        Assert.Equal("User client certificate is invalid.", exception.Message);
        Assert.NotNull(exception.InnerException);
    }

    [Fact]
    public async Task AuthenticateByCertificate_RejectsContentThatIsNotBase64()
    {
        await Assert.ThrowsAsync<InvalidFormatException>(
            () => Service().AuthenticateUserAsync(new AuthenticationRequestDto { CertificateContent = "not base64 at all" }));
    }

    #endregion

    #region Token authentication

    [Fact]
    public async Task AuthenticateByClaims_ReturnsTheProfileOfAKnownUser()
    {
        var role = new Role { Name = "admin", Users = [] };
        var user = User("jane");
        user.Roles.Add(role);
        _manager.UserRepository.Seed(user);
        _manager.RoleRepository.Seed(role);

        var response = await Service().AuthenticateUserAsync(new AuthenticationRequestDto { AuthenticationTokenUserClaims = Claims() });

        Assert.True(response.Authenticated);
        Assert.NotNull(response.Data);
        Assert.Equal("jane", response.Data.User.Username);
        Assert.Equal("admin", Assert.Single(response.Data.Roles).Name);
        Assert.Same(_permissions.SubjectPermissions, response.Data.Permissions);
        Assert.Equal([user.Uuid], _permissions.UserPermissionsRequested);
    }

    [Fact]
    public async Task AuthenticateByClaims_RejectsAnUnknownUserWhenAutoRegistrationIsOff()
    {
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => Service().AuthenticateUserAsync(new AuthenticationRequestDto { AuthenticationTokenUserClaims = Claims() }));

        Assert.Equal("Unknown user with username 'jane'.", exception.Message);
        Assert.Empty(_manager.Transactions);
    }

    [Fact]
    public async Task AuthenticateByClaims_RejectsATokenThatNamesNoUser()
    {
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => Service().AuthenticateUserAsync(new AuthenticationRequestDto { AuthenticationTokenUserClaims = Claims(username: null) }));

        Assert.Equal("Username not found in authentication token claims.", exception.Message);
    }

    [Fact]
    public async Task AuthenticateByClaims_RegistersAnUnknownUserWhenAutoRegistrationIsOn()
    {
        var options = new AuthOptions { CreateUnknownUsers = true };

        var response = await Service(options).AuthenticateUserAsync(new AuthenticationRequestDto
        {
            AuthenticationTokenUserClaims = Claims(firstName: "Jane", lastName: "Doe", email: "jane@example.test"),
        });

        Assert.True(response.Authenticated);
        var stored = Assert.Single(_manager.UserRepository.Stored);
        Assert.Equal("jane", stored.Username);
        Assert.Equal("Jane", stored.FirstName);
        Assert.Equal("subject-1", stored.AuthTokenSubjectId);
        Assert.True(_manager.SingleTransaction().Committed);
    }

    [Fact]
    public async Task AuthenticateByClaims_RegistersAUserNamedOnlyByThePreferredUsername()
    {
        var options = new AuthOptions { CreateUnknownUsers = true };

        await Service(options).AuthenticateUserAsync(new AuthenticationRequestDto
        {
            AuthenticationTokenUserClaims = Claims(username: null, preferredUsername: "jane"),
        });

        Assert.Equal("jane", Assert.Single(_manager.UserRepository.Stored).Username);
    }

    [Fact]
    public async Task AuthenticateByClaims_AssignsAnExistingRoleToANewUser()
    {
        var role = new Role { Name = "admin", Users = [] };
        _manager.RoleRepository.Seed(role);
        var options = new AuthOptions { CreateUnknownUsers = true };

        var response = await Service(options).AuthenticateUserAsync(new AuthenticationRequestDto
        {
            AuthenticationTokenUserClaims = Claims(roles: ["admin"]),
        });

        Assert.Equal("admin", Assert.Single(response.Data!.Roles).Name);
        Assert.Equal([role], Assert.Single(_manager.UserRepository.Stored).Roles);
    }

    [Fact]
    public async Task AuthenticateByClaims_LeavesAnUnknownRoleUnassignedWhenRoleCreationIsOff()
    {
        var options = new AuthOptions { CreateUnknownUsers = true, CreateUnknownRoles = false };

        var response = await Service(options).AuthenticateUserAsync(new AuthenticationRequestDto
        {
            AuthenticationTokenUserClaims = Claims(roles: ["admin"]),
        });

        Assert.Empty(response.Data!.Roles);
        Assert.Empty(_manager.RoleRepository.Stored);
    }

    [Fact]
    public async Task AuthenticateByClaims_CreatesAndAssignsAnUnknownRoleWhenRoleCreationIsOn()
    {
        var options = new AuthOptions { CreateUnknownUsers = true, CreateUnknownRoles = true };

        var response = await Service(options).AuthenticateUserAsync(new AuthenticationRequestDto
        {
            AuthenticationTokenUserClaims = Claims(roles: ["admin"]),
        });

        Assert.Equal("admin", Assert.Single(_manager.RoleRepository.Stored).Name);
        Assert.Equal("admin", Assert.Single(response.Data!.Roles).Name);
    }

    [Fact]
    public async Task AuthenticateByClaims_SyncsTheProfileFieldsOfAKnownUserUnderTheSyncPolicy()
    {
        var user = User("jane");
        _manager.UserRepository.Seed(user);
        var options = new AuthOptions { SyncPolicy = SyncPolicy.SyncData };

        await Service(options).AuthenticateUserAsync(new AuthenticationRequestDto
        {
            AuthenticationTokenUserClaims = Claims(firstName: "Jane", lastName: "Doe", email: "jane@example.test"),
        });

        Assert.Equal("Jane", user.FirstName);
        Assert.Equal("Doe", user.LastName);
        Assert.Equal("jane@example.test", user.Email);
    }

    [Fact]
    public async Task AuthenticateByClaims_LeavesTheProfileOfAKnownUserAloneUnderTheDefaultPolicy()
    {
        var user = User("jane");
        user.FirstName = "unchanged";
        _manager.UserRepository.Seed(user);

        await Service().AuthenticateUserAsync(new AuthenticationRequestDto
        {
            AuthenticationTokenUserClaims = Claims(firstName: "Jane"),
        });

        Assert.Equal("unchanged", user.FirstName);
    }

    [Fact]
    public async Task AuthenticateByClaims_AddsAMissingRoleToAKnownUserUnderTheSyncPolicy()
    {
        var role = new Role { Name = "admin", Users = [] };
        var user = User("jane");
        _manager.UserRepository.Seed(user);
        _manager.RoleRepository.Seed(role);
        var options = new AuthOptions { SyncPolicy = SyncPolicy.SyncData };

        await Service(options).AuthenticateUserAsync(new AuthenticationRequestDto
        {
            AuthenticationTokenUserClaims = Claims(roles: ["admin"]),
        });

        Assert.Equal([role], user.Roles);
    }

    [Fact]
    public async Task AuthenticateByClaims_UnassignsARoleTheTokenNoLongerCarriesUnderTheSyncPolicy()
    {
        var stale = new Role { Name = "auditor", Users = [] };
        var kept = new Role { Name = "admin", Users = [] };
        var user = User("jane");
        user.Roles.Add(stale);
        _manager.UserRepository.Seed(user);
        _manager.RoleRepository.Seed(stale, kept);
        var options = new AuthOptions { SyncPolicy = SyncPolicy.SyncData };

        await Service(options).AuthenticateUserAsync(new AuthenticationRequestDto
        {
            AuthenticationTokenUserClaims = Claims(roles: ["admin"]),
        });

        Assert.Equal(["admin"], user.Roles.Select(r => r.Name));
    }

    [Fact]
    public async Task AuthenticateByClaims_KeepsTheRolesOfAKnownUserUnderTheDefaultPolicy()
    {
        var stale = new Role { Name = "auditor", Users = [] };
        var available = new Role { Name = "admin", Users = [] };
        var user = User("jane");
        user.Roles.Add(stale);
        _manager.UserRepository.Seed(user);
        _manager.RoleRepository.Seed(stale, available);

        await Service().AuthenticateUserAsync(new AuthenticationRequestDto
        {
            AuthenticationTokenUserClaims = Claims(roles: ["admin"]),
        });

        Assert.Equal(["auditor"], user.Roles.Select(r => r.Name));
    }

    [Fact]
    public async Task AuthenticateByClaims_DoesNotReassignARoleTheUserAlreadyHoldsUnderTheSyncPolicy()
    {
        var role = new Role { Name = "admin", Users = [] };
        var user = User("jane");
        user.Roles.Add(role);
        _manager.UserRepository.Seed(user);
        _manager.RoleRepository.Seed(role);
        var options = new AuthOptions { SyncPolicy = SyncPolicy.SyncData };

        await Service(options).AuthenticateUserAsync(new AuthenticationRequestDto
        {
            AuthenticationTokenUserClaims = Claims(roles: ["admin"]),
        });

        Assert.Single(user.Roles);
    }

    [Fact]
    public async Task AuthenticateByClaims_ContinuesWithTheRowAnotherInstanceCommittedFirst()
    {
        var options = new AuthOptions { CreateUnknownUsers = true };
        var winner = User("jane");
        _manager.OnSave = attempt =>
        {
            if (attempt != 1) return null;

            _manager.UserRepository.Seed(winner);
            return UsernameUniqueViolation();
        };

        var response = await Service(options).AuthenticateUserAsync(new AuthenticationRequestDto
        {
            AuthenticationTokenUserClaims = Claims(),
        });

        Assert.True(response.Authenticated);
        Assert.Equal(winner.Uuid, response.Data!.User.Uuid);
        Assert.Single(_manager.UserRepository.Stored);
        Assert.Single(_manager.DetachedEntities);
        Assert.True(_manager.SingleTransaction().Committed);
    }

    [Fact]
    public async Task AuthenticateByClaims_FailsWhenTheConflictingRowCannotBeFoundAfterAll()
    {
        var options = new AuthOptions { CreateUnknownUsers = true };
        _manager.OnSave = attempt => attempt == 1 ? UsernameUniqueViolation() : null;

        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => Service(options).AuthenticateUserAsync(new AuthenticationRequestDto { AuthenticationTokenUserClaims = Claims() }));

        Assert.Equal("Error in creating user or assigning roles based on authentication token.", exception.Message);
        Assert.IsType<DbUpdateException>(exception.InnerException);
        Assert.True(_manager.SingleTransaction().RolledBack);
    }

    [Fact]
    public async Task AuthenticateByClaims_RollsBackAndReportsAFailureThatIsNotAUsernameConflict()
    {
        var options = new AuthOptions { CreateUnknownUsers = true };
        _manager.OnSave = _ => new DbUpdateException("connection reset", new Exception("socket closed"));

        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => Service(options).AuthenticateUserAsync(new AuthenticationRequestDto { AuthenticationTokenUserClaims = Claims() }));

        Assert.Equal("Error in creating user or assigning roles based on authentication token.", exception.Message);
        Assert.True(_manager.SingleTransaction().RolledBack);
        Assert.Empty(_manager.UserRepository.Stored);
    }

    [Fact]
    public async Task AuthenticateByClaims_LeavesTheTransactionOpenWhenRoleHandlingReportsUnauthorized()
    {
        // The rethrow of an UnauthorizedException skips the rollback that every other failure goes through.
        var options = new AuthOptions { CreateUnknownUsers = true, CreateUnknownRoles = true };

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => Service(options, new ThrowingRoleService()).AuthenticateUserAsync(new AuthenticationRequestDto
            {
                AuthenticationTokenUserClaims = Claims(roles: ["admin"]),
            }));

        var transaction = _manager.SingleTransaction();
        Assert.False(transaction.RolledBack);
        Assert.False(transaction.Committed);
    }

    #endregion

    #region System username, UUID and anonymous

    [Fact]
    public async Task AuthenticateBySystemUsername_ReturnsTheProfileOfTheSystemUser()
    {
        _manager.UserRepository.Seed(User("core", systemUser: true));

        var response = await Service().AuthenticateUserAsync(new AuthenticationRequestDto { SystemUsername = "core" });

        Assert.True(response.Authenticated);
        Assert.Equal("core", response.Data!.User.Username);
    }

    [Fact]
    public async Task AuthenticateBySystemUsername_RefusesAUserThatIsNotASystemUser()
    {
        _manager.UserRepository.Seed(User("core"));

        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => Service().AuthenticateUserAsync(new AuthenticationRequestDto { SystemUsername = "core" }));

        Assert.Equal("Unknown system user for specified username: core", exception.Message);
    }

    [Fact]
    public async Task AuthenticateByUuid_ReturnsTheProfileOfThatUser()
    {
        var user = User("jane");
        _manager.UserRepository.Seed(user);

        var response = await Service().AuthenticateUserAsync(new AuthenticationRequestDto { UserUuid = user.Uuid.ToString() });

        Assert.True(response.Authenticated);
        Assert.Equal(user.Uuid, response.Data!.User.Uuid);
    }

    [Fact]
    public async Task AuthenticateByUuid_RefusesAnUnknownUuid()
    {
        var uuid = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => Service().AuthenticateUserAsync(new AuthenticationRequestDto { UserUuid = uuid.ToString() }));

        Assert.Equal($"Unknown user for specified UUID: {uuid}", exception.Message);
    }

    [Fact]
    public async Task AuthenticateWithAnEmptyRequest_IsAnonymous()
    {
        var response = await Service().AuthenticateUserAsync(new AuthenticationRequestDto());

        Assert.False(response.Authenticated);
        Assert.Null(response.Data);
    }

    [Fact]
    public async Task AuthenticateADisabledUser_IsRefused()
    {
        var user = User("jane", enabled: false);
        _manager.UserRepository.Seed(user);

        var exception = await Assert.ThrowsAsync<UnauthorizedException>(
            () => Service().AuthenticateUserAsync(new AuthenticationRequestDto { UserUuid = user.Uuid.ToString() }));

        Assert.Equal("User 'jane' is disabled", exception.Message);
    }

    [Fact]
    public async Task AuthenticateAUserWithoutARoleCollection_ReportsNoRoles()
    {
        var user = User("jane");
        user.Roles = null!;
        _manager.UserRepository.Seed(user);

        var response = await Service().AuthenticateUserAsync(new AuthenticationRequestDto { UserUuid = user.Uuid.ToString() });

        Assert.Empty(response.Data!.Roles);
    }

    #endregion

    private sealed class ThrowingRoleService : IRoleService
    {
        public Task<RoleDetailDto> CreateAsync(ICrudRequestDto dto) => throw new UnauthorizedException("role creation refused");

        public Task<PagedResponse<RoleDto>> GetAsync(IQueryRequestDto dto) => throw new NotSupportedException();

        public Task<RoleDetailDto> GetDetailAsync(Guid key) => throw new NotSupportedException();

        public Task<RoleDetailDto> UpdateAsync(Guid key, ICrudRequestDto dto) => throw new NotSupportedException();

        public Task DeleteAsync(Guid key) => throw new NotSupportedException();

        public Task<List<RoleDto>> GetUserRolesAsync(Guid userUuid) => throw new NotSupportedException();

        public Task<RoleDetailDto> AssignUsersAsync(Guid roleUuid, IEnumerable<Guid> userUuids) => throw new NotSupportedException();
    }
}
