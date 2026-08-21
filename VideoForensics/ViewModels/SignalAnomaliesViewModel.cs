using Microsoft.Toolkit.Mvvm.ComponentModel;
using Ring.Api.Forensics.Models;
using System.Collections.ObjectModel;

namespace VideoForensics.ViewModels;

public partial class SignalAnomaliesViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<SignalAnomalyFinding> anomalies = new();

    [ObservableProperty]
    private double anomalyPercentage;

    public SignalAnomaliesViewModel()
    {
        LoadSampleAnomalies();
    }

    private void LoadSampleAnomalies()
    {
        AnomalyPercentage = 15.5;
        Anomalies.Add(new SignalAnomalyFinding
        {
            EventId = "EVT-001",
            DeviceId = "RING-FRONT-DOOR",
            EventTimestamp = DateTime.UtcNow.AddHours(-2),
            AnomalyType = SignalAnomalyType.ExtremelyWeakSignal,
            DeviationFromMedian = 45.2,
            Description = "Signal strength dropped to critical levels"
        });
    }
}
