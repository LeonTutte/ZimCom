using Spectre.Console;
using ZimCom.Core.Models;
using ZimCom.Core.Modules.Static.Misc;
using ZimCom.ServerConsole.Modules.Dynamic;

namespace ZimCom.ServerConsole;

internal static class Program
{
    static void Main(string[] args)
    {
        StaticLogModule.LogAppStart();
        AnsiConsole.MarkupLine("Starting [green]ZimCom Server[/]!");
        AnsiConsole.MarkupLine("Loaded [blue]configuration[/] files");
        var dynamicManagerModule = new DynamicManagerModuleServerExtras();
        if (dynamicManagerModule.InternalServer.Label == "Default Server")
        {
            AnsiConsole.MarkupLine("Created a [yellow]new Server[/] configuration file");
            dynamicManagerModule.InternalServer.Save();
        }

        AnsiConsole.MarkupLine($"Counting {dynamicManagerModule.InternalServer.Channels.Count} Channels");
        AnsiConsole.MarkupLine($"Counting {dynamicManagerModule.InternalServer.Groups.Count} Groups");
        AnsiConsole.MarkupLine(
            $"Counting {dynamicManagerModule.InternalServer.UserToGroup!.Count} user to group combinations");
        if (args.Length > 0 && bool.Parse(args[0]) is false)
        {
            AnsiConsole.MarkupLine("[yellow]Developer Mode -> Skipping configuration checks![/]");
        }
        else
        {
            dynamicManagerModule.DoBasicServerConfigChecks();
        }

        AnsiConsole.MarkupLine(
            $"Starting server on [blue]{Server.GetV6Address()}[/]");
        AnsiConsole.MarkupLine("Starting listener threads");
        dynamicManagerModule.StartNetworkListener().ConfigureAwait(false);
        AnsiConsole.MarkupLine("[yellow]Press any key to exit.[/]");
        Console.ReadKey();
    }
}