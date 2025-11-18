using Microsoft.Extensions.Http;
using System.Net.Http;
using Esatto.Outreach.Infrastructure.Common;
using Esatto.Outreach.Infrastructure.SoftDataCollection;
using Esatto.Outreach.Infrastructure.EmailGeneration;
using Esatto.Outreach.Infrastructure.EmailDelivery;
using Esatto.Outreach.Infrastructure.Chat;
using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

        services.AddScoped<IProspectRepository, ProspectRepository>();
        services.AddScoped<IHardCompanyDataRepository, HardCompanyDataRepository>();
        services.AddScoped<ISoftCompanyDataRepository, SoftCompanyDataRepository>();
        services.AddScoped<IGenerateEmailPromptRepository, GenerateEmailPromptRepository>();

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

