using UnityEngine;
using SceneManagement;

/// <summary>
/// Example controller showing various ways to use the scene management system
/// Attach this to a GameObject to see different loading methods in action
/// </summary>
public class ExampleSceneController : MonoBehaviour
{
    [Header("Scene Loader References")]
    [SerializeField] private SceneLoader mainSceneLoader;
    [SerializeField] private ProximitySceneTrigger proximityTrigger;
    [SerializeField] private InteractionSceneTrigger interactionTrigger;
    [SerializeField] private BooleanSceneTrigger booleanTrigger;

    [Header("Example Settings")]
    [SerializeField] private string exampleSceneName = "Level2";
    [SerializeField] private bool showDebugLogs = true;

    private void Start()
    {
        // Subscribe to scene loader events
        if (mainSceneLoader != null)
        {
            mainSceneLoader.OnLoadStart.AddListener(OnSceneLoadStart);
            mainSceneLoader.OnLoadComplete.AddListener(OnSceneLoadComplete);
        }

        DebugLog("ExampleSceneController initialized. Use the methods below for different loading types.");
    }

    #region Direct Loading Examples

    /// <summary>
    /// Example 1: Load scene directly via SceneLoader
    /// Usage: Call from UI button or script
    /// </summary>
    public void Example_LoadSceneDirectly()
    {
        DebugLog("Loading scene directly via SceneLoader...");
        
        if (mainSceneLoader != null)
        {
            mainSceneLoader.LoadSceneByName(exampleSceneName);
        }
        else
        {
            DebugLog("No SceneLoader assigned!", true);
        }
    }

    /// <summary>
    /// Example 2: Load scene with delay
    /// Usage: Useful for timed transitions
    /// </summary>
    public void Example_LoadSceneWithDelay()
    {
        DebugLog("Loading scene with 2 second delay...");
        
        if (mainSceneLoader != null)
        {
            mainSceneLoader.LoadSceneWithDelay(2f);
        }
    }

    /// <summary>
    /// Example 3: Reload current scene
    /// Usage: Game over, try again, reset level
    /// </summary>
    public void Example_ReloadCurrentScene()
    {
        DebugLog("Reloading current scene...");
        
        if (mainSceneLoader != null)
        {
            mainSceneLoader.ReloadCurrentScene();
        }
    }

    /// <summary>
    /// Example 4: Quick static load
    /// Usage: When you don't have a SceneLoader reference
    /// </summary>
    public void Example_QuickStaticLoad()
    {
        DebugLog("Using static loader method...");
        SceneLoader.LoadSceneStatic(exampleSceneName);
    }

    #endregion

    #region Trigger Examples

    /// <summary>
    /// Example 5: Reset proximity trigger
    /// Usage: Allow trigger to activate again
    /// </summary>
    public void Example_ResetProximityTrigger()
    {
        DebugLog("Resetting proximity trigger...");
        
        if (proximityTrigger != null)
        {
            proximityTrigger.ResetTrigger();
        }
    }

    /// <summary>
    /// Example 6: Manual interaction trigger
    /// Usage: Trigger interaction from code
    /// </summary>
    public void Example_TriggerInteraction()
    {
        DebugLog("Manually triggering interaction...");
        
        if (interactionTrigger != null)
        {
            interactionTrigger.Interact();
        }
    }

    /// <summary>
    /// Example 7: Set boolean trigger condition
    /// Usage: Load scene based on game state
    /// </summary>
    public void Example_SetBooleanCondition(bool value)
    {
        DebugLog($"Setting boolean trigger condition to: {value}");
        
        if (booleanTrigger != null)
        {
            booleanTrigger.SetTriggerCondition(value);
        }
    }

    /// <summary>
    /// Example 8: Immediately trigger boolean load
    /// Usage: Bypass condition checking
    /// </summary>
    public void Example_TriggerBooleanImmediately()
    {
        DebugLog("Triggering boolean scene load immediately...");
        
        if (booleanTrigger != null)
        {
            booleanTrigger.TriggerSceneLoad();
        }
    }

    #endregion

    #region Advanced Examples

    /// <summary>
    /// Example 9: Load scene based on quest completion
    /// Usage: Game progression system
    /// </summary>
    public void Example_LoadSceneOnQuestComplete()
    {
        // Simulate quest completion
        bool questCompleted = CheckQuestStatus(); // Your quest logic here
        
        if (questCompleted)
        {
            DebugLog("Quest completed! Loading reward scene...");
            
            if (booleanTrigger != null)
            {
                booleanTrigger.SetTriggerCondition(true);
            }
        }
        else
        {
            DebugLog("Quest not yet completed.");
        }
    }

    /// <summary>
    /// Example 10: Load scene after timer
    /// Usage: Auto-transition after time period
    /// </summary>
    public void Example_LoadSceneAfterTimer(float seconds)
    {
        DebugLog($"Scene will load in {seconds} seconds...");
        Invoke(nameof(Example_LoadSceneDirectly), seconds);
    }

    /// <summary>
    /// Example 11: Conditional scene loading
    /// Usage: Different scenes based on player choice
    /// </summary>
    public void Example_LoadSceneBasedOnChoice(int choice)
    {
        string sceneToLoad = "";
        
        switch (choice)
        {
            case 1:
                sceneToLoad = "GoodEnding";
                break;
            case 2:
                sceneToLoad = "BadEnding";
                break;
            case 3:
                sceneToLoad = "SecretEnding";
                break;
            default:
                DebugLog("Invalid choice!", true);
                return;
        }
        
        DebugLog($"Loading scene based on choice {choice}: {sceneToLoad}");
        SceneLoader.LoadSceneStatic(sceneToLoad);
    }

    /// <summary>
    /// Example 12: Chain scene loads
    /// Usage: Load multiple scenes in sequence
    /// </summary>
    public void Example_ChainSceneLoads()
    {
        DebugLog("Starting scene chain...");
        StartCoroutine(ChainLoadCoroutine());
    }

    private System.Collections.IEnumerator ChainLoadCoroutine()
    {
        DebugLog("Loading first scene...");
        // In real implementation, you'd actually load scenes
        // This is just a demonstration
        yield return new WaitForSeconds(2f);
        
        DebugLog("Loading second scene...");
        yield return new WaitForSeconds(2f);
        
        DebugLog("Chain complete!");
    }

    #endregion

    #region Event Callbacks

    private void OnSceneLoadStart()
    {
        DebugLog("=== SCENE LOAD STARTED ===");
        // Add your logic here: pause game, save progress, etc.
    }

    private void OnSceneLoadComplete()
    {
        DebugLog("=== SCENE LOAD COMPLETED ===");
        // Add your logic here: initialize new scene, resume game, etc.
    }

    #endregion

    #region Helper Methods

    private bool CheckQuestStatus()
    {
        // This is where you'd check your actual quest system
        // For example purposes, we'll return a random value
        return Random.value > 0.5f;
    }

    private void DebugLog(string message, bool isError = false)
    {
        if (!showDebugLogs) return;

        if (isError)
        {
            Debug.LogError($"[ExampleSceneController] {message}");
        }
        else
        {
            Debug.Log($"[ExampleSceneController] {message}");
        }
    }

    #endregion

    #region Testing Shortcuts (Editor Only)

#if UNITY_EDITOR
    private void Update()
    {
        // Press number keys to test different methods
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Example_LoadSceneDirectly();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Example_LoadSceneWithDelay();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Example_ReloadCurrentScene();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Example_SetBooleanCondition(true);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            Example_LoadSceneAfterTimer(3f);
        }
    }

    private void OnValidate()
    {
        // Try to auto-find components if not assigned
        if (mainSceneLoader == null)
        {
            mainSceneLoader = GetComponent<SceneLoader>();
        }
        if (proximityTrigger == null)
        {
            proximityTrigger = GetComponent<ProximitySceneTrigger>();
        }
        if (interactionTrigger == null)
        {
            interactionTrigger = GetComponent<InteractionSceneTrigger>();
        }
        if (booleanTrigger == null)
        {
            booleanTrigger = GetComponent<BooleanSceneTrigger>();
        }
    }
#endif

    #endregion
}
