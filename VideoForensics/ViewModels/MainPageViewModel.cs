using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Ring.Api.Forensics.Models;
using System.Collections.ObjectModel;

namespace VideoForensics.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<EvidenceMetadata> evidenceList = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusMessage = "Ready to analyze evidence";

    public MainPageViewModel()
    {
        LoadSampleData();
    }

    [RelayCommand]
    private async Task LoadEvidence()
    {
        IsLoading = true;
        StatusMessage = "Loading evidence...";
        await Task.Delay(500);
        StatusMessage = $"Loaded {EvidenceList.Count} evidence items";
        IsLoading = false;
    }

    private void LoadSampleData()
    {
        EvidenceList.Add(new EvidenceMetadata
        {
            EvidenceId = "EVD-2026-001",
            ExtractionTimestamp = DateTime.UtcNow.AddHours(-2),
            SourceDeviceId = "RING-FRONT-DOOR",
            EventTimestamp = DateTime.UtcNow.AddHours(-3),
            EventType = "Signal Anomaly Detected",
            ExtractionHandler = "VideoForensics",
            Notes = "RF jamming suspected - sustained signal degradation"
        });

        EvidenceList.Add(new EvidenceMetadata
        {
            EvidenceId = "EVD-2026-002",
            ExtractionTimestamp = DateTime.UtcNow.AddHours(-5),
            SourceDeviceId = "RING-BACKYARD",
            EventTimestamp = DateTime.UtcNow.AddHours(-6),
            EventType = "Device Tampering",
            ExtractionHandler = "VideoForensics",
            Notes = "Unusual power cycle - possible physical tampering"
        });

        EvidenceList.Add(new EvidenceMetadata
        {
            EvidenceId = "EVD-2026-003",
            ExtractionTimestamp = DateTime.UtcNow.AddHours(-12),
            SourceDeviceId = "RING-FRONT-DOOR",
            EventTimestamp = DateTime.UtcNow.AddHours(-13),
            EventType = "Access Control Violation",
            ExtractionHandler = "VideoForensics",
            Notes = "Unauthorized access attempt detected in chain of custody"
        });
    }
}
