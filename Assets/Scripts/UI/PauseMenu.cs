using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using System.Collections;

/// <summary>
/// In-game pause menu that appears when pressing ESC.
/// Allows the player to exit to desktop.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string menuSceneName = "Menu";
    [SerializeField] private float fadeSpeed = 5f;

    [Header("Colors")]
    [SerializeField] private Color overlayColor = new Color(0.02f, 0.01f, 0.05f, 0.85f);
    [SerializeField] private Color buttonColor = new Color(0.9f, 0.85f, 1f, 1f);
    [SerializeField] private Color buttonTextColor = new Color(0.15f, 0.1f, 0.2f, 1f);
    [SerializeField] private Color titleColor = new Color(0.95f, 0.93f, 1f, 1f);

    private Canvas pauseCanvas;
    private CanvasGroup canvasGroup;
    private GameObject pausePanel;
    private bool isPaused = false;
    private bool isTransitioning = false;

    private InputSystemUIInputModule inputModule;

    private static PauseMenu instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("PauseMenu");
            instance = go.AddComponent<PauseMenu>();
            DontDestroyOnLoad(go);
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        CreatePauseUI();
    }

    private void Update()
    {
        // Don't show pause menu in the Menu scene itself
        if (SceneManager.GetActiveScene().name == menuSceneName)
        {
            if (isPaused) HidePauseMenu();
            return;
        }

        // Toggle pause on ESC
        if (Input.GetKeyDown(KeyCode.Escape) && !isTransitioning)
        {
            if (isPaused)
                HidePauseMenu();
            else
                ShowPauseMenu();
        }

        // Enforce cursor visibility while paused (prevent other scripts from hiding it)
        if (isPaused)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void EnsureEventSystem()
    {
        EventSystem existingES = FindObjectOfType<EventSystem>();
        if (existingES == null)
        {
            GameObject eventSystemObj = new GameObject("PauseMenuEventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
            DontDestroyOnLoad(eventSystemObj);
        }
    }

    private void CreatePauseUI()
    {
        // Ensure EventSystem exists (required for UI interaction)
        EnsureEventSystem();

        // Create canvas
        GameObject canvasObj = new GameObject("PauseCanvas");
        canvasObj.transform.SetParent(transform);

        pauseCanvas = canvasObj.AddComponent<Canvas>();
        pauseCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        pauseCanvas.sortingOrder = 1000; // Always on top

        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasObj.GetComponent<CanvasScaler>().matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // Create panel
        pausePanel = new GameObject("PausePanel");
        pausePanel.transform.SetParent(canvasObj.transform, false);

        Image panelImage = pausePanel.AddComponent<Image>();
        panelImage.color = overlayColor;

        RectTransform panelRT = pausePanel.GetComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;

        // Create content container
        GameObject content = new GameObject("Content");
        content.transform.SetParent(pausePanel.transform, false);

        RectTransform contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0.5f, 0.5f);
        contentRT.anchorMax = new Vector2(0.5f, 0.5f);
        contentRT.sizeDelta = new Vector2(400, 300);
        contentRT.anchoredPosition = Vector2.zero;

        // Title - "PAUSED"
        CreateText(content.transform, "PAUSED", new Vector2(0, 80), 42, titleColor, 8f);

        // Resume button
        CreateButton(content.transform, "Resume", new Vector2(0, 0), ResumeGame);

        // Exit to Desktop button
        CreateButton(content.transform, "Exit to Desktop", new Vector2(0, -70), ExitToDesktop);
    }

    private void CreateText(Transform parent, string text, Vector2 position, float fontSize, Color color, float letterSpacing = 0)
    {
        GameObject textObj = new GameObject(text + "Text");
        textObj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.characterSpacing = letterSpacing;

        RectTransform rt = textObj.GetComponent<RectTransform>();
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(350, 60);
    }

    private void CreateButton(Transform parent, string text, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        GameObject btnObj = new GameObject(text + "Button");
        btnObj.transform.SetParent(parent, false);

        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = buttonColor;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;
        btn.onClick.AddListener(action);

        // Color transitions
        ColorBlock colors = btn.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.7f, 0.6f, 0.9f, 1f);
        colors.fadeDuration = 0.1f;
        btn.colors = colors;

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(260, 50);

        // Button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 24;
        tmp.color = buttonTextColor;
        tmp.alignment = TextAlignmentOptions.Center;

        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        // Add hover effect
        btnObj.AddComponent<PauseButtonEffect>();
    }

    private void ShowPauseMenu()
    {
        isPaused = true;
        Time.timeScale = 0f; // Pause game

        // Ensure EventSystem exists (may have been destroyed on scene change)
        EnsureEventSystem();

        // Disable Input System UI module so its cursor lock behavior doesn't fight us
        inputModule = FindObjectOfType<InputSystemUIInputModule>();
        if (inputModule != null)
        {
            inputModule.enabled = false;
        }

        // Immediately make UI visible and interactable
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        // Force cursor to be visible and unlocked immediately
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void SetCursorVisible(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void HidePauseMenu()
    {
        isPaused = false;
        Time.timeScale = 1f; // Resume game

        // Re-enable Input System UI module
        if (inputModule != null)
        {
            inputModule.enabled = true;
        }

        // Immediately hide UI
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        // Re-lock cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ResumeGame()
    {
        HidePauseMenu();
    }

    public void ExitToDesktop()
    {
        if (isTransitioning) return;
        StartCoroutine(ExitWithFade());
    }

    private IEnumerator ExitWithFade()
    {
        isTransitioning = true;
        Time.timeScale = 1f; // Restore time for fade animation

        // Fade to black
        Image overlay = pausePanel.GetComponent<Image>();
        float elapsed = 0;
        float duration = 0.5f;
        Color startColor = overlay.color;
        Color endColor = new Color(0, 0, 0, 1);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            overlay.color = Color.Lerp(startColor, endColor, elapsed / duration);
            yield return null;
        }

        Debug.Log("Exiting to desktop...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}

/// <summary>
/// Simple hover effect for pause menu buttons.
/// </summary>
public class PauseButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private RectTransform rectTransform;
    private Vector3 originalScale;
    private float targetScale = 1f;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
    }

    private void Update()
    {
        float current = rectTransform.localScale.x / originalScale.x;
        float newScale = Mathf.Lerp(current, targetScale, Time.unscaledDeltaTime * 8f);
        rectTransform.localScale = originalScale * newScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = 1.05f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = 1f;
    }
}
