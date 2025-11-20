using Microsoft.Extensions.Http;
using System.Net.Http;
using System.Text;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Infrastructure.Common;
using Esatto.Outreach.Infrastructure.SoftDataCollection;
using Esatto.Outreach.Infrastructure.EmailGeneration;
using Esatto.Outreach.Infrastructure.EmailDelivery;
using Esatto.Outreach.Infrastructure.Chat;
using Esatto.Outreach.Infrastructure.Auth;
using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Esatto.Outreach.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "Default")
    {
        var provider = configuration.GetSection("Database")["Provider"] ?? "Sqlite";
        var conn = configuration.GetConnectionString(connectionStringName)
                  ?? throw new InvalidOperationException($"Connection string '{connectionStringName}' missing");

        services.AddDbContext<OutreachDbContext>(opt =>
        {
            if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
                opt.UseSqlite(conn);
            else if (provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
                opt.UseNpgsql(conn);
            else
                throw new InvalidOperationException($"Unknown DB provider: {provider}");
        });

        // ============ IDENTITY SETUP ============
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            // Password requirements
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            
            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            
            // User settings
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = false; // Change to true when email service is added
        })
        .AddEntityFrameworkStores<OutreachDbContext>()
        .AddDefaultTokenProviders();

        // ============ JWT AUTHENTICATION ============
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT configuration missing");

        if (string.IsNullOrWhiteSpace(jwtOptions.Secret) || jwtOptions.Secret == "PLACEHOLDER_CHANGE_IN_USER_SECRETS")
            throw new InvalidOperationException("JWT:Secret must be set in user-secrets or environment");

        var key = Encoding.UTF8.GetBytes(jwtOptions.Secret);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false; // Set true in production
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero // No grace period
            };
        });

        services.AddAuthorization();
        // ============================================

        // JWT Token Service
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        services.AddScoped<IProspectRepository, ProspectRepository>();
        services.AddScoped<IHardCompanyDataRepository, HardCompanyDataRepository>();
        services.AddScoped<ISoftCompanyDataRepository, SoftCompanyDataRepository>();
        services.AddScoped<IGenerateEmailPromptRepository, GenerateEmailPromptRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        // OpenAI options (shared across features)
        services.Configure<OpenAiOptions>(configuration.GetSection(OpenAiOptions.SectionName));
        services.Configure<ClaudeOptions>(configuration.GetSection(ClaudeOptions.SectionName));

        // Email Generation
        services.AddHttpClient<ICustomEmailGenerator, OpenAICustomEmailGenerator>();

        // Email Delivery (N8n)
        services.Configure<N8nOptions>(configuration.GetSection(N8nOptions.SectionName));
        services.AddHttpClient<IN8nEmailService, N8nEmailService>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<N8nOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        // Chat
        services.AddHttpClient<IOpenAIChatClient, OpenAIChatService>();

        // Soft Data Collection (multi-provider)
        services.Configure<SoftDataCollectionOptions>(configuration.GetSection(SoftDataCollectionOptions.SectionName));
        services.AddHttpClient<OpenAIResearchService>();
        services.AddHttpClient<ClaudeResearchService>();
        services.AddScoped<HybridResearchService>();
        services.AddScoped<IResearchServiceFactory, ResearchServiceFactory>();
        
        return services;
    }
}

