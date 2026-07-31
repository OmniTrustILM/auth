using Auth.Common.Models.Dto;
using Auth.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Auth.Tests.Data;

/// <summary>
/// A user's groups are stored as one pipe-delimited <c>uuid:name</c> column, so the conversion has to survive a
/// round trip through the database rather than merely compile.
/// </summary>
public class UserGroupsConversionTests : SqliteTestBase
{
    private static NameAndUuidDto Group(string name) => new() { Uuid = Guid.NewGuid(), Name = name };

    private async Task<User> RoundTrip(List<NameAndUuidDto> groups)
    {
        Guid uuid = default;
        await Seed(context =>
        {
            var user = new User { Username = "jane", Groups = groups };
            context.Users.Add(user);
            uuid = user.Uuid;

            return Task.CompletedTask;
        });

        await using var reader = NewContext();
        return await reader.Users.SingleAsync(u => u.Uuid == uuid);
    }

    [Fact]
    public async Task OneGroupSurvivesTheRoundTrip()
    {
        var group = Group("operators");

        var stored = await RoundTrip([group]);

        var read = Assert.Single(stored.Groups!);
        Assert.Equal(group.Uuid, read.Uuid);
        Assert.Equal("operators", read.Name);
    }

    [Fact]
    public async Task SeveralGroupsSurviveTheRoundTripInOrder()
    {
        var first = Group("operators");
        var second = Group("auditors");

        var stored = await RoundTrip([first, second]);

        Assert.Equal([first.Uuid, second.Uuid], stored.Groups!.Select(g => g.Uuid));
        Assert.Equal(["operators", "auditors"], stored.Groups!.Select(g => g.Name));
    }

    [Fact]
    public async Task AnEmptyGroupListIsStoredAsNothingAndComesBackAsNoListAtAll()
    {
        // An empty list converts to a null column value, and a null column value bypasses the conversion on the way
        // back, so the empty list the write started from is not what the read returns.
        var stored = await RoundTrip([]);

        Assert.Null(stored.Groups);
    }

    [Fact]
    public async Task AUserWhoseGroupsCameBackAsNoList_CanStillBeSavedAgain()
    {
        // The conversion is skipped for a null value in both directions, so the null-unsafe list access in the
        // converter is never reached and a later edit of the same row succeeds.
        Guid uuid = default;
        await Seed(context =>
        {
            var user = new User { Username = "jane", Groups = [] };
            context.Users.Add(user);
            uuid = user.Uuid;

            return Task.CompletedTask;
        });

        await using (var context = NewContext())
        {
            var user = await context.Users.SingleAsync(u => u.Uuid == uuid);
            user.Description = "any other edit";
            await context.SaveChangesAsync();
        }

        await using var reader = NewContext();
        var stored = await reader.Users.SingleAsync(u => u.Uuid == uuid);
        Assert.Equal("any other edit", stored.Description);
        Assert.Null(stored.Groups);
    }

    [Fact]
    public async Task AGroupNameContainingTheKeySeparatorSurvivesIntact()
    {
        var group = Group("tenant:operators");

        var stored = await RoundTrip([group]);

        var read = Assert.Single(stored.Groups!);
        Assert.Equal(group.Uuid, read.Uuid);
        Assert.Equal("tenant:operators", read.Name);
    }

    [Fact]
    public async Task ReplacingTheGroupListIsDetectedAsAChange()
    {
        Guid uuid = default;
        await Seed(context =>
        {
            var user = new User { Username = "jane", Groups = [Group("operators")] };
            context.Users.Add(user);
            uuid = user.Uuid;

            return Task.CompletedTask;
        });

        await using (var context = NewContext())
        {
            var user = await context.Users.SingleAsync(u => u.Uuid == uuid);
            user.Groups = [Group("auditors")];
            await context.SaveChangesAsync();
        }

        await using var reader = NewContext();
        var stored = await reader.Users.SingleAsync(u => u.Uuid == uuid);
        Assert.Equal("auditors", Assert.Single(stored.Groups!).Name);
    }
}
