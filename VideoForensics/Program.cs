using Spectre.Console;
using Ring.Api.Forensics.Models;
using Ring.Api.Forensics.Models.Reports;
using System.Collections.Generic;

namespace VideoForensics;

class Program
{
    static void Main(string[] args)
    {
        // Run demo mode if launched with --demo flag
        if (args.Length > 0 && args[0] == "--demo")
        {
            DemoMode.RunDemo();
            return;
        }
        AnsiConsole.MarkupLine("[bold green]═══════════════════════════════════════[/]");
        AnsiConsole.MarkupLine("[bold green]    VideoForensics - Evidence Analysis[/]");
        AnsiConsole.MarkupLine("[bold green]  DV Victim Protection & Tamper Detection[/]");
        AnsiConsole.MarkupLine("[bold green]═══════════════════════════════════════[/]");
        AnsiConsole.MarkupLine("");

        while (true)
        {
            AnsiConsole.MarkupLine("[yellow]Main Menu:[/]");
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select an option")
                    .HighlightStyle("green")
                    .AddChoices(
                        "View Evidence",
                        "Generate Reports",
                        "Signal Anomalies",
                        "Chain of Custody",
                        "Access Control",
                        "Exit"
                    ));

            switch (choice)
            {
                case "View Evidence":
                    ShowEvidence();
                    break;
                case "Generate Reports":
                    ShowReports();
                    break;
                case "Signal Anomalies":
                    ShowSignalAnomalies();
                    break;
                case "Chain of Custody":
                    ShowChainOfCustody();
                    break;
                case "Access Control":
                    ShowAccessControl();
                    break;
                case "Exit":
                    return;
            }

            AnsiConsole.MarkupLine("");
        }
    }

    static void ShowEvidence()
    {
        AnsiConsole.MarkupLine("[bold cyan]📋 Forensic Evidence[/]");
        var table = new Spectre.Console.Table();
        table.AddColumn("Evidence ID");
        table.AddColumn("Device");
        table.AddColumn("Type");
        table.AddColumn("Status");

        table.AddRow("EVD-2026-001", "RING-FRONT-DOOR", "Signal Anomaly", "[yellow]⚠ Flagged[/]");
        table.AddRow("EVD-2026-002", "RING-BACKYARD", "Device Tampering", "[red]🚨 Critical[/]");
        table.AddRow("EVD-2026-003", "RING-FRONT-DOOR", "Access Violation", "[yellow]⚠ Flagged[/]");

        AnsiConsole.Write(table);
    }

    static void ShowReports()
    {
        AnsiConsole.MarkupLine("[bold cyan]📊 Generated Reports[/]");
        var panel = new Panel("[green]✓ Forensic Analysis Report[/]\nRPT-2026-001 - RF Jamming Detected")
        {
            Header = new PanelHeader("Analysis Report"),
            Border = BoxBorder.Rounded
        };
        AnsiConsole.Write(panel);

        var panel2 = new Panel("[green]✓ Chain of Custody Report[/]\nRPT-2026-002 - Integrity Verified")
        {
            Header = new PanelHeader("Custody Report"),
            Border = BoxBorder.Rounded
        };
        AnsiConsole.Write(panel2);

        var panel3 = new Panel("[red]✗ Access Control Alert[/]\n3 suspicious access attempts")
        {
            Header = new PanelHeader("Access Report"),
            Border = BoxBorder.Rounded
        };
        AnsiConsole.Write(panel3);
    }

    static void ShowSignalAnomalies()
    {
        AnsiConsole.MarkupLine("[bold cyan]📡 Signal Strength Analysis[/]");
        var barChart = "[yellow]████████████[/] 65% - Normal range\n" +
                      "[orange3]██████[/] 32% - Degraded signal\n" +
                      "[red]███[/] 15% - Critical (Jamming detected)";
        AnsiConsole.MarkupLine(barChart);

        AnsiConsole.MarkupLine("\n[yellow]Anomalies Detected:[/]");
        var list = new Spectre.Console.Markup("• [red]RF Jamming Incident[/] - Front door camera\n");
        AnsiConsole.Write(list);
        list = new Spectre.Console.Markup("• [yellow]Signal Drop[/] - Backyard camera (3 min duration)\n");
        AnsiConsole.Write(list);
        list = new Spectre.Console.Markup("• [orange3]Sustained Degradation[/] - Side camera (25% loss)\n");
        AnsiConsole.Write(list);
    }

    static void ShowChainOfCustody()
    {
        AnsiConsole.MarkupLine("[bold cyan]🔐 Chain of Custody Audit[/]");
        var table = new Spectre.Console.Table();
        table.AddColumn("Handler");
        table.AddColumn("Action");
        table.AddColumn("Time");
        table.AddColumn("Status");

        table.AddRow("Officer Smith", "Reception", "2026-08-20 14:30", "[green]✓[/]");
        table.AddRow("Officer Johnson", "Analysis", "2026-08-20 16:45", "[green]✓[/]");
        table.AddRow("Lab Tech Brown", "Validation", "2026-08-20 18:20", "[green]✓[/]");

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine("\n[green]✓ Integrity Verified[/] - No tampering detected");
    }

    static void ShowAccessControl()
    {
        AnsiConsole.MarkupLine("[bold cyan]🚨 Evidence Access Monitoring[/]");
        AnsiConsole.MarkupLine("[red]High-Risk Alerts: 2[/]");

        var table = new Spectre.Console.Table();
        table.AddColumn("Alert Type");
        table.AddColumn("Evidence");
        table.AddColumn("Attempts");
        table.AddColumn("Severity");

        table.AddRow("[red]Failed Access[/]", "EVD-2026-001", "3", "[red]🔴 Critical[/]");
        table.AddRow("[yellow]Off-Hours Access[/]", "EVD-2026-002", "1", "[yellow]⚠ High[/]");

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine("\n[yellow]Recommendation:[/] Review access logs for potential evidence tampering");
    }
}
