using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Ring.Api;
using Ring.Api.Entities;

namespace Ring.Videos
{
    /// <summary>
    /// Builds a DownloadedEventRecord from Ring event and file data.
    /// Respects EventRecordingOptions to include/exclude sensitive data.
    /// </summary>
    public class DownloadedEventRecordBuilder
    {
        private readonly EventRecordingOptions _options;

        public DownloadedEventRecordBuilder(EventRecordingOptions options = null)
        {
            _options = options ?? EventRecordingOptions.CreatePrivacySafe();
        }

        /// <summary>
        /// Build a complete DownloadedEventRecord for a downloaded event.
        /// </summary>
        public DownloadedEventRecord Build(
            DoorbotHistoryEvent ringEvent,
            string filePath,
            DateTime downloadStart,
            DateTime downloadEnd,
            Ring.Api.Session session = null,
            bool success = true,
            int attempts = 1,
            int retryCount = 0,
            string errorMessage = null)
        {
            var fileInfo = new FileInfo(filePath);

            var record = new DownloadedEventRecord
            {
                Event = BuildEventMetadata(ringEvent),
                File = BuildFileInfo(fileInfo),
                Device = BuildDeviceSnapshot(ringEvent.Doorbot),
                DeviceHealth = BuildDeviceHealthSnapshot(ringEvent.Doorbot?.Health),
                Download = BuildDownloadInfo(downloadStart, downloadEnd, success, attempts, retryCount, errorMessage)
            };

            if (_options.IncludeDeviceConfig)
            {
                record.DeviceConfig = BuildDeviceConfigSnapshot(ringEvent.Doorbot);
            }

            if (_options.IncludeLocationInfo && session != null)
            {
                record.Location = BuildLocationSnapshot(ringEvent.Doorbot?.LocationId, session);
            }

            if (_options.IncludeAccountInfo && session != null)
            {
                record.Account = BuildAccountSnapshot(session);
            }

            if (ringEvent.CvProperties != null)
            {
                record.AiAnalysis = BuildAiAnalysisSnapshot(ringEvent.CvProperties, _options);
            }

            return record;
        }

        private DownloadEventMetadata BuildEventMetadata(DoorbotHistoryEvent evt)
        {
            return new DownloadEventMetadata
            {
                Id = evt.Id,
                CreatedAt = evt.CreatedAtDateTime,
                Kind = evt.Kind,
                Answered = evt.Answered,
                Favorite = evt.Favorite,
                SnapshotUrl = evt.SnapshotUrl,
                RecordingStatus = evt.Recording?.Status
            };
        }

        private DownloadedFileInfo BuildFileInfo(FileInfo fileInfo)
        {
            var info = new DownloadedFileInfo
            {
                Path = fileInfo.FullName,
                Filename = fileInfo.Name,
                SizeBytes = fileInfo.Length,
                CreatedAt = fileInfo.CreationTime,
                LastModified = fileInfo.LastWriteTime
            };

            if (_options.ComputeFileHash)
            {
                info.Sha256Hash = ComputeSha256(fileInfo.FullName);
            }

            if (_options.ExtractVideoMetadata)
            {
                ExtractVideoMetadata(fileInfo.FullName, info);
            }

            return info;
        }

        private DeviceSnapshot BuildDeviceSnapshot(Doorbot device)
        {
            if (device == null)
                return null;

            return new DeviceSnapshot
            {
                Id = device.Id,
                DeviceId = device.DeviceId,
                Description = device.Description,
                Kind = device.Kind,
                Type = device.Type,
                FirmwareVersion = device.FirmwareVersion,
                Owned = device.Owned,
                Timezone = device.TimeZone,
                Address = device.Address,
                Latitude = device.Latitude,
                Longitude = device.Longitude
            };
        }

        private DeviceHealthSnapshot BuildDeviceHealthSnapshot(DeviceHealth health)
        {
            if (health == null)
                return null;

            return new DeviceHealthSnapshot
            {
                Connected = health.Connected,
                BatteryPercentage = health.BatteryPercentage,
                BatteryPercentageCategory = health.BatteryPercentageCategory,
                BatteryVoltage = health.BatteryVoltage,
                BatteryVoltageCategory = health.BatteryVoltageCategory,
                BatteryPresent = health.BatteryPresent,
                ExternalPowerConnected = health.ExtPowerState == 1,
                Rssi = health.Rssi,
                RssiCategory = health.RssiCategory,
                LatestSignalStrength = health.LatestSignalStrength,
                LatestSignalCategory = health.LatestSignalCategory,
                AverageSignalStrength = health.AverageSignalStrength,
                AverageSignalCategory = health.AverageSignalCategory,
                PacketLoss = health.PacketLoss,
                PacketLossCategory = health.PacketLossCategory,
                WifiName = health.WifiName,
                WifiIsRingNetwork = health.WifiIsRingNetwork,
                FirmwareVersion = health.FirmwareVersion,
                FirmwareVersionStatus = health.FirmwareVersionStatus,
                OtaStatus = health.OtaStatus,
                LastUpdateTime = health.LastUpdateTime.HasValue ?
                    UnixTimeStampToDateTime(health.LastUpdateTime.Value) : (DateTime?)null
            };
        }

        private DeviceConfigSnapshot BuildDeviceConfigSnapshot(Doorbot device)
        {
            if (device?.Features == null)
                return null;

            return new DeviceConfigSnapshot
            {
                MotionDetectionEnabled = device.Features.MotionsEnabled,
                AdvancedMotionEnabled = device.Features.AdvancedMotionEnabled,
                PeopleOnlyDetectionEnabled = device.Features.PeopleOnlyEnabled,
                NightVisionEnabled = device.Features.NightVisionEnabled,
                ShowRecordingsEnabled = device.Features.ShowRecordings,
                ShadowCorrectionEnabled = device.Features.ShadowCorrectionEnabled,
                MotionMessageEnabled = device.Features.MotionMessageEnabled,
                SubscribedToNotifications = device.Subscribed,
                SubscribedToMotions = device.SubscribedMotions
            };
        }

        private LocationSnapshot BuildLocationSnapshot(Guid? locationId, Ring.Api.Session session)
        {
            if (!locationId.HasValue || session == null)
                return null;

            // Note: In a real implementation, this would fetch from session
            // For now, return a basic snapshot - the session would need to cache locations
            return new LocationSnapshot
            {
                Id = locationId,
                Name = "Location" // Would need to look this up
            };
        }

        private AccountSnapshot BuildAccountSnapshot(Ring.Api.Session session)
        {
            // Note: Would need to get profile from session
            return new AccountSnapshot
            {
                Email = "email@example.com" // Would need to get from session profile
            };
        }

        private AiAnalysisSnapshot BuildAiAnalysisSnapshot(CvProperties cv, EventRecordingOptions options)
        {
            if (cv == null)
                return null;

            var snapshot = new AiAnalysisSnapshot
            {
                PersonDetected = cv.PersonDetected,
                DetectionType = cv.DetectionType,
                ConfidenceScore = cv.Similarity,
                AnomalyScore = cv.Anomaly,
                StreamQualityBroken = cv.StreamBroken,
                FullDescription = cv.FullDescription,
                ShortDescription = cv.ShortDescription,
                Tags = cv.Tags
            };

            if (cv.DetectionTypes?.Count > 0)
            {
                snapshot.DetectionTypes = cv.DetectionTypes.Select(dt => dt.DetectionType).ToList();
            }

            if (options.IncludeRecognizedPersons && cv.Profiles?.Count > 0)
            {
                snapshot.RecognizedPersons = cv.Profiles.Select(p => new RecognizedPersonSnapshot
                {
                    Id = p.Id,
                    Name = p.Name,
                    Confidence = p.Confidence,
                    ThumbnailUrl = p.ThumbnailUrl
                }).ToList();
            }

            if (cv.SecurityAlerts?.Alerts?.Count > 0)
            {
                snapshot.SecurityAlerts = cv.SecurityAlerts.Alerts;
            }

            if (cv.DetectionDetails?.Zones?.Count > 0)
            {
                snapshot.MotionZones = cv.DetectionDetails.Zones.Select(z => new MotionZoneSnapshot
                {
                    Id = z.Id,
                    Name = z.Name,
                    Confidence = z.Confidence
                }).ToList();
            }

            return snapshot;
        }

        private DownloadProcessingInfo BuildDownloadInfo(
            DateTime start, DateTime end, bool success, int attempts, int retryCount, string errorMessage)
        {
            return new DownloadProcessingInfo
            {
                DownloadStart = start,
                DownloadEnd = end,
                DurationSeconds = (int)Math.Round((end - start).TotalSeconds),
                Success = success,
                Attempts = attempts,
                RetryCount = retryCount,
                ErrorMessage = errorMessage,
                DownloadedByVersion = _options.ApplicationVersion,
                Application = "Ring.Videos"
            };
        }

        private string ComputeSha256(string filePath)
        {
            try
            {
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                using (var fileStream = File.OpenRead(filePath))
                {
                    byte[] hash = sha256.ComputeHash(fileStream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
            catch
            {
                return null;
            }
        }

        private void ExtractVideoMetadata(string filePath, DownloadedFileInfo info)
        {
            // TODO: Use ffprobe or mediainfo to extract:
            // - video codec
            // - audio codec
            // - resolution
            // - frame rate
            // For now, this is a placeholder
        }

        private DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }
    }
}
