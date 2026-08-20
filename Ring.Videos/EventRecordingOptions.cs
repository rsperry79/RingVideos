using System;

namespace Ring.Videos
{
    /// <summary>
    /// Configuration options for what data to include in the DownloadedEventRecord JSON files
    /// that are written alongside downloaded videos. Allows balancing between comprehensive
    /// audit trails and file size/privacy concerns.
    /// </summary>
    public class EventRecordingOptions
    {
        /// <summary>
        /// Whether to write DownloadedEventRecord JSON files for each successful download.
        /// Default: true
        /// </summary>
        public bool WriteEventRecords { get; set; } = true;

        /// <summary>
        /// Whether to include device configuration snapshot (motion settings, feature enablement, etc.)
        /// in the event record. Useful for auditing feature changes over time.
        /// Default: true
        /// </summary>
        public bool IncludeDeviceConfig { get; set; } = true;

        /// <summary>
        /// Whether to include location information in the event record.
        /// Can be privacy-sensitive (reveals home address, coordinates).
        /// Default: false (for privacy)
        /// </summary>
        public bool IncludeLocationInfo { get; set; } = false;

        /// <summary>
        /// Whether to include account/user information in the event record.
        /// Can be privacy-sensitive (reveals email, name, phone).
        /// Default: false (for privacy)
        /// </summary>
        public bool IncludeAccountInfo { get; set; } = false;

        /// <summary>
        /// Whether to include recognized persons (face recognition results) in AI analysis.
        /// Can be privacy-sensitive (stores recognized person names and photo URLs).
        /// Default: false (for privacy)
        /// </summary>
        public bool IncludeRecognizedPersons { get; set; } = false;

        /// <summary>
        /// Whether to compute and include SHA256 hash of downloaded video file.
        /// Useful for integrity verification but adds CPU cost (hashing).
        /// Default: false (for performance)
        /// </summary>
        public bool ComputeFileHash { get; set; } = false;

        /// <summary>
        /// Whether to extract and include video codec, audio codec, resolution, fps from video file.
        /// Requires ffprobe/mediainfo executable. Can be slow for large files.
        /// Default: false (for performance)
        /// </summary>
        public bool ExtractVideoMetadata { get; set; } = false;

        /// <summary>
        /// Whether to pretty-print JSON (more readable, larger file size) or minify it.
        /// Default: true (for readability)
        /// </summary>
        public bool PrettyPrintJson { get; set; } = true;

        /// <summary>
        /// Application version string to embed in the download record.
        /// Helps track which version of Ring.Videos downloaded each file.
        /// </summary>
        public string ApplicationVersion { get; set; } = "1.0.0";

        /// <summary>
        /// Create a copy of options with default privacy-safe settings.
        /// Disables location, account, and recognized persons data.
        /// JSON sidecars written alongside video files.
        /// </summary>
        public static EventRecordingOptions CreatePrivacySafe()
        {
            return new EventRecordingOptions
            {
                WriteEventRecords = true,
                IncludeDeviceConfig = true,
                IncludeLocationInfo = false,
                IncludeAccountInfo = false,
                IncludeRecognizedPersons = false,
                ComputeFileHash = false,
                ExtractVideoMetadata = false,
                PrettyPrintJson = true
            };
        }

        /// <summary>
        /// Create a copy of options with comprehensive audit trail settings.
        /// Includes all available data for complete event documentation.
        /// WARNING: Privacy-sensitive - includes location, account, and person data.
        /// JSON sidecars written alongside video files.
        /// </summary>
        public static EventRecordingOptions CreateAuditTrail()
        {
            return new EventRecordingOptions
            {
                WriteEventRecords = true,
                IncludeDeviceConfig = true,
                IncludeLocationInfo = true,
                IncludeAccountInfo = true,
                IncludeRecognizedPersons = true,
                ComputeFileHash = true,
                ExtractVideoMetadata = true,
                PrettyPrintJson = true
            };
        }

        /// <summary>
        /// Create a copy of options optimized for minimal file size.
        /// Disables optional data and uses minified JSON.
        /// JSON sidecars written alongside video files.
        /// </summary>
        public static EventRecordingOptions CreateMinimal()
        {
            return new EventRecordingOptions
            {
                WriteEventRecords = true,
                IncludeDeviceConfig = false,
                IncludeLocationInfo = false,
                IncludeAccountInfo = false,
                IncludeRecognizedPersons = false,
                ComputeFileHash = false,
                ExtractVideoMetadata = false,
                PrettyPrintJson = false
            };
        }
    }
}
