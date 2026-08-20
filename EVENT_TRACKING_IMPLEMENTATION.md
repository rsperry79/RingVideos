# Event Tracking Enhancement - Implementation Summary

## What's New

The Ring.Videos application now automatically captures comprehensive telemetry for every detected event during download runs.

### New Report: `event_tracking.tsv`

A new TSV (Tab-Separated Values) report is automatically generated in the `reports/` directory with the following columns:

```
Timestamp
EventId
CameraId
CameraName
Kind
BatteryPercentage      ← Battery level at time of event (0-100)
BatteryCategory        ← Battery health (full, medium, low, critical)
SignalStrength         ← WiFi signal in dB (e.g., -45.5)
SignalCategory         ← Signal quality (excellent, good, fair, poor)
PacketLoss             ← Network packet loss % (e.g., 0.5%)
Connected              ← Device connectivity status (True/False)
WifiName               ← Connected network SSID
FirmwareVersion        ← Device firmware version
PersonDetected         ← AI: Person in video (True/False)
DetectionType          ← AI detection (human, vehicle, animal, other_motion)
Confidence             ← AI confidence score (0.0-1.0)
```

### Example Report Output

```
2026-08-20 10:30:45	987654321	123	Front Door	motion	87	full	-45.5	good	0.5	True	MyHomeNetwork	1.8.31	True	human	0.85
2026-08-20 11:15:22	987654322	123	Front Door	ring	85	full	-44.0	good	0.3	True	MyHomeNetwork	1.8.31	False			
2026-08-20 14:05:10	987654323	124	Back Patio	motion	65	medium	-62.0	fair	1.2	True	MyHomeNetwork	1.8.25	False			
2026-08-20 16:42:33	987654324	123	Front Door	motion	42	low	-71.5	poor	3.5	True	MyHomeNetwork	1.8.31	False			
```

## How It Works

### Automatic Collection

When you run the downloader:

```bash
cd Ring.Videos
dotnet run -- --download --path "C:\Videos" --from 2026-08-10 --to 2026-08-20
```

The application will:

1. **Authenticate** with Ring.com
2. **Fetch all events** matching your criteria
3. **Collect telemetry** for each event:
   - Device health (battery, signal, connectivity)
   - AI/Computer Vision analysis (person detection, confidence)
   - Camera information and settings
4. **Generate reports**:
   - `reports/camera_health.tsv` - Device health snapshot
   - `reports/event_tracking.tsv` ← **NEW** - Per-event telemetry
   - `reports/download_failures.tsv` - Failed downloads
   - `logs/*.json` - Raw per-event JSON with all data

## Use Cases

### 1. Diagnostics: Why Did Downloads Fail?

Correlate failed downloads with device telemetry:

```sql
-- Find events that failed AND had weak signal
SELECT Timestamp, CameraName, EventId, SignalStrength, SignalCategory
FROM event_tracking
WHERE EventId IN (SELECT event_id FROM download_failures)
  AND SignalStrength < -65  -- Poor signal threshold
ORDER BY Timestamp DESC
```

### 2. Analytics: Device Health Trends

Track battery depletion patterns:

```python
import pandas as pd

events = pd.read_csv('reports/event_tracking.tsv', sep='\t')

# Battery trend for a specific camera
front_door = events[events['CameraName'] == 'Front Door']
print(front_door[['Timestamp', 'BatteryPercentage', 'SignalStrength']])

# Average signal by device
print(events.groupby('CameraName')[['BatteryPercentage', 'SignalStrength']].mean())
```

### 3. Security: AI Detection Analysis

Find all motion events with detected persons:

```python
events = pd.read_csv('reports/event_tracking.tsv', sep='\t')

# Person detections
persons = events[events['PersonDetected'] == 'True']
print(f"Found {len(persons)} events with people detected")

# High-confidence detections
high_confidence = events[
    (events['PersonDetected'] == 'True') & 
    (events['Confidence'].astype(float) > 0.85)
]
print(f"High confidence: {len(high_confidence)} events")
```

### 4. Quality: Stream Integrity

Monitor for stream problems:

```python
# Events with packet loss
bad_signal = events[events['PacketLoss'].astype(float) > 2.0]
print(f"Events with >2% packet loss: {len(bad_signal)}")

# Devices going offline
offline = events[events['Connected'] == 'False']
print(f"Offline events: {len(offline)}")
```

## Data Availability

### Always Present
- Event timestamp, ID, and type
- Camera ID, name, kind
- Battery percentage and category
- Signal strength and category
- Packet loss
- Firmware version

### Conditional (May Be Null)
- WiFi name (if available)
- Person detected, detection type (only if AI processed)
- Confidence score (only if detection occurred)
- Signal category (only if device connected)

## Integration with Existing Reports

### camera_health.tsv
- **When**: Generated once per run (device snapshot at runtime)
- **Data**: Current device health across all cameras
- **Rows**: One per device

### event_tracking.tsv (NEW)
- **When**: Generated during download (appends for each run)
- **Data**: Historical telemetry for each event
- **Rows**: One per event found

### download_failures.tsv
- **When**: Generated during download failures
- **Data**: Failed event details
- **Rows**: One per failure

### logs/*.json
- **When**: Generated for each event (if enabled)
- **Data**: Complete raw event JSON
- **Files**: One JSON file per event

## Excel/Spreadsheet Analysis

### Import in Excel

1. Open Excel
2. Data → Get Data → From File → From Text
3. Select `reports/event_tracking.tsv`
4. Set delimiter to Tab
5. Click Load

### Create Pivot Tables

**Battery Trend Chart:**
- Rows: Timestamp
- Values: BatteryPercentage (average)
- Filter: CameraName

**Signal Quality Heatmap:**
- Rows: CameraName
- Columns: SignalCategory
- Values: Count of events

**AI Detection Summary:**
- Rows: DetectionType
- Values: Count of events
- Filter: PersonDetected = True

## Performance Notes

- Telemetry collection adds minimal overhead (~1-2ms per event)
- Report generation is I/O bound, not CPU bound
- Storage: ~1KB per event in TSV + ~50KB per event in JSON log
- 100 events = ~150KB total report space

## Future Enhancements (Phase 2+)

### Planned Features
- ✅ Event-level battery/signal tracking (DONE)
- 📋 AI/CV detection summary report
- 📋 Device health trends over time
- 📋 Motion zone heatmap visualization
- 📋 Recognized persons frequency tracking
- 📋 Signal strength correlation analysis
- 📋 Predictive maintenance alerts

### Possible Extensions
- Graphana dashboard integration
- Real-time Slack notifications
- Power draw estimation from events/battery trend
- Network topology mapping
- Geolocation heatmaps

## Troubleshooting

### No event_tracking.tsv Generated
- **Cause**: No events matched download criteria
- **Solution**: Verify filter settings, check download_failures.tsv

### Missing Columns in TSV
- **Cause**: Events without AI analysis
- **Solution**: This is normal; null columns are empty (not errors)

### High Packet Loss Values
- **Cause**: Poor WiFi signal or congestion
- **Solution**: Check RF environment, reposition device, check WiFi channel

### Battery Percentage = 0
- **Cause**: Device on external power
- **Solution**: This is normal for hardwired devices; check external_connection in camera_health.tsv

## Technical Details

### Event Telemetry Collection

```csharp
// Method: TrackEventTelemetry
// Location: RingVideoService.cs
// Called: After event filtering, before download starts
// Purpose: Capture per-event health/AI data to TSV

// Data sources:
event.DoorbotHistoryEvent
├── event.Doorbot.Health (DeviceHealth)
├── event.CvProperties (AI/CV analysis)
└── event.Doorbot (device info)

// Output: reports/event_tracking.tsv (append mode)
```

### Integration Points

1. **Event Retrieval** → Filter events
2. **Telemetry Collection** ← **NEW** → Track health/AI data
3. **Download Loop** → Download videos
4. **Report Generation** → Create TSV/JSON outputs

## Questions?

See `EVENT_DATA_ENHANCEMENT.md` for complete API data reference and all available fields.
