using Auth.Common.Exceptions;
using System.Net;

namespace Auth.Tests.Common;

public class RequestExceptionTests
{
    public static TheoryData<RequestException, HttpStatusCode, string> Subclasses => new()
    {
        { new EntityNotFoundException("missing"), HttpStatusCode.NotFound, "ENTITY_NOT_FOUND" },
        { new EntityNotUniqueException("taken"), HttpStatusCode.BadRequest, "ENTITY_NOT_UNIQUE" },
        { new InvalidActionException("nope"), HttpStatusCode.BadRequest, "INVALID_ACTION" },
        { new InvalidFormatException("garbled"), HttpStatusCode.BadRequest, "INVALID_FORMAT" },
        { new UnauthorizedException("denied"), HttpStatusCode.Unauthorized, "UNAUTHORIZED" },
    };

    [Theory]
    [MemberData(nameof(Subclasses))]
    public void EachSubclass_CarriesItsOwnStatusCodeAndCode(RequestException exception, HttpStatusCode statusCode, string code)
    {
        Assert.Equal(statusCode, exception.StatusCode);
        Assert.Equal(code, exception.Code);
    }

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
