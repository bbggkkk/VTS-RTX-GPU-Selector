# VTS RTX GPU Selector

[![Build](https://github.com/bbggkkk/VTS-RTX-GPU-Selector/actions/workflows/build.yml/badge.svg)](https://github.com/bbggkkk/VTS-RTX-GPU-Selector/actions/workflows/build.yml)

[한국어](README.ko.md)

A [BepInEx](https://github.com/BepInEx/BepInEx) plugin for [VTube Studio](https://denchisoft.com/) that enables NVIDIA RTX face tracking on systems with **AMD + NVIDIA dual-GPU** setups.

## Problem

VTube Studio checks only the **rendering GPU** to determine RTX support. If VTS renders on an AMD GPU (common for OBS capture compatibility), NVIDIA Broadcast tracking features are hidden — even though an RTX GPU is available in the system for ExpressionApp.

## Solution

This plugin:

1. **Scans all system GPUs** via Windows Registry (not just the rendering GPU)
2. **Forces `SupportsRTX = true`** if any RTX GPU is found
3. **Injects `CUDA_VISIBLE_DEVICES`** to direct ExpressionApp to the selected RTX GPU
4. **GPU selection popup** using VTS's native UI when NVIDIA tracking quality is selected

## Requirements

- **Windows** with NVIDIA RTX GPU
- **VTube Studio** (Steam version)
- **BepInEx 5.4.x** (Unity Mono x64)

## Installation

### 1. Install BepInEx

Download [BepInEx 5.4.23.2 (x64)](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.2) and extract to VTS root:

```
VTube Studio/
├── winhttp.dll              ← BepInEx
├── doorstop_config.ini      ← BepInEx
└── BepInEx/
    └── core/                ← BepInEx runtime
```

### 2. Install Plugin

Download `VTSRTXGPUSelector.dll` from [Releases](https://github.com/bbggkkk/VTS-RTX-GPU-Selector/releases) and place it in:

```
VTube Studio/BepInEx/plugins/VTSRTXGPUSelector.dll
```

### 3. Usage

1. Launch VTube Studio (on AMD GPU for rendering)
2. Go to **Settings → Tracking Quality → NVIDIA Broadcast**
3. GPU selection popup appears automatically
4. Select your RTX GPU → Start tracking

### Troubleshooting

If VTube Studio is running but the RTX GPU isn't selected by default:

1. Ensure the plugin `VTSRTXGPUSelector.dll` is in `[VTS Path]/BepInEx/plugins/`
2. Check if the NVIDIA Tracker is properly installed via the official VTube Studio installer.

## Building from Source

```bash
# Clone
git clone https://github.com/[your-username]/VTS-RTX-GPU-Selector.git
cd VTS-RTX-GPU-Selector
dotnet build src/VTS-RTX-GPU-Selector.csproj -c Release
```

Output: `bin/Release/net462/VTS-RTX-GPU-Selector.dll`

## How It Works

```
VTS starts → Logger.LogDeviceType() → "AMD Radeon" → SupportsRTX = false
  → [Harmony Postfix] Registry scan → RTX found → SupportsRTX = true

NVIDIA tracking selected in settings
  → [Harmony Postfix] GPU selection popup (native VTS UI)

ExpressionApp starts → MXStarter.StartTrackerWithArguments()
  → [Harmony Prefix] CUDA_VISIBLE_DEVICES = selected GPU index
  → ExpressionApp uses selected RTX GPU
  → [Harmony Postfix] env var cleared
```

## License

MIT
