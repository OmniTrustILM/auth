using Auth.Common.Exceptions;
using System.Text.Json;

namespace Auth.Tests.Common;

public class ErrorDetailsExtendedTests
{
    private static Exception Thrown(string message, Exception? inner = null)
    {
        try
        {
            throw new InvalidActionException(message, inner);
        }
        catch (Exception caught)
        {
            return caught;
        }
    }

    [Fact]
    public void Details_KeepTheRequestUrlAndServiceAlongsideTheBaseFields()
    {
        var details = new ErrorDetailsExtended("/auth/users", "auth-service", Thrown("bad request"));

        Assert.Equal("/auth/users", details.Url);
        Assert.Equal("auth-service", details.Service);
        Assert.Equal(400, details.StatusCode);
        Assert.Equal("INVALID_ACTION", details.Code);
    }

    [Fact]
    public void Exception_LeadsWithTheMessageAndThenTheStackFrames()
    {
        var details = new ErrorDetailsExtended("/auth", "auth", Thrown("bad request"));

        Assert.Equal("bad request", details.Exception[0]);
        Assert.True(details.Exception.Length > 1);
    }

    [Fact]
    public void InnerException_IsAbsentWhenThereIsNone()
    {
        Assert.Null(new ErrorDetailsExtended("/auth", "auth", Thrown("standalone")).InnerException);
    }

    [Fact]
    public void InnerException_LeadsWithTheInnerMessage()
    {
        var details = new ErrorDetailsExtended("/auth", "auth", Thrown("outer", new FormatException("inner cause")));

        Assert.NotNull(details.InnerException);
        Assert.Equal("inner cause", details.InnerException[0]);
    }

    [Fact]
    public void ToString_SerializesTheExtendedFieldsTogetherWithTheBaseFields()
    {
        var json = new ErrorDetailsExtended("/auth/roles", "auth", Thrown("bad request")).ToString();

        using var document = JsonDocument.Parse(json);
        Assert.Equal("/auth/roles", document.RootElement.GetProperty("url").GetString());
        Assert.Equal("auth", document.RootElement.GetProperty("service").GetString());
        Assert.Equal(400, document.RootElement.GetProperty("statusCode").GetInt32());
        Assert.True(document.RootElement.GetProperty("exception").GetArrayLength() > 0);
    }

    [Fact]
    public void Details_HandleAnExceptionThatWasNeverThrown()
    {
        var details = new ErrorDetailsExtended("/auth", "auth", new UnauthorizedException("never thrown"));

        Assert.Equal(["never thrown"], details.Exception);
    }
}
