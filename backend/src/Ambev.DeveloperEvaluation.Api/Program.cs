using Ambev.DeveloperEvaluation.Api.Endpoints.V1;
using Ambev.DeveloperEvaluation.Api.ExceptionHandling;
using Ambev.DeveloperEvaluation.Application.DependencyInjection;
using Ambev.DeveloperEvaluation.Infrastructure.DependencyInjection;
using Asp.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Serilog;
using System.Threading.RateLimiting;
using Wolverine;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

    // Infrastructure + Application
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // OpenAPI (native .NET 10)
    builder.Services.AddOpenApi();

    // API versioning
    builder.Services.AddApiVersioning(opts =>
    {
        opts.DefaultApiVersion = new ApiVersion(1);
        opts.AssumeDefaultVersionWhenUnspecified = true;
        opts.ReportApiVersions = true;
        opts.ApiVersionReader = new UrlSegmentApiVersionReader();
    });

    // Rate limiting (in-process fixed window; swap TokenBucketRateLimiter for Redis-backed in production)
    builder.Services.AddRateLimiter(opts =>
    {
        opts.AddFixedWindowLimiter("api", limiterOpts =>
        {
            limiterOpts.PermitLimit = builder.Configuration.GetValue("RateLimit:PermitLimit", 100);
            limiterOpts.Window = TimeSpan.FromSeconds(
                builder.Configuration.GetValue("RateLimit:WindowSeconds", 60));
            limiterOpts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiterOpts.QueueLimit = builder.Configuration.GetValue("RateLimit:QueueLimit", 10);
        });
        opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });

    // Output cache (in-memory; can be backed by Redis via custom IOutputCacheStore)
    builder.Services.AddOutputCache(opts =>
    {
        opts.AddPolicy("sales", b => b.Tag("sales").Expire(TimeSpan.FromSeconds(30)));
        opts.AddPolicy("salesList", b => b.Tag("salesList").Expire(TimeSpan.FromSeconds(10)));
    });

    // Health checks
    var connStr = builder.Configuration.GetConnectionString("DefaultConnection")!;
    var redisConnStr = builder.Configuration.GetConnectionString("Redis")!;
    builder.Services.AddHealthChecks()
        .AddNpgSql(connStr, name: "postgres")
        .AddRedis(redisConnStr, name: "redis");

    // CORS
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    builder.Services.AddCors(opts =>
        opts.AddDefaultPolicy(p =>
            p.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader()));

    // Exception handling (ProblemDetails)
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // OpenTelemetry
    var svcName = builder.Configuration["OpenTelemetry:ServiceName"] ?? "ambev-developer-evaluation";
    var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317";
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(r => r.AddService(svcName))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint)))
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint)));

    // Wolverine (messaging + outbox)
    builder.Host.UseWolverine(opts =>
        InfrastructureServiceCollectionExtensions.ConfigureWolverine(opts, builder.Configuration));

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseCors();
    app.UseRateLimiter();
    app.UseOutputCache();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseExceptionHandler();

    if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(opts =>
        {
            opts.Title = "Ambev Developer Evaluation API";
            opts.Theme = ScalarTheme.Purple;
        });
    }

    var v1 = app.NewVersionedApi()
        .MapGroup("/api/v{version:apiVersion}")
        .HasApiVersion(1);

    v1.MapSalesV1();
    v1.MapAuthV1();

    app.MapHealthChecks("/health");
    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false
    });
    app.MapHealthChecks("/health/ready");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    await Log.CloseAndFlushAsync();
}

#pragma warning disable S1118
public partial class Program { }
#pragma warning restore S1118
