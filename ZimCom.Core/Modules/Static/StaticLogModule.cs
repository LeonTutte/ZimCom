using Serilog;

namespace ZimCom.Core.Modules.Static;
public class StaticLogModule {
    static StaticLogModule() {
        Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.Console()
                    .WriteTo.File(Path.Combine(StaticFileModule.GetApplicationDirectory(), "logs/log_.txt"), rollingInterval: RollingInterval.Day)
                    .CreateLogger();
    }
    public static void LogInformation(string message) {
        if (message is null) {
            Log.Error("Loginput was null");
            return;
        }
        Log.Information(message);
    }

    public static void LogDebug(string message) {
        if (message is null) {
            Log.Error("Loginput was null");
            return;
        }
        Log.Debug(message);
    }

    public static void LogAppStart() {
        Log.Information("-- Application was started on " + DateTime.Now + " ---");
    }

    public static void LogError(string message, Exception? ex) {
        if (ex is null) {
            Log.Error(message);
        } else {
            if (message is null) {
                Log.Error("Loginput was null");
                return;
            }
            Log.Error(message + " with exception: " + ex.Message);
        }
    }
}
