using Auth.Models.Config;

namespace Auth.Tests.Config;

public class AuthOptionsTests
{
    [Fact]
    public void Defaults_CreateNothingAndDoNotSync()
    {
        var options = new AuthOptions();

        Assert.False(options.CreateUnknownUsers);
        Assert.False(options.CreateUnknownRoles);
        Assert.Equal(SyncPolicy.CreateOnly, options.SyncPolicy);
    }

    [Theory]
    [InlineData("sync-data")]
    [InlineData("SYNC-DATA")]
    [InlineData("Sync-Data")]
    public void GetSyncPolicy_RecognizesSyncDataRegardlessOfCase(string value)
    {
        Assert.Equal(SyncPolicy.SyncData, AuthOptions.GetSyncPolicy(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("create-only")]
    [InlineData("syncdata")]
    [InlineData(" sync-data ")]
    public void GetSyncPolicy_FallsBackToCreateOnly(string? value)
    {
        Assert.Equal(SyncPolicy.CreateOnly, AuthOptions.GetSyncPolicy(value));
    }
}
