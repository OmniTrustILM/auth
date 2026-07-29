using System.Net;

namespace Auth.Common.Exceptions
{
    public class ExceptionMiddleware
    {
        private readonly ILogger _logger;
        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _env;

        public ExceptionMiddleware(IWebHostEnvironment env, RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _env = env;
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                var unknownException = ex is not RequestException;
                if (unknownException) _logger.LogError($"Internal server error: {ex}");
                else _logger.LogError(ex.InnerException == null ? ex.Message : $"{ex.Message}:{ex.InnerException.Message}");

                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var errorDetail = _env.IsProduction() ? new ErrorDetails(exception) : new ErrorDetailsExtended(context.Request.Path, context.Request.Host.Host, exception);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = errorDetail.StatusCode;
            await context.Response.WriteAsync(errorDetail.ToString());
        }
    }
}
