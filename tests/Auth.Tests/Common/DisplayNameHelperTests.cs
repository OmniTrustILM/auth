using Auth.Common.Helpers;

namespace Auth.Tests.Common;

public class DisplayNameHelperTests
{
    [Theory]
    [InlineData("certificate", "Certificate")]
    [InlineData("Certificate", "Certificate")]
    [InlineData("raProfile", "Ra Profile")]
    [InlineData("RaProfile", "Ra Profile")]
    [InlineData("listObjectsEndpoint", "List Objects Endpoint")]
    public void GetDisplayName_SplitsCamelCaseAndCapitalizesEachWord(string name, string expected)
    {
        Assert.Equal(expected, DisplayNameHelper.GetDisplayName(name));
    }

    [Theory]
    [InlineData("ACME", "ACME")]
    [InlineData("acmeAccount", "Acme Account")]
    [InlineData("ACMEAccount", "ACME Account")]
    public void GetDisplayName_KeepsRunsOfCapitalsAsOneWord(string name, string expected)
    {
        Assert.Equal(expected, DisplayNameHelper.GetDisplayName(name));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetDisplayName_ReturnsInputUnchanged_WhenNullOrEmpty(string? name)
    {
        Assert.Equal(name, DisplayNameHelper.GetDisplayName(name!));
    }

    [Theory]
    [InlineData("_", "")]
    [InlineData("123", "")]
    public void GetDisplayName_ReturnsEmpty_WhenNothingMatchesAWord(string name, string expected)
    {
        Assert.Equal(expected, DisplayNameHelper.GetDisplayName(name));
    }
}
