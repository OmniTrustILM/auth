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
    public void InnerException_CarriesTheInnerStackFramesRatherThanLeakingThemIntoTheOuterList()
    {
        Exception inner;
        try
        {
            throw new FormatException("inner cause");
        }
        catch (Exception caught)
        {
            inner = caught;
        }

        var details = new ErrorDetailsExtended("/auth", "auth", Thrown("outer", inner));

        Assert.NotNull(details.InnerException);
        Assert.True(details.InnerException.Length > 1);
        Assert.DoesNotContain(details.Exception, frame => frame.Contains("inner cause", StringComparison.Ordinal));
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

    [Theory]
    [InlineData("   at First()\n   at Second()\n   at Third()")]
    [InlineData("   at First()\r\n   at Second()\r\n   at Third()")]
    public void Frames_AreSplitWhicheverLineEndingTheTraceCarries(string stackTrace)
    {
        // The service runs on Linux, so a trace separated by bare newlines is the production case; a Windows
        // development run produces the other.
        var details = new ErrorDetailsExtended("/auth", "auth", new TracedException("boom", stackTrace));

        Assert.Equal(["boom", "First()", "Second()", "Third()"], details.Exception);
    }

    [Fact]
    public void Frames_OfTheInnerExceptionStayOutOfTheOuterList()
    {
        var inner = new TracedException("inner cause", "   at InnerFrame()");
        var details = new ErrorDetailsExtended("/auth", "auth", new TracedException("outer", "   at OuterFrame()", inner));

        Assert.Equal(["outer", "OuterFrame()"], details.Exception);
        Assert.NotNull(details.InnerException);
        Assert.Equal(["inner cause", "InnerFrame()"], details.InnerException);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    public void AStackTraceTooShortToCarryAFramePrefix_IsNotARangeError(string stackTrace)
    {
        var details = new ErrorDetailsExtended("/auth", "auth", new TracedException("boom", stackTrace));

        Assert.Equal("boom", details.Exception[0]);
    }

    private sealed class TracedException(string message, string stackTrace, Exception? innerException = null)
        : Exception(message, innerException)
    {
        public override string? StackTrace { get; } = stackTrace;
    }
}
