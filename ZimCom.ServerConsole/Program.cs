using System.Net.Quic;
using System.Runtime.Versioning;
using Spectre.Console;
using ZimCom.Core.Models;
using ZimCom.Core.Modules.Static.Misc;
using ZimCom.ServerConsole.Modules.Dynamic;

namespace ZimCom.ServerConsole;

[SupportedOSPlatform("linux")]
[SupportedOSPlatform("windows")]
[SupportedOSPlatform("macOS")]
internal static class Program
{
    private static DynamicManagerModuleServerExtras? _dynamicManagerModule;

    static void Main(string[] args)
    {
        StaticLogModule.LogAppStart();
        AnsiConsole.MarkupLine("Starting [green]ZimCom Server[/]!");
        AnsiConsole.MarkupLine("Loaded [blue]configuration[/] files");
        _dynamicManagerModule = new DynamicManagerModuleServerExtras();
        if (_dynamicManagerModule.Server.Label == "Default Server")
        {
            AnsiConsole.MarkupLine("Created a [yellow]new Server[/] configuration file");
            _dynamicManagerModule.Server.Save();
        }

        AnsiConsole.MarkupLine($"Counting {_dynamicManagerModule.Server.Channels.Count} Channels");
        AnsiConsole.MarkupLine($"Counting {_dynamicManagerModule.Server.Groups.Count} Groups");
        AnsiConsole.MarkupLine(
            $"Counting {_dynamicManagerModule.Server.UserToGroup!.Count} user to group combinations");
        if (args.Length > 0 && bool.Parse(args[0]) is false)
        {
            AnsiConsole.MarkupLine("[yellow]Developer Mode -> Skipping configuration checks![/]");
        }
        else
        {
            _dynamicManagerModule.DoBasicServerConfigChecks();
        }

        AnsiConsole.MarkupLine(
            $"Starting server on [blue]{Server.HostName}[/] and {Server.ServerUrl ?? "[red]null domain[/]"}" +
            $" and [blue]{Server.GetV6Address()}[/]");
        AnsiConsole.MarkupLine($"[green]IPv6 Status[/] is:{Environment.NewLine}" +
                               $"   Link Local: [blue]{Server.GetV6Address().IsIPv6LinkLocal}{Environment.NewLine}[/]" +
                               $"   Multicast: [blue]{Server.GetV6Address().IsIPv6Multicast}{Environment.NewLine}[/]" +
                               $"   Site Local: [blue]{Server.GetV6Address().IsIPv6SiteLocal}{Environment.NewLine}[/]" +
                               $"   Teredo: [blue]{Server.GetV6Address().IsIPv6Teredo}{Environment.NewLine}[/]" +
                               $"   Unique Local: [blue]{Server.GetV6Address().IsIPv6UniqueLocal}[/]");
        if (QuicConnection.IsSupported is false)
        {
            AnsiConsole.MarkupLine(
                "[red]QUIC is not supported[/], check for presence of [blue]libmsquic[/] and support of [blue]TLS 1.3[/].");
            AnsiConsole.MarkupLine("[red]Terminating server...[/]");
            AnsiConsole.MarkupLine("[yellow]Press any key to exit.[/]");
            Console.ReadKey();
            Environment.Exit(-1);
        }

        AnsiConsole.MarkupLine($"[green]QUIC Status[/] is:{Environment.NewLine}" +
                               $" Connection: [blue]{QuicConnection.IsSupported}{Environment.NewLine}[/]" +
                               $" Listener: [blue]{QuicListener.IsSupported}{Environment.NewLine}[/]");
        AnsiConsole.MarkupLine("Starting listener threads");
        // Listener Threads
        // Task.Run(() =>
        // {
        //     _dynamicManagerModule.StartTcpListener();
        //     return Task.CompletedTask;
        // }).ConfigureAwait(false);
        Task.Run(() =>
        {
            _dynamicManagerModule.StartQuicListener();
            return Task.CompletedTask;
        }).ConfigureAwait(false);

        AnsiConsole.MarkupLine("[yellow]Press any key to exit.[/]");
        Console.ReadKey();
    }
}