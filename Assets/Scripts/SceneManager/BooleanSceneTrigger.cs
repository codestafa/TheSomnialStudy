using UnityEngine;

namespace SceneManagement
{
    /// <summary>
    /// Triggers scene loading when a boolean condition becomes true
    /// Can be used for quest completion, game state changes, etc.
    /// </summary>
    public class BooleanSceneTrigger : MonoBehaviour
    {
        [Header("Condition Settings")]
        [SerializeField] private bool triggerCondition = false;
        [SerializeField] private bool checkEveryFrame = false;
        [SerializeField] private float checkInterval = 0.5f;

        [Header("Scene Loader Reference")]
        [SerializeField] private SceneLoader sceneLoader;

        [Header("Optional: Direct Scene Settings")]
        [SerializeField] private bool useDirectLoading = false;
        [SerializeField] private string sceneName;

        [Header("Trigger Settings")]
        [SerializeField] private float delayAfterTrigger = 0f;
        [SerializeField] private bool triggerOnce = true;

        private bool hasTriggered = false;
        private float lastCheckTime = 0f;

        private void Awake()
        {
            // If no scene loader is assigned, try to find one on this GameObject
            if (sceneLoader == null)
            {
                sceneLoader = GetComponent<SceneLoader>();
            }
        }

        private void Update()
        {
            if (hasTriggered && triggerOnce)
                return;

            if (checkEveryFrame)
            {
                CheckCondition();
            }
            else if (Time.time - lastCheckTime >= checkInterval)
            {
                CheckCondition();
                lastCheckTime = Time.time;
            }
        }

        private void CheckCondition()
        {
            if (triggerCondition)
            {
                TriggerSceneLoad();
            }
        }

        /// <summary>
        /// Set the trigger condition from external scripts
        /// </summary>
        public void SetTriggerCondition(bool value)
        {
            triggerCondition = value;

            if (value && !checkEveryFrame)
            {
                CheckCondition();
            }
        }

        /// <summary>
        /// Immediately trigger scene load
        /// </summary>
        public void TriggerSceneLoad()
        {
            if (hasTriggered && triggerOnce)
                return;

            hasTriggered = true;

            if (delayAfterTrigger > 0)
            {
                Invoke(nameof(LoadScene), delayAfterTrigger);
            }
            else
            {
                LoadScene();
            }
        }

        private void LoadScene()
        {
            if (useDirectLoading && !string.IsNullOrEmpty(sceneName))
            {
                SceneLoader.LoadSceneStatic(sceneName);
            }
            else if (sceneLoader != null)
            {
                sceneLoader.LoadScene();
            }
            else
            {
                Debug.LogError($"No SceneLoader assigned and useDirectLoading is false on {gameObject.name}!");
            }
        }

        /// <summary>
        /// Reset the trigger so it can be activated again
        /// </summary>
        public void ResetTrigger()
        {
            hasTriggered = false;
            triggerCondition = false;
        }

        /// <summary>
        /// Enable automatic checking
        /// </summary>
        public void EnableAutoCheck(bool everyFrame = false)
        {
            checkEveryFrame = everyFrame;
        }

        /// <summary>
        /// Disable automatic checking
        /// </summary>
        public void DisableAutoCheck()
        {
            checkEveryFrame = false;
        }
    }
}
