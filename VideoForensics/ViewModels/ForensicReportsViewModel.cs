using Microsoft.Toolkit.Mvvm.ComponentModel;
using Ring.Api.Forensics.Models.Reports;
using System.Collections.ObjectModel;

namespace VideoForensics.ViewModels;

public partial class ForensicReportsViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<ForensicAnalysisReport> reports = new();

    [ObservableProperty]
    private string selectedReportFormat = "JSON";

    public ForensicReportsViewModel()
    {
        LoadSampleReports();
    }

    private void LoadSampleReports()
    {
        Reports.Add(new ForensicAnalysisReport
        {
            ReportId = "RPT-2026-001",
            GeneratedAt = DateTime.UtcNow,
            AnalysisType = "Signal Integrity Analysis",
            Summary = "Critical RF jamming detected on front door camera"
        });
    }
}
