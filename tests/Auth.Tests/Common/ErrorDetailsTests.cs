using Auth.Common.Exceptions;
using System.Text.Json;

namespace Auth.Tests.Common;

public class ErrorDetailsTests
{
    [Fact]
    public void RequestException_IsReportedWithItsOwnStatusCodeAndCode()
    {
        var details = new ErrorDetails(new EntityNotFoundException("user not found"));

        Assert.Equal(404, details.StatusCode);
        Assert.Equal("ENTITY_NOT_FOUND", details.Code);
        Assert.Equal("user not found", details.Message);
    }

    [Fact]
    public void UnknownException_IsReportedAsAnOpaqueApplicationError()
    {
        var details = new ErrorDetails(new InvalidOperationException("connection string missing"));

        Assert.Equal(500, details.StatusCode);
        Assert.Equal("APPLICATION_ERROR", details.Code);
        Assert.Equal("Application Error", details.Message);
    }

    [Fact]
    public void ToString_SerializesTheThreeReportedFields()
    {
        var json = new ErrorDetails(new UnauthorizedException("denied")).ToString();

        using var document = JsonDocument.Parse(json);
        Assert.Equal(401, document.RootElement.GetProperty("statusCode").GetInt32());
        Assert.Equal("UNAUTHORIZED", document.RootElement.GetProperty("code").GetString());
        Assert.Equal("denied", document.RootElement.GetProperty("message").GetString());
    }
}
