using Spectre.Console;

using ZimCom.Core.Modules.Static;

namespace ZimCom.ServerConsole.Modules.Static;
internal static class StaticLogWrapper {
    public static void WriteAnsiMarkupDebug(string message) {
        StaticLogModule.LogDebug(message);
        AnsiConsole.MarkupLine(message);
    }
}
