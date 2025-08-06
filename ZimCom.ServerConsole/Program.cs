using Spectre.Console;
using ZimCom.Core.Modules.Dynamic.Misc;
using ZimCom.Core.Modules.Static.Misc;

namespace ZimCom.ServerConsole;

internal class Program {
    private static DynamicManagerModuleServerExtras? _dynamicManagerModule;
    static void Main() {
        StaticLogModule.LogAppStart();
        AnsiConsole.MarkupLine("Starting [green]ZimCom Server[/]!");
        AnsiConsole.MarkupLine("Loaded [blue]configuration[/] files ");
        _dynamicManagerModule = new DynamicManagerModuleServerExtras();
        if (_dynamicManagerModule.Server.Label == "Default Server") {
            AnsiConsole.MarkupLine("Created a [yellow]new Server[/] configuration file");
            _dynamicManagerModule.Server.Save();
        }
        AnsiConsole.MarkupLine($"Counting {_dynamicManagerModule.Server.Channels.Count} Channels");
        AnsiConsole.MarkupLine($"Counting {_dynamicManagerModule.Server.Groups.Count} Groups");
        AnsiConsole.MarkupLine($"Counting {_dynamicManagerModule.Server.UserToGroup!.Count} user to group combinations");
        AnsiConsole.MarkupLine($"Starting server on [blue]{_dynamicManagerModule.Server.HostName}[/]" +
                               $" and [blue]{_dynamicManagerModule.Server.IpAddress.MapToIPv4()}[/]" +
                               $" | [blue]{_dynamicManagerModule.Server.IpAddress}[/]" +
                               $" as [blue]{_dynamicManagerModule.Server.IpAddress.AddressFamily}[/]");
        AnsiConsole.MarkupLine($"[green]IPv4 Status[/] is:{Environment.NewLine}" +
                               $"   Mapped to IPv6: [blue]{_dynamicManagerModule.Server.IpAddress.IsIPv4MappedToIPv6}[/]");
        AnsiConsole.MarkupLine($"[green]IPv6 Status[/] is:{Environment.NewLine}" +
                               $"   Link Local: [blue]{_dynamicManagerModule.Server.IpAddress.IsIPv6LinkLocal}{Environment.NewLine}[/]" +
                               $"   Multicast: [blue]{_dynamicManagerModule.Server.IpAddress.IsIPv6Multicast}{Environment.NewLine}[/]" +
                               $"   Site Local: [blue]{_dynamicManagerModule.Server.IpAddress.IsIPv6SiteLocal}{Environment.NewLine}[/]" +
                               $"   Teredo: [blue]{_dynamicManagerModule.Server.IpAddress.IsIPv6Teredo}{Environment.NewLine}[/]" +
                               $"   Unique Local: [blue]{_dynamicManagerModule.Server.IpAddress.IsIPv6UniqueLocal}[/]");
        
        _dynamicManagerModule.StartServerListener();
    }
}