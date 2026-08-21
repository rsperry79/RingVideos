using Microsoft.Toolkit.Mvvm.ComponentModel;
using Ring.Api.Forensics.Models;
using System.Collections.ObjectModel;

namespace VideoForensics.ViewModels;

public partial class AccessControlViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<AccessAnomaly> suspiciousAccess = new();

    [ObservableProperty]
    private int highRiskCount;

    public AccessControlViewModel()
    {
        LoadSampleData();
    }

    private void LoadSampleData()
    {
        HighRiskCount = 2;
        SuspiciousAccess.Add(new AccessAnomaly
        {
            EvidenceId = "EVD-001",
            AnomalyType = "Failed Access Attempt",
            Severity = AccessAnomalySeverity.High,
            DetectedAt = DateTime.UtcNow.AddHours(-1),
            AffectedUserId = "USER-UNKNOWN",
            FailedAttemptCount = 3
        });
    }
}
