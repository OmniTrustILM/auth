using Auth.Common.Models.Dto;
using Auth.Models.Dto;
using Auth.Models.Entities;
using Auth.Models.Mappings;

namespace Auth.Tests.Mappings;

public class UserMapperTests
{
    private static readonly Guid CertificateUuid = Guid.NewGuid();

    private static User StoredUser() => new()
    {
        Uuid = Guid.NewGuid(),
        CreatedAt = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
        UpdatedAt = new DateTimeOffset(2026, 2, 3, 4, 5, 6, TimeSpan.Zero),
        Username = "jane",
        FirstName = "Jane",
        LastName = "Doe",
        Email = "jane@example.test",
        Description = "operator",
        Groups = [new NameAndUuidDto { Uuid = Guid.NewGuid(), Name = "operators" }],
        Enabled = true,
        SystemUser = false,
        CertificateUuid = CertificateUuid,
        CertificateFingerprint = "abc123",
        Roles = [],
    };

    [Fact]
    public void TokenClaims_BecomeAUserWithNoRolesYet()
    {
        var claims = new AuthenticationTokenClaimsDto
        {
            SubjectId = "subject-1",
            Username = "jane",
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@example.test",
        };

        var user = claims.ToEntity();

        Assert.Equal("jane", user.Username);
        Assert.Equal("Jane", user.FirstName);
        Assert.Equal("Doe", user.LastName);
        Assert.Equal("jane@example.test", user.Email);
        Assert.Equal("subject-1", user.AuthTokenSubjectId);
        Assert.True(user.Enabled);
        Assert.Empty(user.Roles);
    }

    [Fact]
    public void TokenClaims_DoNotFallBackToThePreferredUsername()
    {
        // The caller resolves the username and assigns it; the mapper copies only the 'username' claim.
        var user = new AuthenticationTokenClaimsDto { SubjectId = "subject-1", PreferredUsername = "jane" }.ToEntity();

        Assert.Null(user.Username);
    }

    [Fact]
    public void TokenClaims_CarryTheDisabledFlagThrough()
    {
        Assert.False(new AuthenticationTokenClaimsDto { SubjectId = "s", Username = "jane", Enabled = false }.ToEntity().Enabled);
    }

    [Fact]
    public void CreateRequest_BecomesAFullyPopulatedUser()
    {
        var groups = new List<NameAndUuidDto> { new() { Uuid = Guid.NewGuid(), Name = "operators" } };
        var dto = new UserRequestDto
        {
            Username = "jane",
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@example.test",
            Description = "operator",
            Groups = groups,
            Enabled = true,
            SystemUser = true,
            CertificateUuid = CertificateUuid,
            CertificateFingerprint = "abc123",
        };

        var user = dto.ToEntity();

        Assert.Equal("jane", user.Username);
        Assert.Equal("Jane", user.FirstName);
        Assert.Equal("Doe", user.LastName);
        Assert.Equal("jane@example.test", user.Email);
        Assert.Equal("operator", user.Description);
        Assert.Same(groups, user.Groups);
        Assert.True(user.Enabled);
        Assert.True(user.SystemUser);
        Assert.Equal(CertificateUuid, user.CertificateUuid);
        Assert.Equal("abc123", user.CertificateFingerprint);
    }

    [Fact]
    public void CreateRequest_TreatsOmittedFlagsAsFalse()
    {
        var user = new UserRequestDto { Username = "jane", Enabled = null, SystemUser = null }.ToEntity();

        Assert.False(user.Enabled);
        Assert.False(user.SystemUser);
    }

    [Fact]
    public void UpdateRequest_OverwritesOnlyTheUpdatableMembers()
    {
        var user = StoredUser();
        var newGroups = new List<NameAndUuidDto> { new() { Uuid = Guid.NewGuid(), Name = "auditors" } };

        new UserUpdateRequestDto
        {
            FirstName = "Janet",
            LastName = "Roe",
            Email = "janet@example.test",
            Description = "auditor",
            Groups = newGroups,
            CertificateUuid = null,
            CertificateFingerprint = null,
        }.ApplyTo(user);

        Assert.Equal("Janet", user.FirstName);
        Assert.Equal("Roe", user.LastName);
        Assert.Equal("janet@example.test", user.Email);
        Assert.Equal("auditor", user.Description);
        Assert.Same(newGroups, user.Groups);
        Assert.Null(user.CertificateUuid);
        Assert.Null(user.CertificateFingerprint);

        Assert.Equal("jane", user.Username);
        Assert.True(user.Enabled);
        Assert.False(user.SystemUser);
    }

    [Fact]
    public void ListDto_CarriesTheIdentityAndTimestamps()
    {
        var user = StoredUser();

        var dto = user.ToDto();

        Assert.Equal(user.Uuid, dto.Uuid);
        Assert.Equal(user.CreatedAt, dto.CreatedAt);
        Assert.Equal(user.UpdatedAt, dto.UpdatedAt);
        Assert.Equal("jane", dto.Username);
        Assert.Equal("Jane", dto.FirstName);
        Assert.Equal("Doe", dto.LastName);
        Assert.Equal("jane@example.test", dto.Email);
        Assert.Equal("operator", dto.Description);
        Assert.Equal(user.Groups, dto.Groups);
        Assert.True(dto.Enabled);
        Assert.False(dto.SystemUser);
    }

    [Fact]
    public void ListDto_SubstitutesAnEmptyGroupListForAMissingOne()
    {
        var user = StoredUser();
        user.Groups = null;

        Assert.Empty(user.ToDto().Groups);
    }

    [Fact]
    public void DetailDto_ReportsTheCertificateWhenAFingerprintIsStored()
    {
        var certificate = StoredUser().ToDetailDto().Certificate;

        Assert.NotNull(certificate);
        Assert.Equal(CertificateUuid, certificate.Uuid);
        Assert.Equal("abc123", certificate.Fingerprint);
    }

    [Fact]
    public void DetailDto_OmitsTheCertificateWhenNoFingerprintIsStored()
    {
        var user = StoredUser();
        user.CertificateFingerprint = null;

        Assert.Null(user.ToDetailDto().Certificate);
    }

    [Fact]
    public void DetailDto_ExpandsTheAssignedRoles()
    {
        var user = StoredUser();
        user.Roles = [new Role { Uuid = Guid.NewGuid(), Name = "admin" }];

        var roles = user.ToDetailDto().Roles;

        Assert.Equal("admin", Assert.Single(roles).Name);
    }

    [Fact]
    public void DetailDto_SubstitutesAnEmptyRoleListForAMissingOne()
    {
        var user = StoredUser();
        user.Roles = null!;

        Assert.Empty(user.ToDetailDto().Roles);
    }

    [Fact]
    public void EntityMapper_BuildsAUserFromACreateRequest()
    {
        var user = UserEntityMapper.Instance.ToEntity(new UserRequestDto { Username = "jane" });

        Assert.Equal("jane", user.Username);
    }

    [Fact]
    public void EntityMapper_RejectsACreateRequestOfTheWrongType()
    {
        var exception = Assert.Throws<ArgumentException>(() => UserEntityMapper.Instance.ToEntity(new RoleRequestDto { Name = "admin" }));

        Assert.Contains("RoleRequestDto", exception.Message);
    }

    [Fact]
    public void EntityMapper_AppliesAnUpdateRequestToALoadedUser()
    {
        var user = StoredUser();

        UserEntityMapper.Instance.ApplyUpdate(new UserUpdateRequestDto { FirstName = "Janet" }, user);

        Assert.Equal("Janet", user.FirstName);
    }

    [Fact]
    public void EntityMapper_RejectsAnUpdateRequestOfTheWrongType()
    {
        var exception = Assert.Throws<ArgumentException>(() => UserEntityMapper.Instance.ApplyUpdate(new UserRequestDto { Username = "jane" }, StoredUser()));

        Assert.Contains("UserRequestDto", exception.Message);
    }

    [Fact]
    public void EntityMapper_DelegatesBothResponseShapes()
    {
        var user = StoredUser();

        Assert.Equal(user.Uuid, UserEntityMapper.Instance.ToDto(user).Uuid);
        Assert.NotNull(UserEntityMapper.Instance.ToDetailDto(user).Certificate);
    }
}
