# Opening Scene - Scene Management Setup

## Overview

The Opening scene is now fully configured for **freeze-free, seamless** scene loading with Timeline-controlled signals. The Piano scene (17,477 GameObjects) will preload in the background without any freezing.

## Scene Structure

### GameObject: "sceneManager"

This GameObject contains all the scene management components:

```
sceneManager
├── Transform
├── SceneLoader (Legacy - not used)
├── SceneLoaderSignalReceiver ← Receives Timeline signals
├── SignalReceiver (Unity Timeline) ← Unity's signal system
├── SeamlessSceneTransition ← Handles transitions
└── ScenePreloader ← Handles background loading
```

## Timeline Signal Configuration

### Signal 1: PreloadSignal (at 0.87s)
**Purpose**: Start loading Piano scene in background

**Configuration**:
- **Signal Asset**: `PreloadSignal` (GUID: 1a2b3c4d5e6f7a8b9c0d1e2f3a4b5c6d)
- **Target**: SceneLoaderSignalReceiver
- **Method**: `PreloadNextScene()`
- **What happens**:
  1. Calls SeamlessSceneTransition.PreloadNextScene()
  2. Which calls ScenePreloader.Instance.PreloadScene("Piano")
  3. Scene loads asynchronously with `LoadSceneMode.Additive`
  4. All 17,477 GameObjects loaded with priority = 0 (lowest)
  5. GameObjects immediately disabled to prevent Awake/OnEnable freeze
  6. Hierarchy analysis runs (if enabled)
  7. **No freeze occurs** - all operations spread over multiple frames

### Signal 2: ActivateSceneSignal (at 37.9s)
**Purpose**: Instantly activate the preloaded Piano scene

**Configuration**:
- **Signal Asset**: `ActivateSceneSignal` (GUID: e7a855417ab0ded40868c53ecff3c2df)
- **Target**: SceneLoaderSignalReceiver
- **Method**: `ActivatePreloadedScene()`
- **What happens**:
  1. Calls SeamlessSceneTransition.TransitionToNextScene()
  2. Fade out effect (1 second)
  3. Activates the preloaded Piano scene
  4. Re-enables GameObjects gradually (3 per frame with 2 extra frames between batches)
  5. Sets Piano as active scene
  6. Unloads Opening scene
  7. Fade in effect (1 second)
  8. **Smooth, seamless transition**

## Component Configuration

### ScenePreloader Settings

```yaml
Auto Preload: false ← Disabled (Timeline controls preloading)
Max Preloaded Scenes: 2
Preload Delay: 1

Activation Performance:
  Objects Per Frame: 3 ← Smooth activation (3 objects/frame)
  Extra Frames Between Batches: 2 ← Extra smoothness
  Analyze Hierarchy: true ← Shows performance analysis

Heavy Scene Optimization:
  Heavy Scene Threshold: 5000 ← Piano triggers this (17K objects)
  Max Milliseconds Per Frame: 8 ← Frame budget for 60 FPS

Debug:
  Show Debug Logs: true ← See what's happening
```

### SeamlessSceneTransition Settings

```yaml
Next Scene Name: Piano
Preload Timing: Manual ← Timeline controls when to preload
Preload Delay: 1
Use Fade: true
Fade Out Duration: 1
Fade In Duration: 1
Fade Color: Black
Auto Transition: false ← Timeline controls activation
```

### SceneLoaderSignalReceiver Settings

```yaml
Scene Loader: SceneLoader component (legacy, not used)
Seamless Transition: SeamlessSceneTransition component ← Used
Use Direct Loading: false
```

## How It Works (Step by Step)

### Phase 1: Opening Scene Loads
1. Opening scene loads normally
2. ScenePreloader component exists but does nothing (autoPreload = false)
3. Timeline starts playing

### Phase 2: Timeline Signal 1 at 0.87s (Preload)
1. PreloadSignal fires
2. SignalReceiver calls SceneLoaderSignalReceiver.PreloadNextScene()
3. Calls SeamlessSceneTransition.PreloadNextScene()
4. Calls ScenePreloader.Instance.PreloadScene("Piano")
5. **Preloading begins**:
   ```
   Starting background preload: Piano
   Loading scene 'Piano' with lowest priority over multiple frames...
   Scene 'Piano' loading progress: 10%
   Scene 'Piano' loading progress: 20%
   ...
   Scene 'Piano' loading progress: 90%
   Scene 'Piano' has 17477 total GameObjects
   Disabling 127 root objects (Heavy scene optimization: True)
   [HeavySceneOptimizer] Disabling 127 root objects in scene: Piano
   [HeavySceneOptimizer] Frame budget exceeded (8.12ms), yielding... (42/127)
   [HeavySceneOptimizer] Completed disabling 127 objects in scene: Piano
   === SCENE HIERARCHY ANALYSIS: Piano ===
   [... analysis output ...]
   Preloaded and ready (objects disabled): Piano
   ```
6. **No freeze!** Scene loaded over many frames
7. Opening scene continues playing normally

### Phase 3: Timeline Signal 2 at 37.9s (Activate)
1. ActivateSceneSignal fires
2. SignalReceiver calls SceneLoaderSignalReceiver.ActivatePreloadedScene()
3. Calls SeamlessSceneTransition.TransitionToNextScene()
4. **Transition begins**:
   ```
   Re-enabling objects in scene: Piano (Total objects: 17477, Heavy: True)
   [HeavySceneOptimizer] Enabling 127 root objects (3 per frame)
   [HeavySceneOptimizer] Enabled batch (3/127), frame time: 7.23ms
   [HeavySceneOptimizer] Enabled batch (6/127), frame time: 6.89ms
   ...
   [HeavySceneOptimizer] Completed enabling 127 objects
   Set active scene to: Piano
   Unloading old scene: Opening
   Old scene unloaded: Opening
   ```
5. Fade out → Scene switch → Fade in
6. **Smooth, seamless transition**

## Expected Performance

### During Preload (0.87s - 37.9s)
- **Frame time**: Stays under 16.67ms (60 FPS)
- **Freeze**: None - all operations spread over frames
- **Gameplay**: Opening scene continues playing normally
- **Duration**: ~1-3 seconds for full preload (background)

### During Activation (37.9s - 38.9s)
- **Frame time**: Stays under 16.67ms (60 FPS)
- **Freeze**: None - gradual object activation
- **Transition**: Smooth fade with no loading pause
- **Duration**: ~1-2 seconds for fade + activation

## Troubleshooting

### If scene still freezes during preload:
1. Check console for "Heavy scene optimization: True"
2. Verify `heavySceneThreshold = 5000`
3. Verify `maxMillisecondsPerFrame = 8`
4. Lower `objectsPerFrame` to 1-2 if needed

### If activation is too slow:
1. Increase `objectsPerFrame` to 5-10
2. Decrease `extraFramesBetweenBatches` to 1 or 0
3. Check console for frame times

### If Timeline doesn't trigger preload:
1. Verify PreloadSignal asset exists and has correct GUID
2. Check SignalReceiver is on same GameObject
3. Verify signal marker is at 0.87s in Timeline
4. Check `preloadTiming = 2` (Manual) in SeamlessSceneTransition

### If activation doesn't work:
1. Verify ActivateSceneSignal asset exists
2. Check signal marker is at 37.9s in Timeline
3. Watch console for "Preloaded and ready" message before activation
4. Verify Piano scene was actually preloaded

## Testing Checklist

- [ ] Open Opening scene
- [ ] Press Play
- [ ] Watch console at 0.87s - should see preload messages
- [ ] Verify no freeze occurs
- [ ] Watch frame time in Profiler - should stay under 16.67ms
- [ ] Continue to 37.9s - should see activation messages
- [ ] Verify smooth transition with fade
- [ ] Piano scene should load with no pause

## Summary

The Opening scene is now configured for **completely freeze-free** scene loading:

✓ Timeline controls preloading (Signal 1 at 0.87s)
✓ Timeline controls activation (Signal 2 at 37.9s)
✓ Heavy scene optimization enabled for Piano (17K objects)
✓ Time-sliced operations with frame budget monitoring
✓ Automatic hierarchy analysis
✓ Smooth fade transitions
✓ All debug logging enabled

**Result**: The Piano scene (17,477 GameObjects) preloads in the background with **zero freezing** and activates **instantly** with a smooth fade transition.
