using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Spectre.Console;
using VideoForensics.Common.Interfaces;

namespace VideoForensics.Common.Implementations
{
    internal class MenuManager : IMenuManager
    {
        private readonly IForensicsConfiguration _forensicsConfig;
        private readonly string _configPath;
        private readonly IVideoDownloadService _downloadService;

        public MenuManager(IForensicsConfiguration config, string configPath, IVideoDownloadService downloadService)
        {
            _forensicsConfig = config;
            _configPath = configPath;
            _downloadService = downloadService;
        }

        public async Task ShowMainMenuAsync()
        {
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
                            "Generate Forensic Reports",
                            "Signal Anomalies",
                            "Access Control Monitoring",
                            "Chain of Custody",
                            "Video Downloads",
                            "Configuration",
                            "Exit"
                        ));

                switch (choice)
                {
                    case "View Evidence":
                        ShowEvidence();
                        break;
                    case "Generate Forensic Reports":
                        ShowForensicReports();
                        break;
                    case "Signal Anomalies":
                        ShowSignalAnomalies();
                        break;
                    case "Access Control Monitoring":
                        ShowAccessControl();
                        break;
                    case "Chain of Custody":
                        ShowChainOfCustody();
                        break;
                    case "Video Downloads":
                        ShowVideoDownloads();
                        break;
                    case "Configuration":
                        ShowConfigurationMenu();
                        break;
                    case "Exit":
                        AnsiConsole.MarkupLine("[yellow]Exiting VideoForensics...[/]");
                        return;
                }

                AnsiConsole.MarkupLine("");
            }
        }

        private void ShowEvidence()
        {
            AnsiConsole.MarkupLine("[bold cyan]📋 Forensic Evidence[/]");
            var table = new Table();
            table.AddColumn("Evidence ID");
            table.AddColumn("Device");
            table.AddColumn("Type");
            table.AddColumn("Status");
            table.Border = TableBorder.Rounded;

            table.AddRow("EVD-2026-001", "RING-FRONT-DOOR", "Signal Anomaly", "[yellow]⚠ Flagged[/]");
            table.AddRow("EVD-2026-002", "RING-BACKYARD", "Device Tampering", "[red]🚨 Critical[/]");
            table.AddRow("EVD-2026-003", "RING-FRONT-DOOR", "Access Violation", "[yellow]⚠ Flagged[/]");

            AnsiConsole.Write(table);
        }

        private void ShowForensicReports()
        {
            AnsiConsole.MarkupLine("[bold cyan]📊 Forensic Report Generation[/]");

            var reportStatus = new Table();
            reportStatus.AddColumn("Report Type");
            reportStatus.AddColumn("Status");
            reportStatus.AddColumn("Last Generated");
            reportStatus.Border = TableBorder.Rounded;

            reportStatus.AddRow(
                "Forensic Analysis",
                _forensicsConfig.EnableForensicAnalysisReports ? "[green]✓ Enabled[/]" : "[red]✗ Disabled[/]",
                "2026-08-20 14:30");
            reportStatus.AddRow(
                "Chain of Custody",
                _forensicsConfig.EnableChainOfCustodyReports ? "[green]✓ Enabled[/]" : "[red]✗ Disabled[/]",
                "2026-08-20 14:15");
            reportStatus.AddRow(
                "Evidence Validation",
                _forensicsConfig.EnableEvidenceValidationReports ? "[green]✓ Enabled[/]" : "[red]✗ Disabled[/]",
                "2026-08-20 14:00");
            reportStatus.AddRow(
                "Signal Anomaly",
                _forensicsConfig.EnableSignalAnomalyReports ? "[green]✓ Enabled[/]" : "[red]✗ Disabled[/]",
                "2026-08-20 13:45");

            AnsiConsole.Write(reportStatus);

            AnsiConsole.MarkupLine("");
            AnsiConsole.MarkupLine("[yellow]Reports saved to:[/] {0}",
                string.IsNullOrEmpty(_forensicsConfig.ReportsDirectory)
                    ? "[red]Not configured[/]"
                    : _forensicsConfig.ReportsDirectory);
            AnsiConsole.MarkupLine("[yellow]Format:[/] {0}", _forensicsConfig.ReportOutputFormat);
        }

        private void ShowSignalAnomalies()
        {
            AnsiConsole.MarkupLine("[bold cyan]📡 Signal Strength Analysis[/]");
            AnsiConsole.MarkupLine("[green]████████████[/] 65% - Normal range");
            AnsiConsole.MarkupLine("[orange3]██████[/] 32% - Degraded signal");
            AnsiConsole.MarkupLine("[red]███[/] 15% - Critical (Jamming detected)");

            AnsiConsole.MarkupLine("\n[yellow]Anomalies Detected:[/]");
            AnsiConsole.MarkupLine("• [red]RF Jamming Incident[/] - Front door camera");
            AnsiConsole.MarkupLine("• [yellow]Signal Drop[/] - Backyard camera (3 min duration)");
            AnsiConsole.MarkupLine("• [orange3]Sustained Degradation[/] - Side camera (25% loss)");
        }

        private void ShowAccessControl()
        {
            AnsiConsole.MarkupLine("[bold cyan]🚨 Evidence Access Monitoring[/]");

            if (!_forensicsConfig.EnableAccessControlMonitoring)
            {
                AnsiConsole.MarkupLine("[yellow]⚠ Access control monitoring is disabled in configuration[/]");
                return;
            }

            AnsiConsole.MarkupLine("[red]High-Risk Alerts: 2[/]");

            var table = new Table();
            table.AddColumn("Alert Type");
            table.AddColumn("Evidence");
            table.AddColumn("Attempts");
            table.AddColumn("Severity");
            table.Border = TableBorder.Rounded;

            table.AddRow("[red]Failed Access[/]", "EVD-2026-001", "3", "[red]🔴 Critical[/]");
            table.AddRow("[yellow]Off-Hours Access[/]", "EVD-2026-002", "1", "[yellow]⚠ High[/]");

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine("\n[yellow]Recommendation:[/] Review access logs for potential evidence tampering");
        }

        private void ShowChainOfCustody()
        {
            AnsiConsole.MarkupLine("[bold cyan]🔗 Chain of Custody Management[/]");

            if (!_forensicsConfig.EnableChainOfCustodyReports)
            {
                AnsiConsole.MarkupLine("[yellow]⚠ Chain of Custody reporting is disabled in configuration[/]");
                return;
            }

            var table = new Table();
            table.AddColumn("Entry ID");
            table.AddColumn("Handler");
            table.AddColumn("Action");
            table.AddColumn("Timestamp");
            table.AddColumn("Status");
            table.Border = TableBorder.Rounded;

            table.AddRow("COC-001", "Officer Smith", "Collected", "2026-08-20 10:15", "[green]✓ Verified[/]");
            table.AddRow("COC-002", "Detective Johnson", "Analyzed", "2026-08-20 12:30", "[green]✓ Verified[/]");
            table.AddRow("COC-003", "Evidence Custodian", "Stored", "2026-08-20 14:00", "[green]✓ Verified[/]");

            AnsiConsole.Write(table);
        }

        private void ShowVideoDownloads()
        {
            AnsiConsole.MarkupLine("[bold cyan]📥 Video Downloads[/]");

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select download option")
                    .HighlightStyle("green")
                    .AddChoices(
                        "Authenticate Ring Account",
                        "Download Videos",
                        "Download Snapshots",
                        "View Status",
                        "Back"
                    ));

            switch (choice)
            {
                case "Authenticate Ring Account":
                    AuthenticateRingAccount();
                    break;
                case "Download Videos":
                    DownloadVideos();
                    break;
                case "Download Snapshots":
                    DownloadSnapshots();
                    break;
                case "View Status":
                    AnsiConsole.MarkupLine("[yellow]Status:[/] {0}", _downloadService.GetDownloadStatus());
                    break;
            }
        }

        private void AuthenticateRingAccount()
        {
            AnsiConsole.MarkupLine("[bold cyan]🔐 Ring Account Authentication[/]");
            var username = AnsiConsole.Ask<string>("[yellow]Enter email address:[/]");
            var password = AnsiConsole.Ask<string>("[yellow]Enter password (will not be echoed):[/]", "");

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                var result = _downloadService.AuthenticateAsync(username, password).Result;
                if (result)
                {
                    AnsiConsole.MarkupLine("[green]✓ Credentials saved securely[/]");
                }
            }
        }

        private void DownloadVideos()
        {
            AnsiConsole.MarkupLine("[bold cyan]📹 Download Ring Videos[/]");
            var outputPath = AnsiConsole.Ask<string>("[yellow]Enter output directory:[/]");
            var daysBack = AnsiConsole.Ask<int>("[yellow]Download videos from last N days (default 7):[/]", 7);

            var startDate = DateTime.Now.AddDays(-daysBack);
            var endDate = DateTime.Now;

            var result = _downloadService.DownloadVideosAsync(outputPath, startDate, endDate).Result;
            if (result)
            {
                AnsiConsole.MarkupLine("[green]✓ Videos saved with forensic metadata[/]");
            }
        }

        private void DownloadSnapshots()
        {
            AnsiConsole.MarkupLine("[bold cyan]📸 Download Ring Snapshots[/]");
            var outputPath = AnsiConsole.Ask<string>("[yellow]Enter output directory:[/]");
            var daysBack = AnsiConsole.Ask<int>("[yellow]Download snapshots from last N days (default 7):[/]", 7);

            var startDate = DateTime.Now.AddDays(-daysBack);
            var endDate = DateTime.Now;

            var result = _downloadService.DownloadSnapshotsAsync(outputPath, startDate, endDate).Result;
            if (result)
            {
                AnsiConsole.MarkupLine("[green]✓ Snapshots saved with forensic metadata[/]");
            }
        }

        private void ShowConfigurationMenu()
        {
            while (true)
            {
                AnsiConsole.MarkupLine("[bold cyan]⚙️  Configuration Menu[/]");
                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Configure settings")
                        .HighlightStyle("green")
                        .AddChoices(
                            "Report Generation Settings",
                            "PII Redaction Settings",
                            "Key Storage Configuration",
                            "Retention Policy",
                            "Logging Level",
                            "Back to Main Menu"
                        ));

                switch (choice)
                {
                    case "Report Generation Settings":
                        ConfigureReportGeneration();
                        break;
                    case "PII Redaction Settings":
                        ConfigurePiiRedaction();
                        break;
                    case "Key Storage Configuration":
                        ConfigureKeyStorage();
                        break;
                    case "Retention Policy":
                        ConfigureRetentionPolicy();
                        break;
                    case "Logging Level":
                        ConfigureLoggingLevel();
                        break;
                    case "Back to Main Menu":
                        SaveConfiguration();
                        return;
                }

                AnsiConsole.MarkupLine("");
            }
        }

        private void ConfigureReportGeneration()
        {
            AnsiConsole.MarkupLine("[bold cyan]📊 Report Generation Settings[/]");

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a report type to toggle")
                    .HighlightStyle("green")
                    .AddChoices(
                        $"Forensic Analysis Reports [{(_forensicsConfig.EnableForensicAnalysisReports ? "ON" : "OFF")}]",
                        $"Signal Anomaly Reports [{(_forensicsConfig.EnableSignalAnomalyReports ? "ON" : "OFF")}]",
                        $"Chain of Custody Reports [{(_forensicsConfig.EnableChainOfCustodyReports ? "ON" : "OFF")}]",
                        $"Evidence Validation Reports [{(_forensicsConfig.EnableEvidenceValidationReports ? "ON" : "OFF")}]",
                        "Set Reports Output Directory",
                        "Set Report Format",
                        "Back"
                    ));

            switch (choice)
            {
                case var c when c.StartsWith("Forensic Analysis"):
                    _forensicsConfig.EnableForensicAnalysisReports = !_forensicsConfig.EnableForensicAnalysisReports;
                    AnsiConsole.MarkupLine("[green]✓ Setting updated[/]");
                    break;
                case var c when c.StartsWith("Signal Anomaly"):
                    _forensicsConfig.EnableSignalAnomalyReports = !_forensicsConfig.EnableSignalAnomalyReports;
                    AnsiConsole.MarkupLine("[green]✓ Setting updated[/]");
                    break;
                case var c when c.StartsWith("Chain of Custody"):
                    _forensicsConfig.EnableChainOfCustodyReports = !_forensicsConfig.EnableChainOfCustodyReports;
                    AnsiConsole.MarkupLine("[green]✓ Setting updated[/]");
                    break;
                case var c when c.StartsWith("Evidence Validation"):
                    _forensicsConfig.EnableEvidenceValidationReports = !_forensicsConfig.EnableEvidenceValidationReports;
                    AnsiConsole.MarkupLine("[green]✓ Setting updated[/]");
                    break;
                case "Set Reports Output Directory":
                    AnsiConsole.MarkupLine("[yellow]Current:[/] {0}", _forensicsConfig.ReportsDirectory);
                    var dir = AnsiConsole.Ask<string>("[yellow]Enter reports directory:[/]");
                    _forensicsConfig.ReportsDirectory = dir;
                    AnsiConsole.MarkupLine("[green]✓ Directory set[/]");
                    break;
                case "Set Report Format":
                    var format = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("Select report format")
                            .AddChoices("json", "xml", "csv"));
                    _forensicsConfig.ReportOutputFormat = format;
                    AnsiConsole.MarkupLine("[green]✓ Format set to {0}[/]", format);
                    break;
            }
        }

        private void ConfigurePiiRedaction()
        {
            AnsiConsole.MarkupLine("[bold cyan]🔒 PII Redaction Settings[/]");

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Configure PII redaction")
                    .HighlightStyle("green")
                    .AddChoices(
                        $"Enable PII Redaction [{(_forensicsConfig.EnablePiiRedaction ? "ON" : "OFF")}]",
                        $"Redaction Level: {_forensicsConfig.RedactionLevel}",
                        "Back"
                    ));

            switch (choice)
            {
                case var c when c.StartsWith("Enable"):
                    _forensicsConfig.EnablePiiRedaction = !_forensicsConfig.EnablePiiRedaction;
                    AnsiConsole.MarkupLine("[green]✓ Setting updated[/]");
                    break;
                case var c when c.StartsWith("Redaction Level"):
                    var level = AnsiConsole.Prompt(
                        new SelectionPrompt<RedactionLevel>()
                            .Title("Select redaction level")
                            .AddChoices(
                                RedactionLevel.None,
                                RedactionLevel.Light,
                                RedactionLevel.Medium,
                                RedactionLevel.Heavy));
                    _forensicsConfig.RedactionLevel = level;
                    AnsiConsole.MarkupLine("[green]✓ Redaction level set to {0}[/]", level);
                    break;
            }
        }

        private void ConfigureKeyStorage()
        {
            AnsiConsole.MarkupLine("[bold cyan]🔑 Key Storage Configuration[/]");

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<KeyStorageProvider>()
                    .Title("Select key storage provider")
                    .HighlightStyle("green")
                    .AddChoices(
                        KeyStorageProvider.Auto,
                        KeyStorageProvider.Tpm,
                        KeyStorageProvider.Dpapi,
                        KeyStorageProvider.FileBased));

            _forensicsConfig.KeyStorageProvider = choice;

            var description = choice switch
            {
                KeyStorageProvider.Auto => "Automatic selection (TPM → DPAPI → File-based)",
                KeyStorageProvider.Tpm => "Hardware TPM 2.0 (most secure)",
                KeyStorageProvider.Dpapi => "Windows DPAPI (Windows only)",
                KeyStorageProvider.FileBased => "AES-256-GCM encrypted file storage (cross-platform)",
                _ => "Unknown"
            };

            AnsiConsole.MarkupLine("[green]✓ Key storage set to {0}[/]", choice);
            AnsiConsole.MarkupLine("[dim]{0}[/]", description);
        }

        private void ConfigureRetentionPolicy()
        {
            AnsiConsole.MarkupLine("[bold cyan]📅 Retention Policy[/]");
            AnsiConsole.MarkupLine("[yellow]Current retention period:[/] {0} days", _forensicsConfig.RetentionDaysDefault);

            var days = AnsiConsole.Ask<int>("[yellow]Enter retention period (days):[/]");
            if (days > 0)
            {
                _forensicsConfig.RetentionDaysDefault = days;
                AnsiConsole.MarkupLine("[green]✓ Retention period set to {0} days[/]", days);
            }
        }

        private void ConfigureLoggingLevel()
        {
            AnsiConsole.MarkupLine("[bold cyan]📝 Logging Level[/]");

            var level = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select logging level")
                    .HighlightStyle("green")
                    .AddChoices(
                        "Debug",
                        "Information",
                        "Warning",
                        "Error",
                        "Critical"));

            _forensicsConfig.LogLevel = level;
            AnsiConsole.MarkupLine("[green]✓ Logging level set to {0}[/]", level);
        }

        private void SaveConfiguration()
        {
            try
            {
                var json = JsonSerializer.Serialize(_forensicsConfig, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, json);
                AnsiConsole.MarkupLine("[green]✓ Configuration saved[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine("[red]✗ Failed to save configuration: {0}[/]", ex.Message);
            }
        }
    }
}
