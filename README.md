# 🎮 True Colors

<div align="center">

![Unity 6](https://img.shields.io/badge/Unity-6000.6.0f1-000000?style=for-the-badge&logo=unity&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)
![URP](https://img.shields.io/badge/Render%20Pipeline-URP%2017.6.0-222c37?style=for-the-badge)
![Input System](https://img.shields.io/badge/Input%20System-EnhancedTouch%201.20-blue?style=for-the-badge)
![Target](https://img.shields.io/badge/Target-iOS%20%7C%20Android%20%7C%20Desktop-orange?style=for-the-badge)
![Framerate](https://img.shields.io/badge/Framerate-60%20FPS%20Locked-success?style=for-the-badge)

<p align="center">
  <b>A high-octane, split-brain dual-runner arcade game built for high-refresh mobile and desktop platforms in Unity 6.</b>
</p>

</div>

---

## 📖 Table of Contents

- [Overview](#-overview)
- [Gameplay Mechanics](#-gameplay-mechanics)
  - [Split-Screen Coordination](#split-screen-coordination)
  - [Color Matrix & Hazard Rules](#color-matrix--hazard-rules)
  - [Streak Multipliers & Overdrive Mode](#streak-multipliers--overdrive-mode)
  - [Fail Conditions](#fail-conditions)
- [Software Architecture & Systems](#-software-architecture--systems)
  - [Component Architecture](#component-architecture)
  - [Zero-Allocation Object Pooling (`BlockPool`)](#zero-allocation-object-pooling-blockpool)
  - [Dynamic Difficulty Scaling (`BlockSpawner`)](#dynamic-difficulty-scaling-blockspawner)
  - [Multi-Touch & Spatial Input Handling (`InputHandler`)](#multi-touch--spatial-input-handling-inputhandler)
  - [Native Android JNI & iOS Haptics (`HapticFeedback`)](#native-android-jni--ios-haptics-hapticfeedback)
  - [Unscaled Procedural Camera Shake (`CameraShake`)](#unscaled-procedural-camera-shake-camerashake)
- [Project Structure & Asset Organization](#-project-structure--asset-organization)
- [Controls](#-controls)
  - [Mobile Touch Zones](#mobile-touch-zones)
  - [Desktop / Editor Keybindings](#desktop--editor-keybindings)
- [Prerequisites & Getting Started](#-prerequisites--getting-started)
  - [Engine Requirements](#engine-requirements)
  - [Installation & Setup](#installation--setup)
- [Build & Deployment Pipeline](#-build--deployment-pipeline)
  - [iOS Build Setup](#ios-build-setup)
  - [Android Build Setup](#android-build-setup)
- [Performance & Architectural Highlights](#-performance--architectural-highlights)
- [Authorship & Credits](#-authorship--credits)

---

## 🕹️ Overview

**True Colors** challenges the player's bilateral cognitive processing and reflex precision. Players simultaneously pilot two independent starships situated across partitioned left and right highway sectors:
- **Left Ship (Red Vessel)** defends the left sector from falling color blocks.
- **Right Ship (Blue Vessel)** defends the right sector from falling color blocks.

Players must filter matching target colors while dodging mismatched hazards, stringing together uninterrupted combos to unlock a hyper-speed **Overdrive Mode** that transmutes hazardous blocks into scoring opportunities.

Designed with mobile-first performance principles, True Colors runs with an ultra-lightweight CPU overhead, zero garbage collection pauses during active gameplay, and native hardware vibration response.

---

## ⚡ Gameplay Mechanics

### Split-Screen Coordination

The game field is divided vertically into two symmetric arenas, each containing **3 discrete lanes** (6 parallel tracks in total):

```
       LEFT SECTOR (RED SHIP)        |       RIGHT SECTOR (BLUE SHIP)
 Lane -2.1    Lane -1.4    Lane -0.7 | Lane +0.7    Lane +1.4    Lane +2.1
   [ | ]        [ | ]        [ | ]   |   [ | ]        [ | ]        [ | ]
     │            │            │     |     │            │            │
     ▼            ▼            ▼     |     ▼            ▼            ▼
 ────────────────────────────────────┼────────────────────────────────────
            [ 🚀 Left Ship ]         |           [ 🚀 Right Ship ]
```

Smooth lane changes are governed by interpolation damping (`Vector3.Lerp` at `15f` units/sec), providing punchy, responsive feedback without instantaneous jarring teleportation.

### Color Matrix & Hazard Rules

Blocks spawn from the top boundary (`Y = 6.0`) and cascade downward. Each sector features customized color-matching rules:

| Sector | Player Ship | Valid Target Color | Obstacle Colors | Penalty for Obstacle Contact |
| :--- | :--- | :--- | :--- | :--- |
| **Left Arena** (`X < 0`) | **Red Ship** | 🔴 **Red** (`#FF3333`) | 🔵 Blue, 🟡 Yellow, 🟢 Green | **Immediate Game Over** |
| **Right Arena** (`X > 0`) | **Blue Ship** | 🔵 **Blue** (`#3380FF`) | 🔴 Red, 🟡 Yellow, 🟢 Green | **Immediate Game Over** |

### Streak Multipliers & Overdrive Mode

Successive successful color catches increment the combo counter, progressively boosting score accumulation:

```mermaid
graph LR
    A[Normal Play x1] -->|5 Streak| B[Multiplier x2]
    B -->|10 Streak| C[Multiplier x3]
    C -->|15 Streak| D[🔥 OVERDRIVE x5 🔥]
    D -->|6s Expiration| A
```

- **Streak < 5:** `1x` Point Multiplier (`+10` pts per catch).
- **Streak 5 - 9:** `2x` Point Multiplier (`+20` pts per catch).
- **Streak 10 - 14:** `3x` Point Multiplier (`+30` pts per catch).
- **Streak ≥ 15 → OVERDRIVE ACTIVATION (6 Seconds Duration):**
  - **`5x` Multiplier:** Massive score inflation (`+50` pts per catch).
  - **Invulnerability:** Player is completely immune to collision game overs.
  - **Dynamic Board Transmutation:** All currently falling hazard blocks in both sectors instantly re-skin their colors to match the target vessels.
  - **100% Target Spawn Bias:** Every block generated by the spawner during Overdrive is guaranteed to match the sector's ship.

### Fail Conditions

1. **Hazard Collision:** Catching a block that does not match the active vessel's `targetColor`.
2. **Target Miss:** Allowing a target block (Red on Left or Blue on Right) to fall past the bottom threshold (`Y < -5.5`). Missed obstacles do not penalize the player.

---

## 🏗️ Software Architecture & Systems

True Colors is engineered following clean code and SOLID architectural patterns in C#, emphasizing modularity, testability, and deterministic state transitions.

```mermaid
graph TD
    subgraph Core
        GM[GameManager.cs]
        CC[CountdownController.cs]
        MMC[MainMenuController.cs]
    end

    subgraph Gameplay & Entities
        SC[ShipController.cs]
        ST[ShipTarget.cs]
        CB[CollectibleBlock.cs]
        BP[BlockPool.cs]
        BS[BlockSpawner.cs]
    end

    subgraph Hardware & Feedback
        IH[InputHandler.cs]
        HF[HapticFeedback.cs]
        CS[CameraShake.cs]
    end

    IH -->|MoveLeft / MoveRight| SC
    BS -->|GetBlock / Recycle| BP
    CB -->|OnTriggerEnter2D| ST
    CB -->|AddScore / TriggerGameOver| GM
    CB -->|VibrateCollect| HF
    GM -->|VibrateGameOver| HF
    GM -->|Shake| CS
    CC -->|Enable Input & Spawner| IH
    CC -->|Enable Input & Spawner| BS
```

### Component Architecture

| Script | Namespace / Scope | Responsibility |
| :--- | :--- | :--- |
| [`GameManager.cs`](file:///Users/diegovilla/Desktop/unity/TrueColors/Assets/Scripts/GameManager.cs) | Core Singleton | Manages score, streaks, Overdrive state coroutines, `PlayerPrefs` high scores, UI bindings, and Game Over sequences. |
| [`ShipController.cs`](file:///Users/diegovilla/Desktop/unity/TrueColors/Assets/Scripts/ShipController.cs) | Gameplay Entity | Controls discrete 3-lane positioning, bounds verification, and smooth interpolation movements. |
| [`ShipTarget.cs`](file:///Users/diegovilla/Desktop/unity/TrueColors/Assets/Scripts/ShipTarget.cs) | Gameplay Entity | Exposes target `GameColor` metadata inspected by colliding blocks. |
| [`CollectibleBlock.cs`](file:///Users/diegovilla/Desktop/unity/TrueColors/Assets/Scripts/CollectibleBlock.cs) | Gameplay Entity | Handles downward translation, bounds checking (`Y < -5.5`), particle instantiation, color switching, and pool recycling. |
| [`BlockPool.cs`](file:///Users/diegovilla/Desktop/unity/TrueColors/Assets/Scripts/BlockPool.cs) | Memory Management | Thread-safe FIFO `Queue<GameObject>` pool ensuring zero runtime allocations (`0B GC.Alloc`) during spawning cycles. |
| [`BlockSpawner.cs`](file:///Users/diegovilla/Desktop/unity/TrueColors/Assets/Scripts/BlockSpawner.cs) | Procedural Generator | Time-scaled difficulty director that lerps spawn speed and rates while calculating procedural hazard odds. |
| [`InputHandler.cs`](file:///Users/diegovilla/Desktop/unity/TrueColors/Assets/Scripts/InputHandler.cs) | Input Subsystem | Decouples New Input System touches and keyboard events, transforming screen coordinate quadrants into lane commands. |
| [`CountdownController.cs`](file:///Users/diegovilla/Desktop/unity/TrueColors/Assets/Scripts/CountdownController.cs) | UI Flow | Orchestrates the `3-2-1-GO!` startup sequence, locking input and spawners until the match officially commences. |
| [`HapticFeedback.cs`](file:///Users/diegovilla/Desktop/unity/TrueColors/Assets/Scripts/HapticFeedback.cs) | Native Bridge | Low-latency platform wrapper executing direct Android JNI `VibrationEffect` calls and iOS `Handheld.Vibrate()`. |
| [`CameraShake.cs`](file:///Users/diegovilla/Desktop/unity/TrueColors/Assets/Scripts/CameraShake.cs) | Visual FX | Delivers unscaled procedural camera vibration (`Time.unscaledDeltaTime`), active even during `Time.timeScale = 0`. |
| [`MainMenuController.cs`](file:///Users/diegovilla/Desktop/unity/TrueColors/Assets/Scripts/MainMenuController.cs) | Navigation | Regulates scene transitions between Menu and MainGame, frame pacing initialization, and options overlays. |

### Zero-Allocation Object Pooling (`BlockPool`)

Spawning and destroying hundreds of falling blocks per minute creates severe garbage collection pressure and noticeable micro-stutter on mobile CPUs. `BlockPool` prewarms 25 instances on `Awake()`:

```csharp
public GameObject GetBlock()
{
    GameObject block = pool.Count > 0 ? pool.Dequeue() : Instantiate(blockPrefab, transform);
    block.SetActive(true);
    return block;
}

public void ReturnBlock(GameObject block)
{
    block.SetActive(false);
    pool.Enqueue(block);
}
```

When blocks exit the lower screen bounds or collide with a vessel, they are deactivated and returned to the FIFO queue rather than invoking `Destroy()`.

### Dynamic Difficulty Scaling (`BlockSpawner`)

The difficulty ramp interpolates both spawn interval and vertical velocity over a configurable ramp window (`difficultyRampTime = 60s`):

$$\text{Progress} = \text{clamp}_0^1\left(\frac{t_{\text{elapsed}}}{60.0}\right)$$

- **Spawn Rate:** Interpolates linearly from `1.10s` down to `0.45s`.
- **Block Velocity:** Accelerates linearly from `4.5` units/sec up to `10.0` units/sec.
- **Color Distribution:** 50% probability to spawn the sector's matching target color; 50% distributed among hazardous colors.

### Multi-Touch & Spatial Input Handling (`InputHandler`)

Utilizing Unity's New Input System with `EnhancedTouchSupport`, the screen is partitioned into four dynamic quadrants:

```
┌───────────────────────────────┬───────────────────────────────┐
│        LEFT SECTOR (X < 50%)  │       RIGHT SECTOR (X >= 50%) │
│  [ Left Ship: Move Left ]     │  [ Right Ship: Move Left ]    │
│  Touch: 0% - 25% Screen Width │  Touch: 50% - 75% Screen Width│
│───────────────────────────────┼───────────────────────────────┤
│  [ Left Ship: Move Right ]    │  [ Right Ship: Move Right ]   │
│  Touch: 25% - 50% Screen Width│  Touch: 75% - 100% Screen Width│
└───────────────────────────────┴───────────────────────────────┘
```

Both ships can be maneuvered simultaneously by multiple fingers without input cross-talk or gesture conflicts.

### Native Android JNI & iOS Haptics (`HapticFeedback`)

To avoid standard latency in mobile feedback, True Colors leverages Android JNI directly via `AndroidJavaClass` to access `android.os.VibrationEffect`:
- **Android API ≥ 26:** Invokes `VibrationEffect.createOneShot(45L, -1)` targeting the native vibration hardware layer.
- **Android API < 26:** Falls back to `vibrator.Call("vibrate", 45L)`.
- **iOS:** Dispatches optimized tactile impulses via `Handheld.Vibrate()`.

### Unscaled Procedural Camera Shake (`CameraShake`)

When a game-ending impact occurs, the simulation freezes via `Time.timeScale = 0f`. Standard coroutines relying on `Time.deltaTime` would immediately halt. `CameraShake` circumvents this constraint by sampling `Time.unscaledDeltaTime`, delivering visual punch during fatal impacts.

---

## 📁 Project Structure & Asset Organization

```
TrueColors/
├── Assets/
│   ├── Materials/                  # URP Materials & Trail renderers
│   │   ├── Trail_Mat.mat           # Ship lane transition trail effect
│   │   └── Trail_Mat.mat.meta
│   ├── Prefabs/                    # Configured Game Entities
│   │   ├── Block.prefab            # CollectibleBlock entity with BoxCollider2D & SpriteRenderer
│   │   └── CollectParticles.prefab # Particle burst VFX matching target color
│   ├── Scenes/
│   │   ├── MainMenu.unity          # Entry scene (Play, Options, High Score display)
│   │   └── MainGame.unity          # Core gameplay arena, Ships, Spawner, UGUI Canvas
│   ├── Scripts/                    # Complete C# game architecture
│   │   ├── BlockPool.cs
│   │   ├── BlockSpawner.cs
│   │   ├── CameraShake.cs
│   │   ├── CollectibleBlock.cs
│   │   ├── CountdownController.cs
│   │   ├── GameManager.cs
│   │   ├── HapticFeedback.cs
│   │   ├── InputHandler.cs
│   │   ├── MainMenuController.cs
│   │   ├── ShipController.cs
│   │   └── ShipTarget.cs
│   ├── Settings/                   # URP pipeline assets & quality profiles
│   └── TextMesh Pro/               # Font assets, glyph tables & material presets
├── Packages/
│   └── manifest.json               # Package dependencies (URP, InputSystem, Cinemachine)
├── ProjectSettings/                # Build profiles, physics layers, graphics settings
└── dist/                           # Native Xcode iOS export project (IL2CPP)
```

---

## 🎮 Controls

### Mobile Touch Zones

Tap anywhere in the corresponding screen region to shift lanes:

| Screen Region | Target Ship | Action |
| :--- | :--- | :--- |
| **0% – 25% Width** (Far Left) | 🔴 **Left Ship** | Shift 1 Lane Left |
| **25% – 50% Width** (Mid Left) | 🔴 **Left Ship** | Shift 1 Lane Right |
| **50% – 75% Width** (Mid Right) | 🔵 **Right Ship** | Shift 1 Lane Left |
| **75% – 100% Width** (Far Right) | 🔵 **Right Ship** | Shift 1 Lane Right |

### Desktop / Editor Keybindings

For desktop testing and Unity Editor development:

| Key | Ship | Direction |
| :---: | :---: | :---: |
| <kbd>A</kbd> | 🔴 Left Ship | Move Left |
| <kbd>D</kbd> | 🔴 Left Ship | Move Right |
| <kbd>←</kbd> | 🔵 Right Ship | Move Left |
| <kbd>→</kbd> | 🔵 Right Ship | Move Right |

---

## 🚀 Prerequisites & Getting Started

### Engine Requirements

- **Unity Editor:** `6000.6.0f1` (Unity 6) or compatible 6000.x stream.
- **Render Pipeline:** Universal Render Pipeline (URP) `17.6.0+`.
- **Input System:** Unity New Input System `1.20.0+`.
- **Target OS:** iOS 14.0+, Android 8.0+ (API Level 26+), macOS, Windows.

### Installation & Setup

1. **Clone the Repository:**
   ```bash
   git clone https://github.com/bxnnyblue92/TrueColors.git
   cd TrueColors
   ```

2. **Open Project in Unity Hub:**
   - Launch **Unity Hub**.
   - Click **Add** -> **Add project from disk**.
   - Select the `TrueColors` root directory.
   - Ensure the Editor Version is set to **Unity 6 (6000.6.0f1)**.

3. **Verify Package Dependencies:**
   - Open the project and allow Unity Package Manager (UPM) to resolve dependencies listed in `Packages/manifest.json`.

4. **Run the Game in Play Mode:**
   - In the **Project** window, navigate to `Assets/Scenes/`.
   - Double-click [`MainMenu.unity`](file:///Users/diegovilla/Desktop/unity/TrueColors/Assets/Scenes/MainMenu.unity) to load the starting scene.
   - Press the **Play** button at the top of the Unity Editor.
   - Use <kbd>A</kbd>/<kbd>D</kbd> and <kbd>←</kbd>/<kbd>→</kbd> to control both ships simultaneously.

---

## 📦 Build & Deployment Pipeline

### iOS Build Setup

The project includes an exported Xcode workspace under `dist/`:

1. In Unity, select **File** -> **Build Profiles** (or **Build Settings**).
2. Switch Platform to **iOS**.
3. Ensure the scene build order is:
   - Index 0: `Assets/Scenes/MainMenu.unity`
   - Index 1: `Assets/Scenes/MainGame.unity`
4. Set **Scripting Backend** to `IL2CPP` and **Target Architecture** to `ARM64`.
5. Click **Build** and output to the destination folder.
6. Open `Unity-iPhone.xcworkspace` in Xcode, configure your **Signing & Capabilities** team profile, and deploy to your connected iOS device.

### Android Build Setup

1. Switch Platform to **Android** in **Build Settings**.
2. Set **Scripting Backend** to `IL2CPP` and enable `ARM64` in **Player Settings** -> **Other Settings**.
3. Minimum API Level: **Android 8.0 'Oreo' (API Level 26)** (guarantees `VibrationEffect` hardware support).
4. Target API Level: **Automatic / Highest Installed** (API 34+ recommended).
5. Build format: `.aab` (Android App Bundle) for Google Play release or `.apk` for internal testing.

---

## 📈 Performance & Architectural Highlights

- **⚡ Zero-Allocation Game Loop:** All falling block entities are re-routed through `BlockPool`, generating zero garbage collection spikes during high-density spawn phases.
- **📱 Fluid 60 FPS Target:** Frame rate is locked to 60 FPS (`Application.targetFrameRate = 60`) with `QualitySettings.vSyncCount = 0` to ensure optimal thermal efficiency and battery conservation on mobile hardware.
- **🛡️ State Resilient Singletons:** Core orchestrators (`GameManager`, `BlockPool`, `CameraShake`) implement strict singleton guards (`Instance != null && Instance != this -> Destroy`) preventing duplicate instances across scene reloads.
- **✨ Real-Time Dynamic Reskinning:** During Overdrive activation, the board dynamically queries and recolors existing active blocks without deallocating or re-instantiating objects.
- **🔊 Hardware-Accelerated Haptics:** Direct JNI integration bypasses high-level wrappers for ultra-low latency physical tactile feedback.

---

## ✒️ Authorship & Credits

> This digital ecosystem has been designed, structured, and developed to high-performance standards by **[Cabuweb](https://cabuweb.com)** - **Software Developer: Diego Villa**.
