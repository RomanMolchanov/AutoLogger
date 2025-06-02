using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Scrutor;

namespace AutoLogger;

internal class AutoLoggerDecorationStrategy(Predicate<ServiceDescriptor> resolver) : DecorationStrategy(typeof(AutoLoggerDecorationStrategy), null)
{
    private ILogger<AutoLoggerDecorationStrategy>? _logger;
    private readonly ConcurrentDictionary<(MethodInfo ServiceMethod, Type ObjectType), Settings> _accessCache = new();
    private readonly ConcurrentDictionary<Type, bool> _typeEnableCache = new ConcurrentDictionary<Type, bool>();

    public override Func<IServiceProvider, object?, object> CreateDecorator(Type serviceType, string serviceKey)
    {
        return (serviceProvider, _) =>
        {
            var inner = serviceProvider.GetRequiredKeyedService(serviceType, serviceKey);
            var innerType = inner.GetType();
            if (!_typeEnableCache.GetOrAdd(innerType,
                type => GetAccess(GetMemberAccess, type, serviceType) != false))
                return inner;
            try
            {
                var proxy = (AutoLoggerProxy)DispatchProxy.Create(serviceType, typeof(AutoLoggerProxy));
                return proxy.Init(inner, GetSettingsProvider(serviceProvider));
            }
            catch (Exception ex)
            {
                _logger ??= serviceProvider.GetRequiredService<ILogger<AutoLoggerDecorationStrategy>>();
                _logger.LogError(ex, "{Type} cannot be decorated", serviceType.FullName);
                _typeEnableCache.TryUpdate(innerType, false, true);
                return inner;
            }
        };
    }

    protected override bool CanDecorate(Type serviceType) => throw new NotImplementedException();

    public override bool CanDecorate(ServiceDescriptor descriptor) => descriptor.ServiceType.IsInterface
        && (GetAccess(GetMemberAccess, descriptor.GetInstanceType(), descriptor.ServiceType) ?? resolver(descriptor));

    private bool? GetAccess<T>(Func<T, bool?> func, params T[] memberInfo) =>
        memberInfo.Select(func).FirstOrDefault(x => x.HasValue);

    private bool? GetMemberAccess(MemberInfo? memberInfo) => memberInfo?.GetCustomAttribute<AutoLoggerAttribute>()?.Allow;

    private bool? GetPropertyAccess(ParameterInfo parameterInfo) => parameterInfo.GetCustomAttribute<AutoLoggerAttribute>()?.Allow;

    private Func<MethodInfo, Type, Settings> GetSettingsProvider(IServiceProvider serviceProvider)
    {
        return (serviceMethod, impl) => _accessCache.GetOrAdd((serviceMethod, impl), key =>
        {
            var objectMethod = key.ObjectType.GetMethod(serviceMethod.Name, serviceMethod.GetParameters().Select(x => x.ParameterType).ToArray())!;
            var access = GetAccess(GetMemberAccess, objectMethod, key.ServiceMethod);
            var settings = new Settings { Enabled = access != false };
            if (settings.Enabled)
            {
                access = GetAccess(GetPropertyAccess, objectMethod.ReturnParameter, key.ServiceMethod.ReturnParameter);
                settings.LogResult = access != false;

                settings.AllowParameters = objectMethod.GetParameters()
                    .Zip(key.ServiceMethod.GetParameters(), (x, y) => GetAccess(GetPropertyAccess, x, y) != false)
                    .ToArray();
                settings.Logger = (ILogger)serviceProvider.GetRequiredService(typeof(ILogger<>).MakeGenericType(impl));
            }
            return settings;
        });
    }
}
