# VTS RTX GPU Selector

[English](README.md)

[VTube Studio](https://denchisoft.com/)에서 **AMD + NVIDIA 듀얼 GPU** 환경에서도 NVIDIA RTX 트래킹을 사용할 수 있게 해주는 [BepInEx](https://github.com/BepInEx/BepInEx) 플러그인입니다.

## 문제

VTube Studio는 **렌더링 GPU**만 확인해서 RTX 지원 여부를 판단합니다. OBS 캡처 호환성을 위해 VTS를 AMD GPU에서 렌더링하면, 시스템에 RTX GPU가 있어도 NVIDIA Broadcast 트래킹 메뉴가 감춰집니다.

## 해결

이 플러그인은:

1. **Windows 레지스트리로 전체 GPU 스캔** (렌더링 GPU만이 아닌 전체)
2. RTX GPU가 발견되면 **`SupportsRTX = true` 강제 설정**
3. ExpressionApp 시작 시 **`CUDA_VISIBLE_DEVICES` 주입**으로 선택된 GPU 사용
4. NVIDIA 트래킹 품질 선택 시 **VTS 네이티브 UI로 GPU 선택 팝업** 표시

## 요구사항

- **Windows** + NVIDIA RTX GPU
- **VTube Studio** (Steam 버전)
- **BepInEx 5.4.x** (Unity Mono x64)

## 설치

### 1. BepInEx 설치

[BepInEx 5.4.23.2 (x64)](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.2)를 다운로드하고 VTS 루트 폴더에 압축 해제:

```
VTube Studio/
├── winhttp.dll              ← BepInEx
├── doorstop_config.ini      ← BepInEx
└── BepInEx/
    └── core/                ← BepInEx 런타임
```

### 2. 플러그인 설치

[Releases](https://github.com/bbggkkk/VTS-RTX-GPU-Selector/releases)에서 `VTSRTXGPUSelector.dll`을 다운로드하여 아래 경로에 배치:

```
VTube Studio/BepInEx/plugins/VTSRTXGPUSelector.dll
```

### 3. 사용법

1. VTube Studio 실행 (AMD GPU에서 렌더링)
2. **설정 → 트래킹 품질 → NVIDIA Broadcast** 선택
3. GPU 선택 팝업이 자동 표시됨
4. RTX GPU 선택 → 트래킹 시작

### 문제 해결

만약 VTube Studio가 실행되었는데 RTX GPU가 자동으로 선택되지 않는다면:

1. `VTSRTXGPUSelector.dll` 플러그인이 `[VTS 설치폴더]/BepInEx/plugins/` 안에 있는지 다시 확인해 주세요.
2. VTube Studio 공식 설치 관리자를 통해 **NVIDIA Tracker가 정상적으로 설치되었는지** 확인하세요.

### 소스에서 컴파일 (로컬 빌드)

직접 플러그인을 빌드하고 싶다면 VTube Studio가 설치되어 있는지 확인하고 다음을 실행하세요:

```bash
git clone https://github.com/bbggkkk/VTS-RTX-GPU-Selector.git
cd VTS-RTX-GPU-Selector
dotnet build src/VTS-RTX-GPU-Selector.csproj -c Release
```

> **참고:** 이 프로젝트는 기본적으로 VTube Studio가 `C:\Program Files (x86)\Steam\steamapps\common\VTube Studio`에 설치되어 있다고 가정합니다. 만약 다른 곳에 설치했다면 다음과 같이 경로를 지정하세요:
> `dotnet build src/VTS-RTX-GPU-Selector.csproj -c Release /p:VTSPath="D:\SteamLibrary\steamapps\common\VTube Studio"`

출력: `src/bin/Release/net462/VTSRTXGPUSelector.dll`

## 동작 원리

```
VTS 시작 → Logger.LogDeviceType() → "AMD Radeon" → SupportsRTX = false
  → [Harmony Postfix] 레지스트리 스캔 → RTX 발견 → SupportsRTX = true

설정에서 NVIDIA 트래킹 품질 선택
  → [Harmony Postfix] GPU 선택 팝업 (VTS 네이티브 UI)

ExpressionApp 시작 → MXStarter.StartTrackerWithArguments()
  → [Harmony Prefix] CUDA_VISIBLE_DEVICES = 선택된 GPU 인덱스
  → ExpressionApp이 지정된 RTX GPU 사용
  → [Harmony Postfix] 환경변수 초기화
```

## 제거

```
VTube Studio/winhttp.dll        ← 삭제
VTube Studio/doorstop_config.ini ← 삭제
VTube Studio/BepInEx/            ← 폴더 삭제
```

## 라이선스

MIT
