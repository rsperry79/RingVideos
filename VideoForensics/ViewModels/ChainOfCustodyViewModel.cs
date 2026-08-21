using Microsoft.Toolkit.Mvvm.ComponentModel;
using Ring.Api.Forensics.Models;
using System.Collections.ObjectModel;

namespace VideoForensics.ViewModels;

public partial class ChainOfCustodyViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<ChainOfCustodyEntry> custodyEntries = new();

    [ObservableProperty]
    private bool integrityVerified;

    public ChainOfCustodyViewModel()
    {
        LoadSampleData();
    }

    private void LoadSampleData()
    {
        IntegrityVerified = true;
        CustodyEntries.Add(new ChainOfCustodyEntry
        {
            EvidenceId = "EVD-001",
            Handler = "Officer Smith",
            Action = "reception",
            Timestamp = DateTime.UtcNow.AddHours(-24)
        });
        CustodyEntries.Add(new ChainOfCustodyEntry
        {
            EvidenceId = "EVD-001",
            Handler = "Officer Johnson",
            Action = "analysis",
            Timestamp = DateTime.UtcNow.AddHours(-8)
        });
    }
}
