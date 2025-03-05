using Czertainly.Auth.Common.Exceptions;
using Czertainly.Auth.Common.Filters;
using Czertainly.Auth.Data;
using Czertainly.Auth.Data.Contracts;
using Czertainly.Auth.Data.Repositiories;
using Czertainly.Auth.Models.Config;
using Czertainly.Auth.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using NLog;
using NLog.Web;
using System.Net.Mime;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("init main");

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Configuration.AddEnvironmentVariables();

    // NLog: Setup NLog for Dependency injection
    builder.Logging.ClearProviders();
    builder.Host.UseNLog(
        new NLogAspNetCoreOptions
        {
            RemoveLoggerFactoryFilter = false,
        }
    );

    // Add services to the container.
    builder.Services.AddAutoMapper(cfg =>
    {
        cfg.AllowNullCollections = true;
    }, typeof(Program));

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.AllowTrailingCommas = true;
        })
        .ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var result = new ValidationFailedResult(context.ModelState);
                result.ContentTypes.Add(MediaTypeNames.Application.Json);
                return result;
            };
        });

    builder.Services.AddHealthChecks();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(swagger =>
    {
        swagger.SwaggerDoc("v1", new OpenApiInfo { Title = "CZERTAINLY Auth Service", Version = "v1" });
    });

    builder.Services.AddApiVersioning(o =>
    {
        o.AssumeDefaultVersionWhenUnspecified = true;
        o.DefaultApiVersion = new ApiVersion(1, 0);
        o.ReportApiVersions = true;
    });

    builder.Services.AddDbContext<AuthDbContext>(opts =>
    {
        opts.UseNpgsql(builder.Configuration.GetValue<string>("AUTH_DB_CONNECTION_STRING"), pgsqlOpts =>
        {
            pgsqlOpts.MigrationsHistoryTable("_migrations_history", "auth");
        });
    });

    // add configurations
    builder.Services.Configure<AuthOptions>(authOptions =>
    {
        authOptions.CreateUnknownUsers = builder.Configuration.GetValue<bool>("AUTH_CREATE_UNKNOWN_USERS");
        authOptions.CreateUnknownRoles = builder.Configuration.GetValue<bool>("AUTH_CREATE_UNKNOWN_ROLES");
        authOptions.SyncPolicy = AuthOptions.GetSyncPolicy(builder.Configuration.GetValue<string>("SYNC_POLICY"));
    });

    // add app services
    builder.Services.AddScoped<ValidationFilter>();
    builder.Services.AddScoped<IRepositoryManager, RepositoryManager>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IRoleService, RoleService>();
    builder.Services.AddScoped<IPermissionService, PermissionService>();
    builder.Services.AddScoped<IResourceService, ResourceService>();
    builder.Services.AddScoped<IActionService, ActionService>();

    var app = builder.Build();
    app.MapHealthChecks("/health");

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // run migrations
    // TODO: handle migrations differently in production, not run during deployment
    using (var scope = app.Services.CreateScope())
    {
        var dataContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        dataContext.Database.Migrate();
    }

    app.UseMiddleware<ExceptionMiddleware>();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception exception)
{
    // NLog: catch setup errors
    logger.Error(exception, "Stopped program because of exception");
    throw;
}
finally
{
    // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
    LogManager.Shutdown();
}