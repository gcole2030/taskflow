using DbUp.Engine.Output;
using Serilog;

namespace Api.Common;

public sealed class DbUpSerilogLogger : IUpgradeLog
{
    public void LogDebug(string format, params object?[] args) => Log.Debug(format, args);
    public void LogInformation(string format, params object?[] args) => Log.Information(format, args);
    public void LogTrace(string format, params object?[] args) => Log.Verbose(format, args);
    public void LogWarning(string format, params object?[] args) => Log.Warning(format, args);
    public void LogError(string format, params object?[] args) => Log.Error(format, args);
    public void LogError(Exception ex, string format, params object?[] args) => Log.Error(ex, format, args);
}
