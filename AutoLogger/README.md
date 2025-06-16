using AutoLogger;

//[assembly: AutoLogger]

var builder = WebApplication.CreateBuilder(args);
builder.Host
    .UseSerilogLogging();
builder.Services
    .AddTransient<ITestService, TestService>()
    .AddAutoLogger();
var app = builder.Build();

// Example
var service = app.Services.GetRequiredService<ITestService>();

// Simple test
service.Test();

// Log input and output parameters
service.Params("value1", "value2", new { a = 1 });

// Hide secret or long parameters
service.Secret("password");

// Log Tasks
await service.TaskVoid();
// Log Task with result
await service.TaskResult();
// Log Task with exc
try { await service.TaskException(); } catch { }

// Log properties
service.Prop = "prop";
_ = service.Prop;
service.NoLogProp = "noLogProp";
_ = service.NoLogProp;
service.HiddenProp = "hiddenProp";
_ = service.HiddenProp;

await Task.Delay(1000);

// Log events
EventHandler eh = (e, a) => { };
service.Event += eh;
service.Event -= eh;
service.NoLogEvent += eh;
service.NoLogEvent -= eh;

public interface ITestService
{
    public event EventHandler? Event;
    public event EventHandler? NoLogEvent;
    string? Prop { get; set; }
    string? NoLogProp { get; set; }
    string? HiddenProp { get; set; }
    void Test();
    string Params(string p1, string p2, object o);
    string Secret(string id);
    Task TaskVoid();
    Task<string> TaskResult();
    Task TaskException();
}

[AutoLogger]
public class TestService : ITestService
{
    public event EventHandler? Event;
    [method: AutoLogger(false)] public event EventHandler? NoLogEvent;
    public string? Prop { get; set; }
    public string? NoLogProp { [AutoLogger(false)] get; [AutoLogger(false)] set; }
    public string? HiddenProp { [return: AutoLogger(false)] get; [return: AutoLogger(false)][param: AutoLogger(false)] set; }
    public void Test() { }
    public string Params(string p1, string p2, object o) => "Hi from TestService";
    [return: AutoLogger(false)] public string Secret([AutoLogger(false)] string id) => "wrapped " + id;
    public async Task TaskVoid() => await Task.Yield();
    public async Task<string> TaskResult()
    {
        await Task.Yield();
        return "result";
    }
    public Task TaskException() => throw new Exception("some exception");
}
