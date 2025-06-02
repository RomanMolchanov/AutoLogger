using Microsoft.Extensions.DependencyInjection;

namespace AutoLogger;

internal static class ServiceDescriptorExtensions
{
    public static Type? GetInstanceType(this ServiceDescriptor descriptor) =>
            descriptor.IsKeyedService
            ? (descriptor.KeyedImplementationType ?? (descriptor.KeyedImplementationInstance?.GetType()))
            : (descriptor.ImplementationType ?? (descriptor.ImplementationInstance?.GetType()));
}
