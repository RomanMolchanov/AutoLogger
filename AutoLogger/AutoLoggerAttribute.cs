namespace AutoLogger;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct
    | AttributeTargets.Method | AttributeTargets.ReturnValue | AttributeTargets.Parameter)]
public class AutoLoggerAttribute(bool allow = true) : Attribute
{
    public bool Allow { get; } = allow;
}