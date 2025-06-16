using Microsoft.Extensions.DependencyInjection;

namespace AutoLogger;

public class AutoLoggerSettings
{
    public Predicate<ServiceDescriptor>? Resolver { get; set; }
}
