using Ambev.DeveloperEvaluation.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;

namespace Ambev.DeveloperEvaluation.Api.IntegrationTests.Infrastructure;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly Dictionary<string, string?> _previousEnvironmentVariables = new(StringComparer.Ordinal);

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("ambev_integration")
        .WithUsername("test")
        .WithPassword("test_pass_123")
        .Build();

    private readonly RabbitMqContainer _rabbit = new RabbitMqBuilder("rabbitmq:4-management-alpine")
        .WithUsername("guest")
        .WithPassword("guest")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine").Build();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _postgres.StartAsync(),
            _rabbit.StartAsync(),
            _redis.StartAsync());

        ApplyEnvironmentOverrides();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public new async Task DisposeAsync()
    {
        try
        {
            await base.DisposeAsync();
        }
        finally
        {
            RestoreEnvironmentOverrides();

            await Task.WhenAll(
                _postgres.DisposeAsync().AsTask(),
                _rabbit.DisposeAsync().AsTask(),
                _redis.DisposeAsync().AsTask());
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }

    private void ApplyEnvironmentOverrides()
    {
        SetEnvironmentOverride("ASPNETCORE_ENVIRONMENT", "Testing");
        SetEnvironmentOverride("ConnectionStrings__DefaultConnection", _postgres.GetConnectionString());
        SetEnvironmentOverride("ConnectionStrings__Redis", _redis.GetConnectionString());
        SetEnvironmentOverride("RabbitMq__Host", _rabbit.Hostname);
        SetEnvironmentOverride("RabbitMq__Port", _rabbit.GetMappedPublicPort(5672).ToString());
        SetEnvironmentOverride("RabbitMq__Username", "guest");
        SetEnvironmentOverride("RabbitMq__Password", "guest");
        SetEnvironmentOverride("RabbitMq__VirtualHost", "/");
        SetEnvironmentOverride("Jwt__Issuer", "integration-test");
        SetEnvironmentOverride("Jwt__Audience", "integration-test");
        SetEnvironmentOverride("Jwt__SigningKey", "integration-test-signing-key-must-be-32-chars+");
        SetEnvironmentOverride("Jwt__AccessTokenExpirationMinutes", "60");
        SetEnvironmentOverride("Jwt__RefreshTokenExpirationDays", "7");
        SetEnvironmentOverride("Cors__AllowedOrigins__0", "http://localhost:3000");
        SetEnvironmentOverride("OpenTelemetry__OtlpEndpoint", "http://localhost:4317");
    }

    private void SetEnvironmentOverride(string key, string value)
    {
        _previousEnvironmentVariables.TryAdd(key, Environment.GetEnvironmentVariable(key));
        Environment.SetEnvironmentVariable(key, value);
    }

    private void RestoreEnvironmentOverrides()
    {
        foreach (var (key, value) in _previousEnvironmentVariables)
            Environment.SetEnvironmentVariable(key, value);

        _previousEnvironmentVariables.Clear();
    }
}
