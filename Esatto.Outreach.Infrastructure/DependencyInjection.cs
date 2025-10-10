using Microsoft.Extensions.Http;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Esatto.Outreach.Infrastructure.Email;
using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        services.Configure<OpenAiOptions>(configuration.GetSection("OpenAI"));
        // OpenAI client factory + generator
        services.AddSingleton<IOpenAIResponseClientFactory, OpenAIResponseClientFactory>();
        services.AddScoped<ICustomEmailGenerator, OpenAICustomEmailGenerator>();
        return services;
    }
}
