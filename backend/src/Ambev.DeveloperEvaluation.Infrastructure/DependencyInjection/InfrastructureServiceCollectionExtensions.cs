using System.Text;
using Ambev.DeveloperEvaluation.Application.Abstractions;
using Ambev.DeveloperEvaluation.Application.Auth.Login;
using Ambev.DeveloperEvaluation.Application.Auth.Refresh;
using Ambev.DeveloperEvaluation.Application.Auth.Register;
using Ambev.DeveloperEvaluation.Domain.Sales;
using Ambev.DeveloperEvaluation.Infrastructure.Identity;
using Ambev.DeveloperEvaluation.Infrastructure.Persistence;
using Ambev.DeveloperEvaluation.Infrastructure.Time;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Wolverine;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

namespace Ambev.DeveloperEvaluation.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(opts =>
        {
            opts.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                o => o.EnableRetryOnFailure(3));
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddSingleton<IClock, SystemClock>();

        services.AddIdentityServices(configuration);
        services.AddJwtAuthentication(configuration);

        return services;
    }

    public static WolverineOptions ConfigureWolverine(
        this WolverineOptions opts,
        IConfiguration configuration)
    {
        var pg = configuration.GetConnectionString("DefaultConnection")!;
        opts.PersistMessagesWithPostgresql(pg, "wolverine");

        var rabbitMqConfig = configuration.GetSection("RabbitMq");
        opts.UseRabbitMq(rabbit =>
        {
            rabbit.HostName = rabbitMqConfig["Host"] ?? "localhost";
            rabbit.Port = int.Parse(rabbitMqConfig["Port"] ?? "5672");
            rabbit.UserName = rabbitMqConfig["Username"] ?? "guest";
            rabbit.Password = rabbitMqConfig["Password"] ?? "guest";
            rabbit.VirtualHost = rabbitMqConfig["VirtualHost"] ?? "/";
        }).AutoProvision();

        opts.PublishMessage<Ambev.DeveloperEvaluation.Application.IntegrationEvents.SaleCreatedIntegrationEvent>()
            .ToRabbitExchange("sales");
        opts.PublishMessage<Ambev.DeveloperEvaluation.Application.IntegrationEvents.SaleModifiedIntegrationEvent>()
            .ToRabbitExchange("sales");
        opts.PublishMessage<Ambev.DeveloperEvaluation.Application.IntegrationEvents.SaleCancelledIntegrationEvent>()
            .ToRabbitExchange("sales");
        opts.PublishMessage<Ambev.DeveloperEvaluation.Application.IntegrationEvents.SaleItemCancelledIntegrationEvent>()
            .ToRabbitExchange("sales");

        return opts;
    }

    private static void AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddIdentityCore<AppUser>(opts =>
        {
            opts.Password.RequiredLength = 8;
            opts.Password.RequireDigit = true;
            opts.Password.RequireUppercase = true;
            opts.Password.RequireNonAlphanumeric = true;
            opts.User.RequireUniqueEmail = true;
        })
        .AddRoles<AppRole>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        services.AddScoped<IIdentityService, JwtTokenService>();
        services.AddScoped<IRefreshTokenService, JwtTokenService>();
        services.AddScoped<IUserRegistrationService, JwtTokenService>();
    }

    private static void AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opts =>
            {
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSection["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSection["Audience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSection["SigningKey"]!)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                };
            });

        services.AddAuthorization();
    }
}
