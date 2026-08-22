namespace VideoForensics.Core.Interfaces
{
    public interface IForensicsConfiguration
    {
        bool EnableForensicAnalysisReports { get; set; }
        bool EnableSignalAnomalyReports { get; set; }
        bool EnableChainOfCustodyReports { get; set; }
        bool EnableEvidenceValidationReports { get; set; }
        bool EnableAccessControlMonitoring { get; set; }
        bool EnableMultiDeviceAnalysis { get; set; }
        bool EnablePiiRedaction { get; set; }
        RedactionLevel RedactionLevel { get; set; }
        KeyStorageProvider KeyStorageProvider { get; set; }
        int RetentionDaysDefault { get; set; }
        string ReportOutputFormat { get; set; }
        string ReportsDirectory { get; set; }
        string LogLevel { get; set; }
        string DownloadLocation { get; set; }
    }

    public enum RedactionLevel
    {
        None = 0,
        Light = 1,
        Medium = 2,
        Heavy = 3
    }

    public enum KeyStorageProvider
    {
        Auto = 0,
        Tpm = 1,
        Dpapi = 2,
        FileBased = 3
    }
}
