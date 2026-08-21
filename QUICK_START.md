# Quick Start Guide: Ring Metadata & Evidence Extraction

## Installation

The libraries are already integrated into the Ring.Api solution:

```bash
dotnet build external/RingApi/src/Ring.Api.sln
```

## Basic Usage

### 1. Extract Video Metadata

```csharp
using Ring.Api.Video.Metadata;
using Ring.Api.Entities;

// From a Ring event
var extractor = new MetadataExtractor();
var metadata = extractor.ExtractMetadata(doorbotHistoryEvent);

// Access extracted data
Console.WriteLine($"Device: {metadata.DeviceName}");
Console.WriteLine($"Location: {metadata.Address}");
Console.WriteLine($"Person Detected: {metadata.PersonDetected}");
Console.WriteLine($"Confidence: {metadata.DetectionConfidence}%");
```

### 2. Extract Snapshot Metadata

```csharp
using Ring.Api.Snapshots.Metadata;

// From a Ring event
var extractor = new SnapshotMetadataExtractor();
var metadata = extractor.ExtractMetadata(doorbotHistoryEvent);

// Access snapshot-specific data
Console.WriteLine($"Image Format: {metadata.ImageFormat}");
Console.WriteLine($"Dimensions: {metadata.ImageDimensions}");
Console.WriteLine($"Quality Score: {metadata.ImageQualityScore}");
```

### 3. Extract Video Frames at Detection Moments

```csharp
using Ring.Api.Video.Metadata;

var frameExtractor = new VideoFrameExtractor();

// Extract frames at verified detection timestamps
var frames = frameExtractor.ExtractDetectionFrames(
    videoPath: "/path/to/video.mp4",
    metadata: videoMetadata,
    outputDirectory: "/evidence/frames"
);

// Each frame includes metadata
foreach (var frame in frames)
{
    Console.WriteLine($"Frame at {frame.TimeFormatted}");
    Console.WriteLine($"  Detection: {frame.DetectionType}");
    Console.WriteLine($"  Confidence: {frame.DetectionConfidence}");
    
    if (frame.RecognizedProfiles?.Count > 0)
    {
        foreach (var profile in frame.RecognizedProfiles)
        {
            Console.WriteLine($"  👤 {profile.Name}");
        }
    }
}
```

### 4. Download & Process Snapshots

```csharp
using Ring.Api.Snapshots.Metadata;

var snapshotExtractor = new SnapshotFrameExtractor();

// Download snapshot from Ring URL
var snapshot = snapshotExtractor.DownloadAndTagSnapshot(
    snapshotUrl: "https://...",
    metadata: snapshotMetadata,
    outputDirectory: "/evidence/snapshots"
);

if (snapshot?.ProcessingSuccessful ?? false)
{
    // Generate evidence summary
    var summaryPath = snapshotExtractor.GenerateEvidenceSummary(
        snapshot,
        snapshotMetadata,
        "/evidence/snapshots"
    );
    
    Console.WriteLine($"Summary: {summaryPath}");
}
```

### 5. Save Snapshot as Video Thumbnail

```csharp
using Ring.Api.Video.Metadata;

var thumbExtractor = new VideoThumbnailExtractor();

var thumbnail = thumbExtractor.ExtractAndSaveThumbnail(
    snapshotUrl: ringEvent.SnapshotUrl,
    videoMetadata: videoMetadata,
    videoFilePath: "/path/to/video.mp4",
    outputDirectory: "/thumbnails"
);
```

### 6. Write Metadata to Files

```csharp
// Write to video
var videoWriter = new NoOpMetadataWriter();
var result = videoWriter.WriteMetadata(videoMetadata, videoPath);

Console.WriteLine($"Status: {result.Status}");      // Valid, Corrected, Corrupt, Failed
Console.WriteLine($"IsValid: {result.IsValid}");
Console.WriteLine($"WasWritten: {result.WasWritten}");

// Write to snapshot
var imageWriter = new ImageMetadataWriter();
var imageResult = imageWriter.WriteMetadata(snapshotMetadata, imagePath);
```

### 7. Validate Files

```csharp
var validator = new ImageMetadataValidator();

if (validator.IsValid("/path/to/image.jpg"))
{
    Console.WriteLine("✅ Image is valid");
}
else
{
    Console.WriteLine("❌ Image is corrupt");
}
```

## Configuration & Privacy

### Default Configuration
```csharp
var options = VideoProcessingOptions.CreateDefault();
// Includes all data: GPS, device info, detection data
```

### Privacy-Focused Configuration
```csharp
var options = VideoProcessingOptions.CreatePrivacyFocused();
// Excludes GPS and address, keeps detection/perpetrator data

var extractor = new MetadataExtractor(options);
```

### Minimal Configuration
```csharp
var options = VideoProcessingOptions.CreateMinimal();
// Essential data only

var extractor = new MetadataExtractor(options);
```

### Custom Configuration
```csharp
var options = new VideoProcessingOptions
{
    IncludeGps = true,                  // Include GPS coordinates
    IncludeAddress = true,              // Include street address
    IncludeDeviceHealth = true,         // Include signal/battery
    IncludeAiAnalysis = true,           // Include detection data
    PhotoPrismCompatibility = true,     // Add PhotoPrism tags
    ExtractMetadata = true,             // Enable extraction
    WriteExif = true,                   // Enable EXIF writing
    ValidateImages = true,              // Enable validation
    AutoCorrect = true                  // Auto-correct issues
};

var extractor = new MetadataExtractor(options);
```

## Common Patterns

### Complete Evidence Workflow

```csharp
// Extract video metadata
var videoExtractor = new MetadataExtractor();
var videoMetadata = videoExtractor.ExtractMetadata(ringEvent);

// Extract snapshot metadata
var snapshotExtractor = new SnapshotMetadataExtractor();
var snapshotMetadata = snapshotExtractor.ExtractMetadata(ringEvent);

// Download snapshot and save as thumbnail
var thumbExtractor = new VideoThumbnailExtractor();
thumbExtractor.ExtractAndSaveThumbnail(
    ringEvent.SnapshotUrl,
    videoMetadata,
    videoPath,
    outputDir
);

// Extract video frames at detection times
var frameExtractor = new VideoFrameExtractor();
var frames = frameExtractor.ExtractDetectionFrames(
    videoPath,
    videoMetadata,
    outputDir
);

// Write metadata to files
var videoWriter = new NoOpMetadataWriter();
var videoResult = videoWriter.WriteMetadata(videoMetadata, videoPath);

var imageWriter = new ImageMetadataWriter();
var snapshotResult = imageWriter.WriteMetadata(snapshotMetadata, snapshotPath);

// Validate integrity
var validator = new ImageMetadataValidator();
if (!validator.IsValid(snapshotPath))
{
    Console.WriteLine("⚠️ Evidence integrity issue detected");
}
```

### Process Multiple Events

```csharp
var videoExtractor = new MetadataExtractor();
var snapshotExtractor = new SnapshotFrameExtractor();
var frameExtractor = new VideoFrameExtractor();

foreach (var ringEvent in ringEvents)
{
    // Extract metadata
    var metadata = videoExtractor.ExtractMetadata(ringEvent);
    
    // Download snapshot
    var snapshot = snapshotExtractor.DownloadAndTagSnapshot(
        ringEvent.SnapshotUrl,
        metadata,
        outputDir
    );
    
    // Extract frames
    var frames = frameExtractor.ExtractDetectionFrames(
        videoPath,
        metadata,
        outputDir
    );
    
    Console.WriteLine($"Event {ringEvent.Id}: {frames.Count} frames extracted");
}
```

### Error Handling

```csharp
var frameExtractor = new VideoFrameExtractor();
var frames = frameExtractor.ExtractDetectionFrames(videoPath, metadata, outputDir);

foreach (var frame in frames)
{
    if (!frame.ExtractionSuccessful)
    {
        Console.WriteLine($"⚠️ Frame extraction failed at {frame.TimeFormatted}");
        Console.WriteLine($"   Error: {frame.ExtractionError}");
        
        // Log error, continue with other frames
        continue;
    }
    
    // Process successful frame
    Console.WriteLine($"✅ Frame extracted: {frame.FrameFilePath}");
}
```

## Key Properties

### VideoMetadata
```csharp
// Location
metadata.Latitude, metadata.Longitude, metadata.Address

// Device
metadata.DeviceName, metadata.DeviceModel, metadata.DeviceManufacturer
metadata.DeviceFirmwareVersion, metadata.DeviceOnline

// Detection
metadata.PersonDetected, metadata.MotionDetected
metadata.DetectionType, metadata.DetectionConfidence

// Evidence
metadata.VerifiedDetectionTimestamps    // Critical for frame extraction
metadata.RecognizedProfiles              // Face recognition
metadata.AnomalyScore                    // Suspicious activity score
metadata.SecurityAlerts                  // Threat classification
metadata.StreamBroken                    // Tampering indicator
metadata.OwnerNotificationsEnabled       // Intent marker
```

### ExtractedFrame
```csharp
// Timing
frame.TimestampMs, frame.TimeFormatted   // HH:MM:SS.mmm

// File
frame.FrameFileName, frame.FrameFilePath, frame.FileSizeBytes

// Detection
frame.DetectionType, frame.DetectionConfidence, frame.AnomalyScore

// Evidence
frame.RecognizedProfiles                 // People identified
frame.SecurityAlerts                     // Threats detected
frame.ActiveZones                        // Motion locations

// Status
frame.ExtractionSuccessful, frame.ExtractionError
```

### ProcessedSnapshot
```csharp
// File
snapshot.FilePath, snapshot.FileName, snapshot.FileSizeBytes
snapshot.ImageFormat                     // JPEG, PNG, WebP, GIF

// Detection
snapshot.DetectionType, snapshot.DetectionConfidence
snapshot.AnomalyScore, snapshot.SecurityAlerts

// Identification
snapshot.RecognizedProfiles              // Recognized people

// Status
snapshot.ProcessingSuccessful, snapshot.ProcessingError
snapshot.EvidenceSummaryPath             // Generated summary
```

## Testing

### Run All Tests
```bash
dotnet test external/RingApi/src/video/metadata/tests/
dotnet test external/RingApi/src/snapshots/metadata/tests/
```

### Expected Results
```
Video Metadata Tests:    57 Passed
Snapshot Metadata Tests: 81 Passed
Total:                  138 Passed
```

## Dependencies

### Required
- .NET 10.0+
- System.IO.Abstractions v21.2.1
- MetadataExtractor v2.9.3

### Optional (for frame extraction)
- FFmpeg (for video frame extraction)
  - Windows: `choco install ffmpeg`
  - Linux: `apt-get install ffmpeg`
  - macOS: `brew install ffmpeg`

## References

- **Full DV Support Documentation**: See `DV_EVIDENCE_SUPPORT.md`
- **Frame Extraction Guide**: See `FRAME_EXTRACTION.md`
- **Snapshot Extraction Guide**: See `SNAPSHOT_FRAME_EXTRACTION.md`
- **Implementation Details**: See `IMPLEMENTATION_SUMMARY.md`

## Support

For issues or questions:
1. Check the comprehensive documentation files
2. Review test files for usage examples
3. Examine the models for available fields
4. Run tests to verify functionality

All code is well-documented with XML comments describing purpose, parameters, and return values.
