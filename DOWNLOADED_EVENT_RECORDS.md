# Downloaded Event Records - Complete Guide

## Overview

When you download Ring videos using Ring.Videos, the application can now automatically generate **DownloadedEventRecord** metadata files alongside each video. These are structured JSON files containing comprehensive information about the download, including device health, AI analysis results, device configuration, and file metadata.

**Benefits:**
- 📋 Audit trail: Complete record of what was downloaded and when
- 📊 Analytics: Track device health trends over time
- 🔍 Diagnostics: Correlate download issues with network/device state
- 🤖 AI Tracking: Store person detection and security analysis results
- 🔒 Compliance: Generate security event records for investigations
- ⚙️ Configurability: Control privacy/data exposure with simple options

## Quick Start

### Default Behavior (Privacy-Safe)

The application writes metadata files by default:

```
Videos/
├── Front_Door_2026-08-20_10-30-45.mp4
├── Front_Door_2026-08-20_10-30-45.metadata.json  ← Generated automatically
├── Front_Door_2026-08-20_11-15-22.mp4
└── Front_Door_2026-08-20_11-15-22.metadata.json
```

### Disable Metadata Files

In `appsettings.json` or application configuration:

```json
{
  "EventRecording": {
    "WriteEventRecords": false
  }
}
```

## DTO Structure

### DownloadedEventRecord (Root)

Complete record with all data:

```json
{
  "event": {
    "id": 987654321,
    "created_at": "2026-08-20T10:30:45Z",
    "kind": "motion",
    "answered": false,
    "favorite": false,
    "snapshot_url": "https://..."
  },
  "file": {
    "path": "/videos/Front_Door_2026-08-20_10-30-45.mp4",
    "filename": "Front_Door_2026-08-20_10-30-45.mp4",
    "size_bytes": 15728640,
    "duration_seconds": 30,
    "created_at": "2026-08-20T10:30:45Z",
    "last_modified": "2026-08-20T10:35:12Z",
    "sha256_hash": "a1b2c3d4...",
    "video_codec": "h264",
    "audio_codec": "aac",
    "resolution": "1920x1080",
    "frame_rate": "30fps"
  },
  "device": {
    "id": 123,
    "device_id": "aac123def456",
    "description": "Front Door",
    "kind": "doorbot",
    "type": "doorbot",
    "firmware_version": "1.8.31",
    "owned": true,
    "timezone": "America/New_York",
    "address": "123 Main Street",
    "latitude": 40.7128,
    "longitude": -74.0060
  },
  "device_health": {
    "connected": true,
    "battery_percentage": 87,
    "battery_percentage_category": "full",
    "battery_voltage": 4.1,
    "battery_present": true,
    "external_power_connected": false,
    "rssi": -45.5,
    "rssi_category": "good",
    "latest_signal_strength": -45,
    "latest_signal_category": "good",
    "average_signal_strength": -47,
    "average_signal_category": "good",
    "packet_loss": 0.5,
    "packet_loss_category": "good",
    "wifi_name": "MyHomeNetwork",
    "wifi_is_ring_network": false,
    "firmware_version": "1.8.31",
    "firmware_version_status": "up_to_date",
    "ota_status": "idle",
    "last_update_time": "2026-08-20T09:00:00Z"
  },
  "device_config": {
    "motion_detection_enabled": true,
    "advanced_motion_enabled": true,
    "people_only_detection_enabled": false,
    "night_vision_enabled": true,
    "show_recordings_enabled": true,
    "shadow_correction_enabled": true,
    "motion_message_enabled": true,
    "subscribed_to_notifications": true,
    "subscribed_to_motions": true
  },
  "device_location": null,
  "account": null,
  "ai_analysis": {
    "person_detected": true,
    "detection_type": "human",
    "detection_types": ["human"],
    "confidence_score": 0.85,
    "anomaly_score": null,
    "stream_quality_broken": false,
    "full_description": "Person detected at front door, 85% confidence",
    "short_description": "Person at door",
    "recognized_persons": null,
    "security_alerts": null,
    "motion_zones": [
      {
        "id": "zone_1",
        "name": "Porch",
        "confidence": 0.92
      }
    ],
    "tags": ["visitor"]
  },
  "download": {
    "download_start": "2026-08-20T10:30:45Z",
    "download_end": "2026-08-20T10:30:50Z",
    "duration_seconds": 5,
    "success": true,
    "attempts": 1,
    "retry_count": 0,
    "error_message": null,
    "downloaded_by_version": "1.0.0",
    "application": "Ring.Videos"
  }
}
```

## Configuration Options

### EventRecordingOptions

All options in `Ring.Videos/EventRecordingOptions.cs`:

| Option | Type | Default | Purpose |
|--------|------|---------|---------|
| `WriteEventRecords` | bool | `true` | Enable/disable metadata file generation |
| `IncludeDeviceConfig` | bool | `true` | Include device settings (motions enabled, etc.) |
| `IncludeLocationInfo` | bool | `false` | Include location address & coordinates |
| `IncludeAccountInfo` | bool | `false` | Include account email & name |
| `IncludeRecognizedPersons` | bool | `false` | Include face recognition results (names) |
| `ComputeFileHash` | bool | `false` | Compute SHA256 hash of video file |
| `ExtractVideoMetadata` | bool | `false` | Extract codec, resolution, fps (requires ffprobe) |
| `MetadataOutputDirectory` | string | `null` | Custom output directory (null = same as video) |
| `PrettyPrintJson` | bool | `true` | Format JSON with indentation |
| `ApplicationVersion` | string | `"1.0.0"` | Version string to embed in record |
| `MetadataFilenamePattern` | string | `"{filename}.metadata.json"` | Filename template |

### Preset Configurations

```csharp
// Privacy-safe (default): No location, account, or person data
var options = EventRecordingOptions.CreatePrivacySafe();

// Audit trail: Complete data for investigations
var options = EventRecordingOptions.CreateAuditTrail();

// Minimal: Only essential metadata, minified JSON
var options = EventRecordingOptions.CreateMinimal();

// Custom
var options = new EventRecordingOptions
{
    WriteEventRecords = true,
    IncludeLocationInfo = true,
    IncludeAccountInfo = false,
    ComputeFileHash = true
};
```

## Usage in Code

### Basic Usage

```csharp
// In Ring.Videos Program.cs setup:
var options = EventRecordingOptions.CreatePrivacySafe();
services.AddSingleton(options);
services.AddSingleton<EventMetadataWriter>();
services.AddSingleton<DownloadedEventRecordBuilder>();

// In download handler:
var builder = new DownloadedEventRecordBuilder(options);
var record = builder.Build(
    ringEvent: doorbotEvent,
    filePath: "/path/to/video.mp4",
    downloadStart: DateTime.UtcNow.AddSeconds(-5),
    downloadEnd: DateTime.UtcNow,
    session: ringSession  // Optional - for location/account data
);

var writer = new EventMetadataWriter(logger, options);
await writer.WriteEventRecordAsync(record, "/path/to/video.mp4");
```

### Configuration via appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "EventRecording": {
    "WriteEventRecords": true,
    "IncludeDeviceConfig": true,
    "IncludeLocationInfo": false,
    "IncludeAccountInfo": false,
    "IncludeRecognizedPersons": false,
    "ComputeFileHash": false,
    "ExtractVideoMetadata": false,
    "MetadataOutputDirectory": null,
    "PrettyPrintJson": true,
    "ApplicationVersion": "1.0.0",
    "MetadataFilenamePattern": "{filename}.metadata.json"
  }
}
```

### Binding Configuration

```csharp
// In Program.cs
var eventRecordingConfig = configuration.GetSection("EventRecording");
var options = eventRecordingConfig.Get<EventRecordingOptions>() 
    ?? EventRecordingOptions.CreatePrivacySafe();
services.AddSingleton(options);
```

## Privacy Considerations

### Default: Privacy-Safe ✅

By default, the following **sensitive data is NOT included**:
- ❌ Device location (address, coordinates)
- ❌ User account information (email, name, phone)
- ❌ Recognized person names (face recognition results)

**Included (low sensitivity):**
- ✅ Device health (battery, signal strength)
- ✅ Device configuration (feature enablement)
- ✅ AI detection type (object type, not identity)
- ✅ Event metadata (timestamp, type)
- ✅ File information (size, hash)

### Enabling Sensitive Data

If you need location/account data for compliance or audit purposes:

```csharp
var options = new EventRecordingOptions
{
    IncludeLocationInfo = true,    // Adds: address, coordinates
    IncludeAccountInfo = true,     // Adds: email, name, phone
    IncludeRecognizedPersons = true // Adds: recognized person names
};
```

**⚠️ Warning**: This data should be protected appropriately (encryption, access control, secure storage).

## Use Cases

### 1. Security Audit Trail

Track every download with full device/network context:

```python
import json
import pandas as pd

records = []
for file in os.glob("**/*.metadata.json", recursive=True):
    with open(file) as f:
        records.append(json.load(f))

df = pd.DataFrame([{
    'timestamp': r['event']['created_at'],
    'camera': r['device']['description'],
    'person_detected': r['ai_analysis']['person_detected'],
    'battery': r['device_health']['battery_percentage'],
    'signal': r['device_health']['rssi']
} for r in records])

print(df)
```

### 2. Download Failure Diagnostics

Correlate failures with device state:

```python
failed = [r for r in records if not r['download']['success']]
for rec in failed:
    print(f"Failed: {rec['device']['description']} - "
          f"Signal: {rec['device_health']['rssi']} - "
          f"Error: {rec['download']['error_message']}")
```

### 3. Device Health Trends

Analyze battery and signal over time:

```python
df['date'] = pd.to_datetime(df['timestamp']).dt.date
health_by_date = df.groupby('date')[['battery', 'signal']].mean()
health_by_date.plot()
```

### 4. AI Detection Analysis

Find high-confidence person detections:

```python
detections = [r for r in records 
    if r['ai_analysis']['person_detected'] and 
       r['ai_analysis']['confidence_score'] > 0.9]
    
print(f"Found {len(detections)} high-confidence person detections")
```

## Data Flow

```
Ring API Event
    ↓
DoorbotHistoryEvent (from API)
    ↓
DownloadedEventRecordBuilder.Build()
    ├─ Event metadata
    ├─ Device snapshot
    ├─ Device health
    ├─ Device config (if enabled)
    ├─ Location (if enabled + session provided)
    ├─ Account (if enabled + session provided)
    ├─ AI analysis
    └─ Download info (timing, success, etc.)
    ↓
DownloadedEventRecord (DTO object)
    ↓
EventMetadataWriter.WriteEventRecordAsync()
    ↓
{filename}.metadata.json (JSON file)
```

## File Format Details

### Filename Pattern

Default: `{filename}.metadata.json`

Supported placeholders:
- `{filename}` - Video filename without extension
- `{timestamp}` - ISO8601 timestamp when file was written
- `{event_id}` - Ring event ID (would need to pass explicitly)

Examples:
- `video_2026-08-20.mp4` → `video_2026-08-20.metadata.json`
- With `{timestamp}` pattern → `video_2026-08-20_2026-08-20T10-35-12Z.metadata.json`

### JSON Serialization

**Default (PrettyPrintJson: true):**
```json
{
  "event": {
    "id": 123
  }
}
```

**Minified (PrettyPrintJson: false):**
```json
{"event":{"id":123}}
```

## File Structure

### Metadata Directory Organization

**Option 1: Same Directory as Video (Default)**
```
Videos/
├── Front_Door_2026-08-20_10-30.mp4
├── Front_Door_2026-08-20_10-30.metadata.json
└── Front_Door_2026-08-20_11-15.mp4
```

**Option 2: Subdirectory**
```json
{
  "MetadataOutputDirectory": "metadata"
}
```

Results in:
```
Videos/
├── Front_Door_2026-08-20_10-30.mp4
├── metadata/
│   └── Front_Door_2026-08-20_10-30.metadata.json
```

**Option 3: Custom Absolute Path**
```json
{
  "MetadataOutputDirectory": "C:\\Audit\\Ring\\Metadata"
}
```

## Performance Considerations

### File Size Impact

Per event (approximate):
- Minimal: ~1-2 KB
- Standard: ~5-10 KB  
- Audit trail: ~15-20 KB
- With file hash: +marginal (already hashed)
- With video metadata: +1-2 KB

100 events:
- Minimal: ~100 KB
- Standard: ~500-1000 KB
- Audit trail: ~1.5-2 MB

### Performance Cost

- JSON serialization: <1ms per record
- File I/O: 1-5ms per file (depending on disk)
- File hashing: 100-500ms per video (depends on size)
- Video metadata extraction: 500-2000ms per video (requires ffprobe)

**Recommendation**: Disable `ComputeFileHash` and `ExtractVideoMetadata` unless needed.

## Troubleshooting

### Files Not Being Written

**Check:**
1. `WriteEventRecords` = `true` in options
2. Video download succeeded (records only written on success)
3. Disk space available
4. Write permissions to output directory

**Verify:**
```csharp
var options = new EventRecordingOptions();
Debug.WriteLine($"WriteEventRecords: {options.WriteEventRecords}");
```

### JSON Parse Errors

**Cause:** JSON pretty-printing not working in minified mode
**Solution:** Ensure `PrettyPrintJson` is consistent with how you're reading files

### Size Issues

**Large files?** Disable optional features:
```csharp
var options = EventRecordingOptions.CreateMinimal();
// Disables: device config, location, account, person names, hashes, video metadata
```

## Integration with Existing Systems

### Elasticsearch/Splunk Ingestion

```python
import json
import requests

for metadata_file in glob.glob("**/*.metadata.json"):
    with open(metadata_file) as f:
        record = json.load(f)
        # Send to Elasticsearch
        requests.post("http://es:9200/ring-videos/_doc", json=record)
```

### Database Storage

```sql
INSERT INTO downloads (
    event_id, camera_name, download_time, 
    battery_percent, signal_strength, person_detected, success
) VALUES (
    ?, ?, ?, ?, ?, ?, ?
)
```

### Webhook Notifications

```python
if record['download']['success'] and record['ai_analysis']['person_detected']:
    # Send to webhook for person detection
    requests.post("https://your-webhook.com/", json={
        'camera': record['device']['description'],
        'person': True,
        'confidence': record['ai_analysis']['confidence_score'],
        'timestamp': record['event']['created_at']
    })
```

## Future Enhancements

Planned additions:
- Video codec/resolution/fps extraction (ffprobe integration)
- Thumbnail extraction from snapshot URL
- Encryption support for sensitive data
- Database schema generation from DTO
- Streaming telemetry to cloud service
- Real-time webhook notifications
