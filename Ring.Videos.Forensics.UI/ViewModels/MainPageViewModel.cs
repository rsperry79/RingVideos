using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Ring.Api.Forensics.Models;
using Ring.Api.Forensics.Models.Reports;
using System.Collections.ObjectModel;

namespace Ring.Videos.Forensics.UI.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<EvidenceMetadata> evidenceList = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusMessage = "Ready to load evidence";

    [ObservableProperty]
    private EvidenceMetadata? selectedEvidence;

    public MainPageViewModel()
    {
        LoadSampleData();
    }

    [RelayCommand]
    private async Task LoadEvidence()
    {
        IsLoading = true;
        StatusMessage = "Loading evidence...";

        try
        {
            // TODO: Integrate with Ring.Api.Forensics to load real evidence
            // For now, using sample data
            await Task.Delay(500);
            StatusMessage = $"Loaded {EvidenceList.Count} evidence items";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ViewDetails(EvidenceMetadata evidence)
    {
        if (evidence == null) return;
        SelectedEvidence = evidence;
        // Navigate to details page
        await Shell.Current.GoToAsync($"details?id={evidence.EvidenceId}");
    }

    [RelayCommand]
    private async Task DeleteEvidence(EvidenceMetadata evidence)
    {
        if (evidence == null) return;

        var result = await Application.Current?.MainPage?.DisplayAlert(
            "Confirm Deletion",
            $"Are you sure you want to delete evidence {evidence.EvidenceId}?",
            "Delete", "Cancel") ?? false;

        if (result)
        {
            EvidenceList.Remove(evidence);
            StatusMessage = "Evidence deleted";
        }
    }

    private void LoadSampleData()
    {
        // Sample data for demonstration
        EvidenceList.Add(new EvidenceMetadata
        {
            EvidenceId = "EVD-001",
            ExtractionTimestamp = DateTime.UtcNow.AddHours(-2),
            SourceDeviceId = "CAM-101",
            EventTimestamp = DateTime.UtcNow.AddHours(-3),
            EventType = "Motion Detection",
            ExtractionHandler = "Automated System",
            Notes = "Potential tampering detected"
        });

        EvidenceList.Add(new EvidenceMetadata
        {
            EvidenceId = "EVD-002",
            ExtractionTimestamp = DateTime.UtcNow.AddHours(-5),
            SourceDeviceId = "CAM-102",
            EventTimestamp = DateTime.UtcNow.AddHours(-6),
            EventType = "Signal Loss",
            ExtractionHandler = "Automated System",
            Notes = "Sustained signal degradation"
        });
    }
}
