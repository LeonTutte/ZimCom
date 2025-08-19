using Serilog;
using Serilog.Events;

namespace ZimCom.Core.Modules.Static.Misc;

public static class StaticLogModule
{
    static StaticLogModule()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File("logs/log_.txt", rollingInterval: RollingInterval.Day, buffered: false,
                restrictedToMinimumLevel: LogEventLevel.Verbose)
            .CreateLogger();
    }

    public static void LogInformation(string message) => Log.Information(message);

    public static void LogDebug(string message) => Log.Debug(message);

    public static void LogAppStart() => Log.Information("-- Application was started on " + DateTime.Now + " ---");

    public static void LogError(string message, Exception? ex)
    {
        switch (ex)
        {
            case null:
                Log.Error(message);
                break;
            default:
                Log.Error(message + " with exception: " + ex.Message);
                break;
        }
    }
}