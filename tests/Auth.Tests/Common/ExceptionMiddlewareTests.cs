using Auth.Common.Exceptions;
using Auth.Tests.TestSupport;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Auth.Tests.Common;

public class ExceptionMiddlewareTests
{
    private static DefaultHttpContext Context(string path = "/auth/users", string host = "auth-service")
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Host = new HostString(host);
        context.Response.Body = new MemoryStream();

        return context;
    }

    private static async Task<JsonDocument> Body(HttpContext context)
    {
        context.Response.Body.Position = 0;
        var payload = await new StreamReader(context.Response.Body).ReadToEndAsync();

        return JsonDocument.Parse(payload);
    }

    [Fact]
    public async Task Pipeline_IsLeftUntouchedWhenNothingThrows()
    {
        var logger = new RecordingLogger<ExceptionMiddleware>();
        var called = false;
        var middleware = new ExceptionMiddleware(FakeWebHostEnvironment.Production(), _ =>
        {
            called = true;
            return Task.CompletedTask;
        }, logger);
        var context = Context();

        await middleware.InvokeAsync(context);

        Assert.True(called);
        Assert.Equal(200, context.Response.StatusCode);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task RequestException_IsTranslatedToItsOwnStatusCodeAndCode()
    {
        var middleware = new ExceptionMiddleware(FakeWebHostEnvironment.Production(),
            _ => throw new EntityNotFoundException("role not found"),
            new RecordingLogger<ExceptionMiddleware>());
        var context = Context();

        await middleware.InvokeAsync(context);

        Assert.Equal(404, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);

        using var body = await Body(context);
        Assert.Equal("ENTITY_NOT_FOUND", body.RootElement.GetProperty("code").GetString());
        Assert.Equal("role not found", body.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task UnknownException_IsTranslatedToAnOpaqueFiveHundred()
    {
        var middleware = new ExceptionMiddleware(FakeWebHostEnvironment.Production(),
            _ => throw new InvalidOperationException("connection refused"),
            new RecordingLogger<ExceptionMiddleware>());
        var context = Context();

        await middleware.InvokeAsync(context);

        Assert.Equal(500, context.Response.StatusCode);

        using var body = await Body(context);
        Assert.Equal("APPLICATION_ERROR", body.RootElement.GetProperty("code").GetString());
        Assert.Equal("Application Error", body.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task ProductionBody_WithholdsTheDiagnosticFields()
    {
        var middleware = new ExceptionMiddleware(FakeWebHostEnvironment.Production(),
            _ => throw new UnauthorizedException("denied"),
            new RecordingLogger<ExceptionMiddleware>());
        var context = Context();

        await middleware.InvokeAsync(context);

        using var body = await Body(context);
        Assert.False(body.RootElement.TryGetProperty("url", out _));
        Assert.False(body.RootElement.TryGetProperty("exception", out _));
    }

    [Fact]
    public async Task DevelopmentBody_AddsTheRequestUrlHostAndStackFrames()
    {
        var middleware = new ExceptionMiddleware(FakeWebHostEnvironment.Development(),
            _ => throw new UnauthorizedException("denied"),
            new RecordingLogger<ExceptionMiddleware>());
        var context = Context("/auth/users/identify", "auth.example");

        await middleware.InvokeAsync(context);

        Assert.Equal(401, context.Response.StatusCode);

        using var body = await Body(context);
        Assert.Equal("/auth/users/identify", body.RootElement.GetProperty("url").GetString());
        Assert.Equal("auth.example", body.RootElement.GetProperty("service").GetString());
        Assert.True(body.RootElement.GetProperty("exception").GetArrayLength() > 0);
    }

    [Fact]
    public async Task UnknownException_IsLoggedWithItsFullDetail()
    {
        var logger = new RecordingLogger<ExceptionMiddleware>();
        var middleware = new ExceptionMiddleware(FakeWebHostEnvironment.Production(),
            _ => throw new InvalidOperationException("connection refused"), logger);

        await middleware.InvokeAsync(Context());

        Assert.True(logger.Logged(LogLevel.Error, "Internal server error"));
        Assert.True(logger.Logged(LogLevel.Error, "connection refused"));
    }

    [Fact]
    public async Task RequestException_IsLoggedAsItsMessageAlone()
    {
        var logger = new RecordingLogger<ExceptionMiddleware>();
        var middleware = new ExceptionMiddleware(FakeWebHostEnvironment.Production(),
            _ => throw new EntityNotFoundException("role not found"), logger);

        await middleware.InvokeAsync(Context());

        var entry = Assert.Single(logger.Entries);
        Assert.Equal("role not found", entry.Message);
    }

    [Fact]
    public async Task RequestException_IsLoggedWithItsInnerMessageAppended()
    {
        var logger = new RecordingLogger<ExceptionMiddleware>();
        var middleware = new ExceptionMiddleware(FakeWebHostEnvironment.Production(),
            _ => throw new UnauthorizedException("certificate invalid", new Exception("untrusted root")), logger);

        await middleware.InvokeAsync(Context());

        var entry = Assert.Single(logger.Entries);
        Assert.Equal("certificate invalid:untrusted root", entry.Message);
    }
}
