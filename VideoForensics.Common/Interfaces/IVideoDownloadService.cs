using System;
using System.Threading.Tasks;

namespace VideoForensics.Common.Interfaces
{
    public interface IVideoDownloadService
    {
        Task<bool> AuthenticateAsync(string username, string password);
        Task<bool> DownloadVideosAsync(string outputPath, DateTime startDate, DateTime endDate);
        Task<bool> DownloadSnapshotsAsync(string outputPath, DateTime startDate, DateTime endDate);
        string GetDownloadStatus();
    }
}
