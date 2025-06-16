using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;

namespace AutoLogger;

public static class AutoLoggerDecorationStrategyExtensions
{
    private const string ErrorMessage = """
        No service found for decoration.
        You can do the next:
        Use predicate for AddAutoLogger(type => bool) to allow any service;
        Use attribute [assembly: AutoLogger] to include all services from assembly;
        Use attribute AutoLogger for specific services;
        Call AddAutoLogger() after registration services.
        """;
    public static IServiceCollection AddAutoLogger(this IServiceCollection services, AutoLoggerSettings? settings = null)
    {
        bool? Check(Type? type) => type?.Assembly.GetCustomAttribute<AutoLoggerAttribute>()?.Allow;
        var resolver = settings?.Resolver ?? (sd => (Check(sd.GetInstanceType()) ?? Check(sd.ServiceType)) == true);
        try
        {
            services.Decorate(new AutoLoggerDecorationStrategy(resolver));
        }
        catch (DecorationException ex)
        {
            throw new ApplicationException(ErrorMessage, ex);
        }
        return services;
    }
}
