using System;
using System.Text.Json.Serialization;

namespace Ring.Videos
{
    public class ForensicsConfiguration
    {
        [JsonPropertyName("enableForensicAnalysisReports")]
        public bool EnableForensicAnalysisReports { get; set; } = true;

        [JsonPropertyName("enableSignalAnomalyReports")]
        public bool EnableSignalAnomalyReports { get; set; } = true;

        [JsonPropertyName("enableChainOfCustodyReports")]
        public bool EnableChainOfCustodyReports { get; set; } = true;

        [JsonPropertyName("enableEvidenceValidationReports")]
        public bool EnableEvidenceValidationReports { get; set; } = true;

        [JsonPropertyName("enableAccessControlMonitoring")]
        public bool EnableAccessControlMonitoring { get; set; } = true;

        [JsonPropertyName("enableMultiDeviceAnalysis")]
        public bool EnableMultiDeviceAnalysis { get; set; } = true;

        [JsonPropertyName("enablePiiRedaction")]
        public bool EnablePiiRedaction { get; set; } = true;

        [JsonPropertyName("redactionLevel")]
        public RedactionLevel RedactionLevel { get; set; } = RedactionLevel.Medium;

        [JsonPropertyName("keyStorageProvider")]
        public KeyStorageProvider KeyStorageProvider { get; set; } = KeyStorageProvider.Auto;

        [JsonPropertyName("retentionDaysDefault")]
        public int RetentionDaysDefault { get; set; } = 365;

        [JsonPropertyName("reportOutputFormat")]
        public string ReportOutputFormat { get; set; } = "json";

        [JsonPropertyName("reportsDirectory")]
        public string ReportsDirectory { get; set; } = "";

        [JsonPropertyName("logLevel")]
        public string LogLevel { get; set; } = "Information";
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
