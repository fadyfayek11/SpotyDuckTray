# SpotyDuckTray

A lightweight Windows system tray application that automatically ducks (lowers) Spotify's volume when other audio sources are active, then smoothly restores it when they stop.

## 🎯 Features

- **Automatic Audio Ducking**: Intelligently detects when other applications are playing audio and automatically lowers Spotify's volume
- **Smooth Volume Transitions**: Professional-grade fade in/out effects for seamless audio management
- **Gaming Mode**: Separate ducking level optimized for gaming scenarios
- **Flexible App Rules**: Configure which applications should trigger ducking
  - AllExceptSpotify mode (default)
  - Whitelist mode (only specified apps trigger ducking)
  - Blacklist mode (all apps except specified ones trigger ducking)
- **Global Hotkey Support**: Quick toggle ducking on/off with customizable keyboard shortcuts
- **System Tray Integration**: Runs quietly in the background with easy access to settings
- **Startup Integration**: Optional auto-start with Windows
- **Highly Configurable**: Fine-tune attack time, release time, thresholds, and ducking levels

## 🚀 Getting Started

### Prerequisites

- Windows 10/11
- .NET 10.0 Runtime

### Installation

1. Download the latest release
2. Extract to your preferred location
3. Run `SpotyDuckTray.exe`
4. The app will appear in your system tray

### First-Time Setup

1. Right-click the system tray icon
2. Select "Settings"
3. Configure your preferred ducking level and timing
4. (Optional) Enable "Start with Windows" for automatic startup

## ⚙️ Configuration

### Basic Settings

- **Duck Level**: Volume level when ducking (0.0 - 1.0, default: 0.15)
- **Attack Time**: Delay before ducking starts (ms, default: 200)
- **Release Time**: Delay before volume restores (ms, default: 600)
- **Fade Duration**: Smooth transition time (ms, default: 120)
- **Threshold**: Audio detection sensitivity (default: 0.02)

### Gaming Mode

- **Gaming Duck Level**: Higher volume level for gaming (default: 0.60)
- Useful when you want to hear background music while gaming but still catch important audio cues

### App Rules

Configure which applications trigger ducking:
- **AllExceptSpotify**: Duck for any audio except Spotify (default)
- **Whitelist**: Only duck for specified applications
- **Blacklist**: Duck for all applications except specified ones

### Hotkey

- Enable global hotkey for quick toggle
- Default: `Ctrl+Shift+D`
- Customizable modifiers and key

## 📁 Configuration File

Settings are stored in `appsettings.json`:

```json
{
  "duckLevel": 0.15,
  "attackMs": 200,
  "releaseMs": 600,
  "threshold": 0.02,
  "pollIntervalMs": 100,
  "duckingEnabled": true,
  "gamingModeEnabled": false,
  "startWithWindows": false,
  "gamingDuckLevel": 0.60,
  "fadeDurationMs": 120,
  "appRuleMode": "AllExceptSpotify",
  "appRules": [],
  "hotkeyEnabled": false,
  "hotkeyModifiers": "Control, Shift",
  "hotkeyKey": "D",
  "selectedPreset": "Custom"
}
```

## 🎮 Use Cases

- **Video Calls**: Automatically lower music during Teams/Zoom/Discord calls
- **Gaming**: Keep background music playing while hearing voice chat and game audio
- **Content Creation**: Manage multiple audio sources without manual intervention
- **Productivity**: Never miss important notifications or system sounds

## 🛠️ Technical Details

- **Framework**: .NET 10.0 (Windows Forms)
- **Audio Library**: NAudio for real-time audio session monitoring
- **Architecture**: State machine pattern for precise ducking control
- **Performance**: Lightweight with minimal CPU/memory usage

## 📝 Logging

Logs are stored in `logs/spotyduck.log` for troubleshooting.

## 🤝 Contributing

Contributions are welcome! Feel free to submit issues or pull requests.
---
