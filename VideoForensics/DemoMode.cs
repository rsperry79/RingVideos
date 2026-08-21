using Spectre.Console;

namespace VideoForensics;

public static class DemoMode
{
    public static void RunDemo()
    {
        AnsiConsole.MarkupLine("[bold green]═══════════════════════════════════════[/]");
        AnsiConsole.MarkupLine("[bold green]    VideoForensics - Evidence Analysis[/]");
        AnsiConsole.MarkupLine("[bold green]  DV Victim Protection & Tamper Detection[/]");
        AnsiConsole.MarkupLine("[bold green]═══════════════════════════════════════[/]");
        AnsiConsole.MarkupLine("");

        ShowEvidence();
        AnsiConsole.MarkupLine("");
        ShowReports();
        AnsiConsole.MarkupLine("");
        ShowSignalAnomalies();
        AnsiConsole.MarkupLine("");
        ShowAccessControl();
        AnsiConsole.MarkupLine("");
        ShowRingVideosIntegration();
    }

    static void ShowEvidence()
    {
        AnsiConsole.MarkupLine("[bold cyan]📋 FORENSIC EVIDENCE[/]");
        var table = new Table();
        table.AddColumn("[bold]Evidence ID[/]");
        table.AddColumn("[bold]Device[/]");
        table.AddColumn("[bold]Type[/]");
        table.AddColumn("[bold]Status[/]");
        table.Border = TableBorder.Rounded;

        table.AddRow("EVD-2026-001", "RING-FRONT-DOOR", "Signal Anomaly", "[yellow]⚠ FLAGGED[/]");
        table.AddRow("EVD-2026-002", "RING-BACKYARD", "Device Tampering", "[red]🚨 CRITICAL[/]");
        table.AddRow("EVD-2026-003", "RING-FRONT-DOOR", "Access Violation", "[yellow]⚠ FLAGGED[/]");

        AnsiConsole.Write(table);
    }

    static void ShowReports()
    {
        AnsiConsole.MarkupLine("[bold cyan]📊 GENERATED REPORTS[/]");

        var panel1 = new Panel("[green]✓[/] [bold]Forensic Analysis Report[/]\nRPT-2026-001 - RF Jamming Detected\n[yellow]Status: CRITICAL[/]")
        {
            Header = new PanelHeader("[bold green]Analysis Report[/]"),
            Border = BoxBorder.Rounded
        };
        AnsiConsole.Write(panel1);

        var panel2 = new Panel("[green]✓[/] [bold]Chain of Custody Report[/]\nRPT-2026-002 - Integrity Verified\n[green]Status: VERIFIED[/]")
        {
            Header = new PanelHeader("[bold green]Custody Report[/]"),
            Border = BoxBorder.Rounded
        };
        AnsiConsole.Write(panel2);

        var panel3 = new Panel("[red]✗[/] [bold]Access Control Alert[/]\n3 suspicious access attempts\n[red]Status: CRITICAL[/]")
        {
            Header = new PanelHeader("[bold red]Access Report[/]"),
            Border = BoxBorder.Rounded
        };
        AnsiConsole.Write(panel3);
    }

    static void ShowSignalAnomalies()
    {
        AnsiConsole.MarkupLine("[bold cyan]📡 SIGNAL STRENGTH ANALYSIS[/]");
        AnsiConsole.MarkupLine("[green]████████████[/] 65% - Normal range");
        AnsiConsole.MarkupLine("[orange3]██████[/] 32% - Degraded signal");
        AnsiConsole.MarkupLine("[red]███[/] 15% - Critical (Jamming detected)");

        AnsiConsole.MarkupLine("\n[yellow]ANOMALIES DETECTED:[/]");
        AnsiConsole.MarkupLine("• [red]RF JAMMING INCIDENT[/] - Front door camera");
        AnsiConsole.MarkupLine("  └─ Duration: 15 minutes | Confidence: [red]DEFINITE[/]");
        AnsiConsole.MarkupLine("• [yellow]SIGNAL DROP[/] - Backyard camera");
        AnsiConsole.MarkupLine("  └─ Duration: 3 minutes | Deviation: 45.2%");
        AnsiConsole.MarkupLine("• [orange3]SUSTAINED DEGRADATION[/] - Side camera");
        AnsiConsole.MarkupLine("  └─ Duration: 25 minutes | Loss: 25%");
    }

    static void ShowRingVideosIntegration()
    {
        AnsiConsole.MarkupLine("[bold cyan]🔗 RING.VIDEOS INTEGRATION[/]");
        var panel = new Panel("[green]✓ Chain of Custody[/] - Managed by Ring.Videos\n[green]✓ Video Downloads[/] - Ring device management\n[green]✓ Device Authentication[/] - Secure access\n[green]✓ Evidence Storage[/] - Forensic preservation")
        {
            Header = new PanelHeader("[bold green]Linked Systems[/]"),
            Border = BoxBorder.Rounded
        };
        AnsiConsole.Write(panel);
        AnsiConsole.MarkupLine("\n[yellow]Launch Ring.Videos from VideoForensics menu to manage:[/]");
        AnsiConsole.MarkupLine("  • Chain of custody tracking with cryptographic verification");
        AnsiConsole.MarkupLine("  • Evidence video downloads and processing");
        AnsiConsole.MarkupLine("  • Device authentication and authorization");
    }

    static void ShowAccessControl()
    {
        AnsiConsole.MarkupLine("[bold cyan]🚨 EVIDENCE ACCESS MONITORING[/]");
        AnsiConsole.MarkupLine("[red]HIGH-RISK ALERTS: 2[/]");
        AnsiConsole.MarkupLine("");

        var table = new Table();
        table.AddColumn("[bold]Alert Type[/]");
        table.AddColumn("[bold]Evidence[/]");
        table.AddColumn("[bold]Attempts[/]");
        table.AddColumn("[bold]Severity[/]");
        table.Border = TableBorder.Rounded;

        table.AddRow("[red]Failed Access Attempt[/]", "EVD-2026-001", "3", "[red]🔴 CRITICAL[/]");
        table.AddRow("[yellow]Off-Hours Access[/]", "EVD-2026-002", "1", "[yellow]⚠ HIGH[/]");

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine("\n[red]RECOMMENDATION:[/] Investigate unauthorized access attempts");
        AnsiConsole.MarkupLine("[yellow]ACTION:[/] Review handler credentials and access logs");
    }
}
