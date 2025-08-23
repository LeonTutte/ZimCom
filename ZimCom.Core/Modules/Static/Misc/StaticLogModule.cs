using Serilog;
using Serilog.Events;

namespace ZimCom.Core.Modules.Static.Misc;

/// <summary>
/// Provides static methods for logging messages and errors within the application.
/// </summary>
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

    /// <summary>
    /// Logs a info message with a specified string message.
    /// </summary>
    /// <param name="message">The debug message to log.</param>
    public static void LogInformation(string message) => Log.Information(message);

    /// <summary>
    /// Logs a debug message with a specified string message.
    /// </summary>
    /// <param name="message">The debug message to log.</param>
    public static void LogDebug(string message) => Log.Debug(message);

    /// Logs a message indicating that the application has started.
    /// This method captures the current date and time of the application start
    /// and writes the information to the log using the configured logging system.
    public static void LogAppStart() => Log.Information("-- Application was started on " + DateTime.Now + " ---");

    /// <summary>
    /// Logs an exception and/or error message with a specified string message.
    /// </summary>
    /// <param name="message">The debug message to log.</param>
    /// <param name="ex">The exception associated with the error can be null.</param>
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