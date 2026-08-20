# Ring API Event Data Enhancement Guide

## Complete Data Available in Event JSON

### Event Level Data
From `DoorbotHistoryEvent`:
```
- id: Unique event identifier (long)
- created_at: Event timestamp (string)
- createdAtDateTime: Parsed DateTime
- answered: Whether ring was answered (bool)
- kind: Event type (motion, ring, snapshot, etc.)
- favorite: User favorited this event (bool)
- snapshot_url: URL to event snapshot (string)
- events: Raw event list (List<object>)
- recording: Recording details (DoorbotHistoryEventRecording)
- cv_properties: AI/Computer Vision analysis (CvProperties) ✅ NEW
```

### Device Level Data (from event.Doorbot)
```
Device Info:
- id: Device ID (int)
- description: Camera name (string)
- device_id: Device UUID (string)
- kind: Device type (doorbot, stickupcam, chime, etc.)
- type: Device type string (string)
- firmware_version: Firmware version (string)
- owned: Is owned by user (bool)
- address: Device location (string)
- latitude/longitude: GPS coordinates (double)

Power & Connectivity:
- battery_life: Battery level (int) - DEPRECATED
- external_connection: Has external power (bool)
- health: DeviceHealth object ✅ COMPLETE TELEMETRY

Device Features:
- features: DoorbotFeatures
  ├── motions_enabled (bool)
  ├── show_recordings (bool)
  ├── advanced_motion_enabled (bool)
  ├── people_only_enabled (bool)
  ├── shadow_correction_enabled (bool)
  ├── motion_message_enabled (bool)
  └── night_vision_enabled (bool)

Alerts & Notifications:
- alerts: DoorbotAlerts
  └── connection: Alert status (string)

Owner Info:
- owner: Owner object
```

### Complete Device Health Telemetry (event.Doorbot.Health)
```
Connectivity:
- connected: Device online (bool)
- wifi_name: Connected SSID (string)
- wifi_is_ring_network: Ring-hosted network (bool)

Battery & Power:
- battery_present: Battery installed (bool)
- battery_percentage: Battery % (int) ✅ REQUESTED
- battery_percentage_category: "full", "medium", "low", "critical" (string)
- battery_voltage: Voltage in volts (decimal)
- battery_voltage_category: Voltage health (string)
- ext_power_state: External power state (int)

Signal Strength:
- rssi: RSSI in dB (double) ✅ REQUESTED
- rssi_category: "excellent", "good", "fair", "poor" (string)
- latest_signal_strength: Latest reading (double) ✅ REQUESTED
- latest_signal_category: Category (string)
- average_signal_strength: Average reading (double) ✅ REQUESTED
- average_signal_category: Category (string)
- packet_loss: Packet loss % (double) ✅ REQUESTED
- packet_loss_category: Category (string)
- packet_loss_strength: Loss strength metric (double)

Firmware & Updates:
- firmware_version: FW version (string)
- firmware_version_status: Update status (string)
- ota_status: Over-the-air update status (string)
- firmware: Firmware info (string)

Last Update:
- last_update_time: Unix timestamp (long)
- updated_at: ISO timestamp (string)

Device Model Info:
- device_type: Device type string (string)
- id: Health ID (long)
- supported_rpc_commands: List of commands (List<string>)
- ptz_connected: PTZ camera connected (bool)
```

### Computer Vision / AI Analysis (event.CvProperties)
**This data indicates what Ring's AI detected in the video:**

Detection:
- person_detected: Person in frame (bool?)
- detection_type: "human", "vehicle", "animal", "other_motion" (string)
- detection_types: List of CvDetectionType[] with timestamps
  └── Each detection includes verified_timestamps (List<long>)
- detection_details: CvDetectionDetails
  ├── zones: List<CvZone> with detection confidence
  ├── confidence: Overall detection confidence (0.0-1.0)
  └── model_version: AI model version (string)

Quality & Validity:
- stream_broken: Video stream incomplete (bool?)
- anomaly: Anomaly score (0.0-1.0) - unusual activity detection

Descriptions & Tagging:
- full_description: "Person detected at front door" (string)
- short_description: Brief version (string)
- tags: User-applied tags (List<string>)

Face Recognition:
- profiles: Recognized persons (List<CvProfile>)
  └── Each profile:
      ├── id: Profile ID (string)
      ├── name: Person name (string)
      ├── confidence: Match confidence (0.0-1.0)
      └── thumbnail_url: Profile photo (string)

Security Analysis:
- security_alerts: CvSecurityAlerts
  ├── severity: Alert level (string)
  └── alerts: List of security alerts (List<string>)

Confidence:
- similarity: Confidence score (0.0-1.0)
```

### Recording Details (event.Recording)
```
- status: Recording status ("ready", "failed", etc.)
```

---

## Proposed Enhancements

### 1. Enhanced Event Tracking TSV (NEW)
Create `reports/event_tracking.tsv` with:

```
Timestamp | EventId | CameraId | CameraName | Kind | BatteryPercentage | BatteryCategory | SignalStrength | SignalCategory | PacketLoss | Connected | PersonDetected | DetectionType | Confidence | WifiName | Firmware
```

### 2. Device Health Per-Event JSON
Embed in each event's JSON:
```json
{
  "event": { ... },
  "device_health_at_event": {
    "battery_percentage": 87,
    "signal_strength": -45.5,
    "packet_loss": 0.5,
    "connected": true,
    "wifi_name": "MyNetwork"
  }
}
```

### 3. AI/CV Summary Report
New file: `reports/ai_detections_summary.tsv`

```
Timestamp | EventId | CameraId | CameraName | PersonDetected | DetectionType | Confidence | FullDescription | RecognizedPerson | PersonConfidence | SecurityAlerts
```

### 4. Extended Device Status at Download Time
Before download starts, capture and log:
- All device health metrics
- Feature enablement status
- Firmware version
- Location of device
- Owner information

---

## Implementation Roadmap

### Phase 1: Event Tracking Enhancement
- [ ] Add DeviceHealthSnapshot to SaveRecordingAsync
- [ ] Create event_tracking.tsv report
- [ ] Embed health data in per-event JSON
- [ ] Add health data to download failure reports

### Phase 2: AI/CV Reporting
- [ ] Extract CvProperties from events
- [ ] Create ai_detections_summary.tsv
- [ ] Add person detection stats to camera health
- [ ] Track confidence scores over time

### Phase 3: Advanced Analytics
- [ ] Battery depletion trends (events per charge)
- [ ] Signal strength correlation with failed downloads
- [ ] Motion zone heatmaps (from detection_details.zones)
- [ ] Recognized persons frequency
- [ ] Security alert tracking

---

## Data Examples

### Example: Event with Full Health & AI Data
```json
{
  "id": 987654321,
  "created_at": "2026-08-20T10:30:45Z",
  "kind": "motion",
  "answered": false,
  "favorite": false,
  "doorbot": {
    "id": 123,
    "description": "Front Door",
    "device_id": "aac123def456",
    "kind": "doorbot",
    "battery_life": 87,
    "firmware_version": "1.8.31",
    "health": {
      "connected": true,
      "battery_percentage": 87,
      "battery_percentage_category": "full",
      "battery_voltage": 4.1,
      "rssi": -45.5,
      "rssi_category": "good",
      "latest_signal_strength": -45,
      "average_signal_strength": -47,
      "packet_loss": 0.5,
      "wifi_name": "MyHomeNetwork",
      "connected_at": "2026-08-20T09:00:00Z"
    },
    "features": {
      "motions_enabled": true,
      "show_recordings": true,
      "advanced_motion_enabled": true,
      "people_only_enabled": false,
      "night_vision_enabled": true
    }
  },
  "cv_properties": {
    "person_detected": true,
    "detection_type": "human",
    "stream_broken": false,
    "full_description": "Person detected at front door, 85% confidence",
    "short_description": "Person at door",
    "similarity": 0.85,
    "detection_details": {
      "confidence": 0.85,
      "zones": [
        {
          "id": "zone_1",
          "name": "Porch",
          "confidence": 0.92
        }
      ]
    },
    "profiles": [
      {
        "id": "profile_456",
        "name": "John Doe",
        "confidence": 0.91,
        "thumbnail_url": "https://..."
      }
    ],
    "security_alerts": {
      "severity": "low",
      "alerts": ["unfamiliar_person"]
    }
  }
}
```

---

## API Methods for Additional Data

Currently Used:
- `GetAllDevices()` - Device list with health
- `GetRecordingsAsync()` - Events with CV properties
- `GenerateCameraHealthReport()` - Health snapshots

Available but Not Currently Used:
- `GetMonitoringStatus(locationId)` - Location-level monitoring
- `GetDetailedDeviceHealth(deviceId)` - More detailed health
- `GetDoorbotHealth(doorbotId)` - Device-specific health
- `GetEventSubscriptions()` - User notification settings
- `GetLocationEvents(locationId)` - Location event stream
- `GetMotionZones(doorbotId)` - Motion detection zones
- `GetDeviceSettings(doorbotId)` - Device configuration
- `GetLiveViewSession(doorbotId)` - Live view metadata

---

## Benefits of Enhancement

1. **Diagnostics**: Correlate download failures with signal strength
2. **Analytics**: Track camera health trends over time
3. **Security**: Identify recognized persons across recordings
4. **Quality**: Detect stream breaks and quality issues
5. **Automation**: Data for ML models predicting maintenance needs
6. **Reporting**: Professional security event reports with all context
