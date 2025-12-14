# Unity Scene Manager System
### Compatible with Unity 6.2

A flexible scene loading system that works with or without Timeline, supporting multiple trigger methods including proximity, interaction, and boolean conditions.

## ⚡ NEW: Seamless Background Loading (RECOMMENDED)

**Eliminate loading pauses and freezes!** The new seamless loading system preloads scenes in the background, creating buttery-smooth transitions without any gameplay interruption.

### Why Use Seamless Loading?

- ❌ **Old Way:** Timeline signal fires → 5 second freeze → scene loads → gameplay resumes
- ✅ **New Way:** Scene loads silently in background → Timeline signal fires → instant transition!

**Perfect for:** Cinematic sequences, cutscenes, and any situation where you can't afford a loading pause.

---

## 📦 Components Included

### Core Loading
1. **SceneLoader.cs** - Main scene loading manager (legacy, synchronous)
2. **ScenePreloader.cs** - ⭐ NEW: Background scene preloader (no pauses!)
3. **SeamlessSceneTransition.cs** - ⭐ NEW: Seamless transition manager

### Triggers
4. **ProximitySceneTrigger.cs** - Load scenes via proximity zones
5. **InteractionSceneTrigger.cs** - Load scenes via player interaction
6. **BooleanSceneTrigger.cs** - Load scenes via boolean conditions

### Timeline Integration
7. **SceneLoaderTimeline.cs** - Timeline integration with seamless support

---

## 🚀 Quick Start

### Basic Setup

1. **Add scenes to Build Settings**
   - Go to File > Build Settings
   - Add all scenes you want to load

2. **Create a Scene Loader**
   - Create an empty GameObject in your scene
   - Add the `SceneLoader` component
   - Configure the scene name or index
   - Adjust fade settings, loading mode, etc.

---

## 📋 Usage Methods

### Method 1: Proximity Trigger

**Perfect for:** Doorways, level transitions, area boundaries

1. Create a GameObject with a Collider (set as Trigger)
2. Add `ProximitySceneTrigger` component
3. Configure:
   - Target Tag (usually "Player")
   - Trigger settings
4. Reference a SceneLoader or use direct loading

```csharp
// The trigger will automatically detect when the player enters
// No code needed!
```

**Inspector Setup:**
```
- Target Tag: "Player"
- Trigger Once: ✓
- Trigger Delay: 0.5s
- Scene Loader: [Reference to SceneLoader component]
```

---

### Method 2: Interaction Trigger

**Perfect for:** Doors, portals, interactive objects

1. Create an interactable object
2. Add `InteractionSceneTrigger` component
3. Configure:
   - Interaction range
   - Input settings (old or new Input System)
   - Optional prompt UI

```csharp
// The trigger will show a prompt when player is near
// Player presses the interaction key to load the scene
```

**Inspector Setup:**
```
- Target Tag: "Player"
- Interaction Range: 3.0
- Interaction Key: E
- Show Prompt: ✓
- Scene Loader: [Reference to SceneLoader component]
```

---

### Method 3: Boolean Trigger

**Perfect for:** Quest completion, game state changes, conditional loading

1. Create a GameObject
2. Add `BooleanSceneTrigger` component
3. Configure trigger settings
4. Set the condition from your game logic

```csharp
// From another script:
public BooleanSceneTrigger sceneTrigger;

void OnQuestComplete()
{
    sceneTrigger.SetTriggerCondition(true);
    // Scene will automatically load
}

// Or trigger immediately:
sceneTrigger.TriggerSceneLoad();
```

**Inspector Setup:**
```
- Check Every Frame: □
- Check Interval: 0.5s
- Delay After Trigger: 1.0s
- Trigger Once: ✓
- Scene Loader: [Reference to SceneLoader component]
```

---

### Method 4: Direct Script Call

**Perfect for:** Menu buttons, custom game logic

```csharp
using SceneManagement;

public class MenuController : MonoBehaviour
{
    public SceneLoader sceneLoader;
    
    public void OnPlayButtonClick()
    {
        sceneLoader.LoadScene();
    }
    
    public void LoadSpecificScene()
    {
        sceneLoader.LoadSceneByName("Level1");
    }
    
    // Or use the static method:
    public void QuickLoad()
    {
        SceneLoader.LoadSceneStatic("MainMenu");
    }
}
```

---

## 🎬 Timeline Integration

### Method 1: Using Signals (Recommended)

1. Add a `Signal Receiver` component to a GameObject
2. Add `SceneLoaderSignalReceiver` component to the same GameObject
3. Reference your SceneLoader
4. In Timeline:
   - Right-click and create a Signal Emitter
   - Create a new Signal Asset
   - Link the Signal to the receiver
   - Call `LoadSceneFromTimeline()` method

**Timeline Setup:**
```
1. Create Signal Asset (Right-click in Project)
2. Add Signal Emitter to Timeline
3. Assign Signal Asset
4. In Signal Receiver:
   - Add Reaction
   - Drag GameObject with SceneLoaderSignalReceiver
   - Select LoadSceneFromTimeline()
```

### Method 2: Using Custom Track

1. In Timeline, click "Add Track from Asset"
2. Select `SceneLoaderTrack`
3. Drag your SceneLoader GameObject to the track binding
4. Add clips and configure scene names

**Track Setup:**
```
- Add SceneLoaderTrack to Timeline
- Bind to SceneLoader component
- Right-click track > Add Clip
- Configure clip:
  - Scene Name: "YourScene"
  - Load On Start: ✓
  - Load On End: □
```

---

## ⚙️ SceneLoader Configuration

### Scene Settings
- **Scene Name**: Name of the scene to load (takes priority)
- **Scene Index**: Build index of the scene (used if name is empty)
- **Load Mode**: Single (replace current) or Additive (add to current)

### Loading Settings
- **Use Async Loading**: Smooth loading with progress tracking
- **Minimum Load Time**: Ensure loading screen shows for minimum duration
- **Show Loading Screen**: Display a loading screen prefab
- **Loading Screen Prefab**: Optional prefab to instantiate during load

### Fade Settings
- **Use Fade**: Enable fade in/out transition
- **Fade Out Duration**: Time to fade to black
- **Fade In Duration**: Time to fade back from black
- **Fade Color**: Color of the fade overlay

### Trigger Settings
- **Load On Start**: Automatically load scene when script starts
- **Delay Before Load**: Wait time before loading begins

### Events
- **On Load Start**: Triggered when loading begins
- **On Load Complete**: Triggered when loading completes

---

## 🎯 Common Use Cases

### Door/Portal
```
1. Create door GameObject
2. Add BoxCollider (IsTrigger: ✓)
3. Add ProximitySceneTrigger
4. Add SceneLoader
5. Configure scene name
```

### Interactive Object (E.g., Chest that leads to treasure room)
```
1. Create chest GameObject
2. Add InteractionSceneTrigger
3. Add SceneLoader
4. Set interaction key and range
```

### Quest Completion Transition
```csharp
public class QuestManager : MonoBehaviour
{
    public BooleanSceneTrigger endingTrigger;
    
    public void CompleteAllQuests()
    {
        // All quests done, trigger scene change
        endingTrigger.SetTriggerCondition(true);
    }
}
```

### Cutscene to Next Level
```
1. Create Timeline with your cutscene
2. Add Signal Emitter near the end
3. Add SceneLoaderSignalReceiver to Timeline's GameObject
4. Configure signal to call LoadSceneByName("NextLevel")
```

### Menu Button
```csharp
// In Unity Button OnClick:
// Drag SceneLoader GameObject
// Select SceneLoader.LoadScene()
```

---

## 🎨 Advanced Features

### Custom Fade Colors
Change the fade color to match your game's aesthetic:
```
Fade Color: RGB(20, 10, 30) for dark purple
```

### Additive Scene Loading
Load scenes on top of existing ones:
```
Load Mode: Additive
// Useful for UI overlays, separate level chunks, etc.
```

### Loading Screen with Progress
Create a loading screen prefab with a slider/progress bar and assign it:
```
Show Loading Screen: ✓
Loading Screen Prefab: [Your Prefab]
```

### Scene Transitions in Timeline
Combine Timeline with scene loading for cinematic transitions:
1. Play cutscene animation
2. Fade to black
3. Load new scene
4. Scene fades in

---

## 🔧 Tips & Best Practices

### Performance
- Use Async Loading for large scenes
- Set appropriate Minimum Load Time for smooth transitions
- Use Additive loading for small scene additions

### User Experience
- Always provide feedback (fade, loading screen, or prompt)
- Set appropriate trigger delays to avoid accidental loads
- Use "Trigger Once" for doorways to prevent repeated triggers

### Organization
- Keep all scene triggers in a "SceneManagement" folder
- Name GameObjects clearly: "Door_ToLevel2", "Portal_ToBossRoom"
- Use consistent tag names across your project

### Debugging
- Enable Gizmos in Scene view to visualize trigger zones
- Check "Show Prompt" to verify interaction triggers are working
- Use OnLoadStart/OnLoadComplete events for debug logging

---

## 🐛 Troubleshooting

**Scene not loading?**
- Check scene is in Build Settings
- Verify scene name matches exactly (case-sensitive)
- Check console for error messages

**Player not triggering proximity zone?**
- Verify player has correct tag
- Ensure collider is set as Trigger
- Check trigger collider size in Scene view

**Interaction not working?**
- Verify input settings match your Input System
- Check if SphereCollider was automatically added
- Ensure player is within interaction range

**Timeline not loading scene?**
- Verify Signal Receiver is properly configured
- Check binding on SceneLoaderTrack
- Ensure Timeline is playing in Play Mode

**Fade not appearing?**
- Check if Canvas is being created (check Hierarchy during fade)
- Verify fade durations are greater than 0
- Ensure no other UI is blocking the fade canvas

---

## 📝 Example Scene Setup

```
GameManager (GameObject)
├─ SceneLoader
   ├─ Scene Name: "Level2"
   ├─ Use Fade: ✓
   ├─ Fade Duration: 0.5s

Door (GameObject)
├─ BoxCollider (IsTrigger: ✓)
├─ ProximitySceneTrigger
   ├─ Target Tag: "Player"
   ├─ Scene Loader: [Reference to GameManager's SceneLoader]

InteractivePortal (GameObject)
├─ SphereCollider (IsTrigger: ✓)
├─ InteractionSceneTrigger
   ├─ Interaction Key: E
   ├─ Show Prompt: ✓
   └─ Scene Loader: [Reference to GameManager's SceneLoader]
```

---

## 📄 License
Free to use in any project, commercial or personal.

---

## 🤝 Support
For issues or questions, check:
1. This documentation
2. Unity console for error messages
3. Verify all scene names and tags are correct
4. Ensure scenes are in Build Settings

Happy scene loading! 🎮
