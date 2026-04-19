using Esatto.Outreach.Infrastructure.Services;
using Esatto.Outreach.Infrastructure.Clients;
using Esatto.Outreach.Infrastructure.Options;
using Esatto.Outreach.Infrastructure.Services.OutreachGeneration;
using System.Text;
using Esatto.Outreach.Domain.Entities;


using Esatto.Outreach.Infrastructure.Services.Scraping;
using Esatto.Outreach.Infrastructure.Services.Enrichment;



using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.Abstractions.Clients;
using Esatto.Outreach.Infrastructure.Repositories;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Esatto.Outreach.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "Default")
    {
        var conn = configuration.GetConnectionString(connectionStringName)
                  ?? throw new InvalidOperationException($"Connection string '{connectionStringName}' missing");

        services.AddDbContext<OutreachDbContext>(opt =>
        {
            var provider = configuration.GetSection("Database")["Provider"] ?? "Postgres";
            if (provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
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

        // If secret is empty or placeholder, try to read from environment variable directly
        if (string.IsNullOrWhiteSpace(jwtOptions.Secret) || jwtOptions.Secret == "PLACEHOLDER_CHANGE_IN_USER_SECRETS")
        {
            var envSecret = Environment.GetEnvironmentVariable("Jwt__Secret");
            if (!string.IsNullOrWhiteSpace(envSecret))
            {
                jwtOptions.Secret = envSecret;
            }
            else
            {
                throw new InvalidOperationException("JWT:Secret must be set in user-secrets or environment");
            }
        }

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

        services.AddScoped<ISequenceRepository, SequenceRepository>();

        services.AddScoped<IStepExecutor, Esatto.Outreach.Infrastructure.Services.StepExecutors.EmailStepExecutor>();
        services.AddScoped<IStepExecutor, Esatto.Outreach.Infrastructure.Services.StepExecutors.LinkedInMessageExecutor>();
        services.AddScoped<IStepExecutor, Esatto.Outreach.Infrastructure.Services.StepExecutors.LinkedInConnectionExecutor>();
        services.AddScoped<IStepExecutor, Esatto.Outreach.Infrastructure.Services.StepExecutors.LinkedInInteractionExecutor>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IEntityIntelligenceRepository, EntityIntelligenceRepository>();
        services.AddScoped<IOutreachPromptRepository, OutreachPromptRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IInvitationRepository, InvitationRepository>();
        services.AddScoped<ICompanyInfoRepository, CompanyInfoRepository>();
        services.AddScoped<IProjectCaseRepository, ProjectCaseRepository>();
        services.AddScoped<IProspectRepository, ProspectRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        // OpenAI options (shared across features)
        services.Configure<OpenAiOptions>(configuration.GetSection(OpenAiOptions.SectionName));
        services.Configure<ClaudeOptions>(configuration.GetSection(ClaudeOptions.SectionName));

        // Outreach generation — cold outreach (one-off prospect emails/LinkedIn)
        services.Configure<OutreachGenerationOptions>(configuration.GetSection(OutreachGenerationOptions.SectionName));
        services.AddHttpClient<OpenAIColdOutreachGenerator>();
        services.AddHttpClient<CollectedDataColdOutreachGenerator>();
        services.AddScoped<IColdOutreachContextBuilder, ColdOutreachContextBuilder>();
        services.AddScoped<IColdOutreachGeneratorFactory, ColdOutreachGeneratorFactory>();

        // Outreach generation — focused sequence steps (multi-turn conversation thread)
        services.AddHttpClient<OpenAIFocusedSequenceStepGenerator>();
        services.AddScoped<IFocusedSequenceStepContextBuilder, FocusedSequenceStepContextBuilder>();
        services.AddScoped<IFocusedSequenceStepGenerator, OpenAIFocusedSequenceStepGenerator>();

        // Chat
        services.AddHttpClient<IOpenAIChatClient, OpenAIChatService>();

        // Generative AI (General purpose)
        services.AddHttpClient<IGenerativeAIClient, GenerativeAIClient>();

        // Soft Data Collection (multi-provider)

        // Scraping & Enrichment
        services.AddHttpClient<IWebScraperService, WebScraperService>();
        services.AddHttpClient<DuckDuckGoSerpService>();
        services.AddScoped<IContactDiscoveryProvider, HybridContactDiscoveryProvider>();
        services.AddScoped<ICompanyEnrichmentService, CompanyEnrichmentService>();
        services.AddScoped<ICompanyKnowledgeBaseService, CompanyKnowledgeBaseService>();

        return services;

    }
}

