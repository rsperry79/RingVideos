namespace VideoForensics.Providers.Common.Contracts
{
    /// <summary>Platform-agnostic media download interface</summary>
    public interface IMediaDownloadService
    {
        /// <summary>Downloads videos for a device within a date range</summary>
        Task<DownloadResult> DownloadVideosAsync(
            string deviceId,
            string outputPath,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default
        );

        /// <summary>Downloads snapshots for a device within a date range</summary>
        Task<DownloadResult> DownloadSnapshotsAsync(
            string deviceId,
            string outputPath,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default
        );

        /// <summary>Gets current download status</summary>
        DownloadStatus GetStatus();
    }

    /// <summary>Result of a download operation</summary>
    public record DownloadResult(
        bool Success,
        int FilesDownloaded = 0,
        long BytesDownloaded = 0,
        string? ErrorMessage = null
    );

    /// <summary>Current download status</summary>
    public record DownloadStatus(
        bool IsDownloading,
        int FilesCompleted,
        int FilesTotal,
        long BytesDownloaded,
        string? CurrentFile = null
    );
}
