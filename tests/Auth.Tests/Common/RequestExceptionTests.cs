using Auth.Common.Exceptions;
using System.Net;

namespace Auth.Tests.Common;

public class RequestExceptionTests
{
    // One case per subclass rather than a TheoryData of exception instances: an exception is not serializable, so a
    // theory built from instances cannot enumerate its rows individually in a test explorer (xUnit1045).
    private static void AssertMapping(RequestException exception, HttpStatusCode statusCode, string code)
    {
        Assert.Equal(statusCode, exception.StatusCode);
        Assert.Equal(code, exception.Code);
    }

    [Fact]
    public void EntityNotFound_IsReportedAsNotFound()
        => AssertMapping(new EntityNotFoundException("missing"), HttpStatusCode.NotFound, "ENTITY_NOT_FOUND");

    [Fact]
    public void EntityNotUnique_IsReportedAsABadRequest()
        => AssertMapping(new EntityNotUniqueException("taken"), HttpStatusCode.BadRequest, "ENTITY_NOT_UNIQUE");

    [Fact]
    public void InvalidAction_IsReportedAsABadRequest()
        => AssertMapping(new InvalidActionException("nope"), HttpStatusCode.BadRequest, "INVALID_ACTION");

    [Fact]
    public void InvalidFormat_IsReportedAsABadRequest()
        => AssertMapping(new InvalidFormatException("garbled"), HttpStatusCode.BadRequest, "INVALID_FORMAT");

    [Fact]
    public void Unauthorized_IsReportedAsUnauthorized()
        => AssertMapping(new UnauthorizedException("denied"), HttpStatusCode.Unauthorized, "UNAUTHORIZED");

    [Fact]
    public void Constructor_KeepsMessageAndInnerException()
    {
        var inner = new InvalidOperationException("root cause");
        var exception = new RequestException(HttpStatusCode.Conflict, "CONFLICT", "clash", inner);

        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
        Assert.Equal("CONFLICT", exception.Code);
        Assert.Equal("clash", exception.Message);
        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void Subclasses_DefaultToNoInnerException()
    {
        Assert.Null(new EntityNotFoundException("missing").InnerException);
    }

    [Fact]
    public void Subclasses_PassTheInnerExceptionThrough()
    {
        var inner = new FormatException("bad base64");

        Assert.Same(inner, new InvalidFormatException("wrapped", inner).InnerException);
    }
}
