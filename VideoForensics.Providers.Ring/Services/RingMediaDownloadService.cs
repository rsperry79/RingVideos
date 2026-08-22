using Microsoft.Extensions.Logging;
using VideoForensics.Providers.Common.Contracts;

namespace VideoForensics.Providers.Ring.Services
{
    public class RingMediaDownloadService : IMediaDownloadService
    {
        private readonly ILogger _logger;
        private readonly Session _session;
        private DownloadStatus _currentStatus = new(IsDownloading: false, FilesCompleted: 0, FilesTotal: 0, BytesDownloaded: 0);

        public RingMediaDownloadService(ILogger logger, Session session)
        {
            _logger = logger;
            _session = session;
        }

        public async Task<DownloadResult> DownloadVideosAsync(string deviceId, string outputPath, DateTime startDate,
            DateTime endDate, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Downloading videos for device {DeviceId} from {StartDate} to {EndDate}",
                    deviceId, startDate, endDate);

                Directory.CreateDirectory(outputPath);

                var events = await _session.GetDoorbotsHistory(startDate, endDate);
                var relevantEvents = events?.Where(e => e.Doorbot?.Id.ToString() == deviceId).ToList() ?? new List<Entities.DoorbotHistoryEvent>();

                var downloadedFiles = 0;
                var downloadedBytes = 0L;

                _currentStatus = _currentStatus with { IsDownloading = true, FilesTotal = relevantEvents.Count };

                foreach (var @event in relevantEvents)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        var fileName = Path.Combine(outputPath,
                            $"{deviceId}_{@event.CreatedAtDateTime:yyyyMMdd_HHmmss}.mp4");

                        await _session.GetDoorbotHistoryRecording(@event, fileName);

                        if (File.Exists(fileName))
                        {
                            var downloadedSize = new FileInfo(fileName).Length;
                            downloadedFiles++;
                            downloadedBytes += downloadedSize;

                            _currentStatus = _currentStatus with
                            {
                                FilesCompleted = downloadedFiles,
                                BytesDownloaded = downloadedBytes,
                                CurrentFile = fileName
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to download video for event {EventId}", @event.Id);
                    }
                }

                _currentStatus = _currentStatus with { IsDownloading = false };

                _logger.LogInformation("Downloaded {FileCount} videos ({Bytes} bytes) for device {DeviceId}",
                    downloadedFiles, downloadedBytes, deviceId);

                return new DownloadResult(
                    Success: true,
                    FilesDownloaded: downloadedFiles,
                    BytesDownloaded: downloadedBytes
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading videos for device {DeviceId}", deviceId);
                _currentStatus = _currentStatus with { IsDownloading = false };
                return new DownloadResult(
                    Success: false,
                    ErrorMessage: $"Download failed: {ex.Message}"
                );
            }
        }

        public async Task<DownloadResult> DownloadSnapshotsAsync(string deviceId, string outputPath, DateTime startDate,
            DateTime endDate, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Downloading snapshots for device {DeviceId} from {StartDate} to {EndDate}",
                    deviceId, startDate, endDate);

                Directory.CreateDirectory(outputPath);

                var events = await _session.GetDoorbotsHistory(startDate, endDate);
                var relevantEvents = events?.Where(e => e.Doorbot?.Id.ToString() == deviceId).ToList() ?? new List<Entities.DoorbotHistoryEvent>();

                var downloadedFiles = 0;
                var downloadedBytes = 0L;

                _currentStatus = _currentStatus with { IsDownloading = true, FilesTotal = relevantEvents.Count };

                foreach (var @event in relevantEvents)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        if (@event.SnapshotUrl != null)
                        {
                            var fileName = Path.Combine(outputPath,
                                $"{deviceId}_{@event.CreatedAtDateTime:yyyyMMdd_HHmmss}.jpg");

                            await DownloadFileAsync(@event.SnapshotUrl, fileName, cancellationToken);

                            if (File.Exists(fileName))
                            {
                                var downloadedSize = new FileInfo(fileName).Length;
                                downloadedFiles++;
                                downloadedBytes += downloadedSize;

                                _currentStatus = _currentStatus with
                                {
                                    FilesCompleted = downloadedFiles,
                                    BytesDownloaded = downloadedBytes,
                                    CurrentFile = fileName
                                };
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to download snapshot for event {EventId}", @event.Id);
                    }
                }

                _currentStatus = _currentStatus with { IsDownloading = false };

                _logger.LogInformation("Downloaded {FileCount} snapshots ({Bytes} bytes) for device {DeviceId}",
                    downloadedFiles, downloadedBytes, deviceId);

                return new DownloadResult(
                    Success: true,
                    FilesDownloaded: downloadedFiles,
                    BytesDownloaded: downloadedBytes
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading snapshots for device {DeviceId}", deviceId);
                _currentStatus = _currentStatus with { IsDownloading = false };
                return new DownloadResult(
                    Success: false,
                    ErrorMessage: $"Download failed: {ex.Message}"
                );
            }
        }

        public DownloadStatus GetStatus() => _currentStatus;

        private async Task DownloadFileAsync(string url, string filePath, CancellationToken cancellationToken)
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            using (var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken))
            using (var fileStream = File.Create(filePath))
            {
                await contentStream.CopyToAsync(fileStream, cancellationToken);
            }
        }
    }
}
