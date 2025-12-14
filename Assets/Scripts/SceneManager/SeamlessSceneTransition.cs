using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SceneManagement
{
    /// <summary>
    /// Handles seamless scene transitions with preloading and smooth fades
    /// Use this for Timeline signals to avoid loading pauses
    /// </summary>
    public class SeamlessSceneTransition : MonoBehaviour
    {
        [Header("Next Scene")]
        [Tooltip("The scene to transition to")]
#if UNITY_EDITOR
        [SerializeField] private SceneAsset nextSceneAsset;
#endif
        [SerializeField] private string nextSceneName;

        [Header("Preload Settings")]
        [Tooltip("When to start preloading the next scene")]
        [SerializeField] private PreloadTiming preloadTiming = PreloadTiming.OnStart;

        [Tooltip("Delay before starting preload (if using OnStart)")]
        [SerializeField] private float preloadDelay = 2f;

        [Header("Transition Settings")]
        [Tooltip("Use fade transition when switching scenes")]
        [SerializeField] private bool useFade = true;

        [Tooltip("Fade out duration")]
        [SerializeField] private float fadeOutDuration = 0.5f;

        [Tooltip("Fade in duration")]
        [SerializeField] private float fadeInDuration = 0.5f;

        [SerializeField] private Color fadeColor = Color.black;

        [Header("Auto Transition")]
        [Tooltip("Automatically transition when scene is ready")]
        [SerializeField] private bool autoTransition = false;

        [Tooltip("Wait this long after preload before auto-transitioning")]
        [SerializeField] private float autoTransitionDelay = 1f;

        private GameObject fadeCanvas;
        private CanvasGroup fadeCanvasGroup;
        private bool isTransitioning = false;

        public enum PreloadTiming
        {
            OnStart,        // Preload when this scene starts
            OnEnable,       // Preload when component is enabled
            Manual          // Call PreloadNextScene() manually
        }

        private void OnEnable()
        {
            if (preloadTiming == PreloadTiming.OnEnable)
            {
                PreloadNextScene();
            }
        }

        private void Start()
        {
            if (preloadTiming == PreloadTiming.OnStart)
            {
                if (preloadDelay > 0)
                {
                    StartCoroutine(PreloadAfterDelay());
                }
                else
                {
                    PreloadNextScene();
                }
            }
        }

        /// <summary>
        /// Preload the next scene in the background (call this early!)
        /// </summary>
        public void PreloadNextScene()
        {
            if (string.IsNullOrEmpty(nextSceneName))
            {
                Debug.LogError($"Next scene name is empty on {gameObject.name}!");
                return;
            }

            ScenePreloader.Instance.PreloadScene(nextSceneName);
            Debug.Log($"[SeamlessTransition] Started preloading: {nextSceneName}");

            if (autoTransition)
            {
                StartCoroutine(AutoTransitionWhenReady());
            }
        }

        /// <summary>
        /// Transition to the preloaded scene instantly (call from Timeline Signal)
        /// </summary>
        public void TransitionToNextScene()
        {
            if (isTransitioning)
            {
                Debug.LogWarning("Already transitioning!");
                return;
            }

            if (string.IsNullOrEmpty(nextSceneName))
            {
                Debug.LogError($"Next scene name is empty on {gameObject.name}!");
                return;
            }

            StartCoroutine(TransitionCoroutine());
        }

        /// <summary>
        /// Check if the next scene is ready to activate
        /// </summary>
        public bool IsNextSceneReady()
        {
            return ScenePreloader.Instance.IsSceneReady(nextSceneName);
        }

        /// <summary>
        /// Get loading progress of next scene (0-1)
        /// </summary>
        public float GetLoadProgress()
        {
            return ScenePreloader.Instance.GetSceneLoadProgress(nextSceneName);
        }

        private IEnumerator PreloadAfterDelay()
        {
            yield return new WaitForSeconds(preloadDelay);
            PreloadNextScene();
        }

        private IEnumerator AutoTransitionWhenReady()
        {
            // Wait until scene is ready
            while (!IsNextSceneReady())
            {
                yield return new WaitForSeconds(0.1f);
            }

            yield return new WaitForSeconds(autoTransitionDelay);
            TransitionToNextScene();
        }

        private IEnumerator TransitionCoroutine()
        {
            isTransitioning = true;

            // Fade out if enabled
            if (useFade)
            {
                yield return StartCoroutine(FadeOut());
            }

            // Check if scene is ready
            if (!IsNextSceneReady())
            {
                Debug.LogWarning($"Scene '{nextSceneName}' is not fully preloaded yet! Waiting...");

                // Show progress while waiting
                while (!IsNextSceneReady())
                {
                    float progress = GetLoadProgress();
                    Debug.Log($"Loading progress: {progress:P0}");
                    yield return new WaitForSeconds(0.1f);
                }
            }

            // Activate the preloaded scene (this is instant!)
            Debug.Log($"[SeamlessTransition] Activating preloaded scene: {nextSceneName}");
            ScenePreloader.Instance.ActivatePreloadedScene(nextSceneName);

            // Wait one frame for scene to activate
            yield return null;

            // Fade in if enabled
            if (useFade)
            {
                yield return StartCoroutine(FadeIn());
            }

            isTransitioning = false;
        }

        private IEnumerator FadeOut()
        {
            CreateFadeCanvas();
            float elapsed = 0f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / fadeOutDuration);
                yield return null;
            }

            fadeCanvasGroup.alpha = 1;
        }

        private IEnumerator FadeIn()
        {
            if (fadeCanvas == null) CreateFadeCanvas();

            float elapsed = 0f;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(1, 0, elapsed / fadeInDuration);
                yield return null;
            }

            fadeCanvasGroup.alpha = 0;
            Destroy(fadeCanvas);
        }

        private void CreateFadeCanvas()
        {
            if (fadeCanvas != null) return;

            // Create canvas
            fadeCanvas = new GameObject("SeamlessFadeCanvas");
            DontDestroyOnLoad(fadeCanvas);

            Canvas canvas = fadeCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;

            fadeCanvas.AddComponent<UnityEngine.UI.CanvasScaler>();
            fadeCanvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Create image
            GameObject imageObject = new GameObject("FadeImage");
            imageObject.transform.SetParent(fadeCanvas.transform, false);

            UnityEngine.UI.Image image = imageObject.AddComponent<UnityEngine.UI.Image>();
            image.color = fadeColor;

            RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;

            // Add canvas group
            fadeCanvasGroup = fadeCanvas.AddComponent<CanvasGroup>();
            fadeCanvasGroup.alpha = 0;
            fadeCanvasGroup.blocksRaycasts = true;
            fadeCanvasGroup.interactable = false;
        }

        private void OnDestroy()
        {
            if (fadeCanvas != null)
            {
                Destroy(fadeCanvas);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-update scene name from asset
            if (nextSceneAsset != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(nextSceneAsset);
                nextSceneName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            }
        }
#endif
    }
}
