using Microsoft.Extensions.Logging;

namespace AutoLogger;

internal class Settings
{
    public bool Enabled { get; set; }
    public bool LogResult { get; set; }
    public bool[]? AllowParameters { get; set; }
    public ILogger Logger { get; set; } = default!;
}