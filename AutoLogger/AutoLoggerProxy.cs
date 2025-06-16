using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace AutoLogger;

internal class AutoLoggerProxy : DispatchProxy
{
    private object _inner = default!;
    private Func<MethodInfo, Type, ProxyInfo> _settingsProvider = default!;

    public DispatchProxy Init(object inner, Func<MethodInfo, Type, ProxyInfo> settingsProvider)
    {
        _inner = inner;
        _settingsProvider = settingsProvider;
        return this;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(targetMethod, nameof(targetMethod));
        object? InnerInvoke() => targetMethod.Invoke(_inner, args);
        var settings = _settingsProvider(targetMethod, _inner.GetType());
        if (!settings.Enabled)
            return InnerInvoke();
        ArgumentNullException.ThrowIfNull(settings.AllowParameters, nameof(settings.AllowParameters));

        settings.Logger.LogDebug("┌┐{Method}({Params})", targetMethod.Name, Stringify(targetMethod, args, settings.AllowParameters));
        var sw = Stopwatch.StartNew();
        try
        {
            var result = InnerInvoke();

            if (result is Task task && task.Status != TaskStatus.Created)
                TaskProcess(task, targetMethod, sw, settings);
            else
            {
                LogResult(targetMethod.Name, targetMethod.ReturnType, result, null, sw.Elapsed, settings, LogLevel.Debug);
            }

            return result;
        }
        catch (OperationCanceledException ex)
        {
            LogResult(targetMethod.Name, targetMethod.ReturnType, null, ex, sw.Elapsed, settings, LogLevel.Debug);
            throw;
        }
        catch (Exception ex)
        {
            LogResult(targetMethod.Name, targetMethod.ReturnType, null, ex, sw.Elapsed, settings, LogLevel.Error);
            throw;
        }
    }

    private static void TaskProcess(Task task, MethodInfo targetMethod, Stopwatch stopwatch, ProxyInfo settings)
    {
        var x =
        task.ContinueWith(x =>
        {
            var t = x.GetType();
            var (type, result) =
                IsTaskT(t)
                ? (t.GenericTypeArguments[0], t.GetProperty("Result")!.GetValue(x))
                : (typeof(void), null);
            LogResult(targetMethod.Name, type, result, x.Exception?.GetBaseException(), stopwatch.Elapsed, settings, x.Status == TaskStatus.Faulted ? LogLevel.Error : LogLevel.Debug);
        }, TaskContinuationOptions.ExecuteSynchronously);
    }

    private static void LogResult(string method, Type returnType, object? result, Exception? exception, TimeSpan elapsed, ProxyInfo settings, LogLevel logLevel)
    {
        settings.Logger.Log(logLevel,
            exception,
            "└┘{Method} -> {Result} ({Duration})",
            method,
            settings.LogResult ? returnType == typeof(void) ? "{void}" : Stringify(result) : "***",
            elapsed.ToString(
                elapsed switch
                {
                    { Days: > 0 } => @"dd\.hh\:mm",
                    { Hours: > 0 } => @"hh\:mm",
                    { Minutes: > 0 } => @"mm\:ss",
                    { Seconds: > 0 } => @"ss\.ff",
                    _ => @"\.ffff"
                }));
    }

    private static string Stringify(MethodInfo targetMethod, object?[]? args, bool[] allowParameters)
    {
        return args == null ? ""
            : string.Join(", ", targetMethod.GetParameters().Zip(args, allowParameters)
                .Select(x => !x.Third ? "***" : Stringify(x.Second)));
    }

    private static string Stringify(object? value)
    {
        return value?.ToString() ?? "{null}";
    }

    private static bool IsTaskT(Type type)
    {
        while (!type.IsPublic)
            type = type.BaseType!;
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>);
    }
}