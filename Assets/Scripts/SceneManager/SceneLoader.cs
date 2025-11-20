using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SceneManagement
{
    /// <summary>
    /// Main scene loader that can be triggered through various methods
    /// Works with or without Timeline
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        [Header("Scene Settings")]
        [Tooltip("Drag a scene asset here, or manually type the scene name below")]
#if UNITY_EDITOR
        [SerializeField] private SceneAsset sceneAsset;
#endif
        [SerializeField] private string sceneName;
        [SerializeField] private LoadSceneMode loadMode = LoadSceneMode.Single;

        [Header("Loading Settings")]
        [SerializeField] private bool useAsyncLoading = true;
        [SerializeField] private float minimumLoadTime = 0.5f;
        [SerializeField] private bool showLoadingScreen = false;
        [SerializeField] private GameObject loadingScreenPrefab;

        [Header("Fade Settings")]
        [SerializeField] private bool useFade = true;
        [SerializeField] private float fadeOutDuration = 0.5f;
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private Color fadeColor = Color.black;

        [Header("Trigger Settings")]
        [SerializeField] private bool loadOnStart = false;
        [SerializeField] private float delayBeforeLoad = 0f;

        [Header("Events")]
        public UnityEngine.Events.UnityEvent OnLoadStart;
        public UnityEngine.Events.UnityEvent OnLoadComplete;

        private static SceneLoader activeLoader;
        private GameObject fadeCanvas;
        private CanvasGroup fadeCanvasGroup;
        private bool isLoading = false;

        private void Start()
        {
            if (loadOnStart)
            {
                LoadScene();
            }
        }

        /// <summary>
        /// Main method to load the scene - can be called from anywhere
        /// </summary>
        public void LoadScene()
        {
            if (isLoading)
            {
                Debug.LogWarning("Scene is already loading!");
                return;
            }

            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("No scene specified! Set the scene name or drag a scene asset.");
                return;
            }

            StartCoroutine(LoadSceneCoroutine());
        }

        /// <summary>
        /// Load scene with a specific delay
        /// </summary>
        public void LoadSceneWithDelay(float delay)
        {
            delayBeforeLoad = delay;
            LoadScene();
        }

        /// <summary>
        /// Reload the current scene
        /// </summary>
        public void ReloadCurrentScene()
        {
            sceneName = SceneManager.GetActiveScene().name;
            LoadScene();
        }

        /// <summary>
        /// Load scene by name (can be called from UnityEvents or Timeline)
        /// </summary>
        public void LoadSceneByName(string name)
        {
            sceneName = name;
            LoadScene();
        }

        private IEnumerator LoadSceneCoroutine()
        {
            isLoading = true;
            activeLoader = this;

            // Invoke start event
            OnLoadStart?.Invoke();

            // Wait for delay
            if (delayBeforeLoad > 0)
            {
                yield return new WaitForSeconds(delayBeforeLoad);
            }

            // Fade out
            if (useFade)
            {
                yield return StartCoroutine(FadeOut());
            }

            // Show loading screen
            GameObject loadingScreen = null;
            if (showLoadingScreen && loadingScreenPrefab != null)
            {
                loadingScreen = Instantiate(loadingScreenPrefab);
                DontDestroyOnLoad(loadingScreen);
            }

            // Load scene
            if (useAsyncLoading)
            {
                yield return StartCoroutine(LoadSceneAsync());
            }
            else
            {
                LoadSceneImmediate();
            }

            // Hide loading screen
            if (loadingScreen != null)
            {
                Destroy(loadingScreen);
            }

            // Fade in
            if (useFade)
            {
                yield return StartCoroutine(FadeIn());
            }

            // Invoke complete event
            OnLoadComplete?.Invoke();

            isLoading = false;
        }

        private IEnumerator LoadSceneAsync()
        {
            float startTime = Time.time;

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, loadMode);
            asyncLoad.allowSceneActivation = false;

            // Wait until scene is loaded
            while (!asyncLoad.isDone)
            {
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

                // Check if minimum load time has passed and scene is ready
                if (asyncLoad.progress >= 0.9f && (Time.time - startTime) >= minimumLoadTime)
                {
                    asyncLoad.allowSceneActivation = true;
                }

                yield return null;
            }
        }

        private void LoadSceneImmediate()
        {
            SceneManager.LoadScene(sceneName, loadMode);
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
            fadeCanvas = new GameObject("FadeCanvas");
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
            fadeCanvasGroup.blocksRaycasts = false;
        }

        /// <summary>
        /// Static method to load any scene from anywhere
        /// </summary>
        public static void LoadSceneStatic(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            GameObject tempLoader = new GameObject("TempSceneLoader");
            SceneLoader loader = tempLoader.AddComponent<SceneLoader>();
            loader.sceneName = sceneName;
            loader.loadMode = mode;
            loader.LoadScene();
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
            // Automatically update scene name when a scene asset is dragged in
            if (sceneAsset != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(sceneAsset);
                sceneName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            }
        }
#endif
    }
}