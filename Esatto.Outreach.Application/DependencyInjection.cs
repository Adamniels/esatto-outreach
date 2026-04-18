using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Esatto.Outreach.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var applicationAssembly = typeof(DependencyInjection).Assembly;
        var handlerTypes = applicationAssembly
            .GetTypes()
            .Where(type =>
                type is { IsClass: true, IsAbstract: false } &&
                (type.Name.EndsWith("CommandHandler", StringComparison.Ordinal) ||
                 type.Name.EndsWith("QueryHandler", StringComparison.Ordinal)));

        foreach (var handlerType in handlerTypes)
        {
            services.AddScoped(handlerType);
        }

        return services;
    }
}
