using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;

/// <summary>
/// Enhanced menu controller for The Somnial Study.
/// Handles navigation, transitions, and settings.
/// </summary>
public class MenuController : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string gameSceneName = "Opening";
    [SerializeField] private float transitionDuration = 1.5f;

    [Header("UI References")]
    [SerializeField] private CanvasGroup mainMenuGroup;
    [SerializeField] private CanvasGroup settingsGroup;
    [SerializeField] private Image fadeOverlay;

    [Header("Audio")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip startGameSound;

    [Header("Title Animation")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private float titlePulseSpeed = 1f;
    [SerializeField] private float titlePulseAmount = 0.05f;

    [Header("Settings Panel Style")]
    [SerializeField] private Color settingsPanelColor = new Color(0.02f, 0.01f, 0.05f, 0.95f);
    [SerializeField] private Color settingsTextColor = new Color(0.9f, 0.85f, 1f, 1f);
    [SerializeField] private Color settingsButtonColor = new Color(0.9f, 0.85f, 1f, 1f);
    [SerializeField] private Color settingsButtonTextColor = new Color(0.15f, 0.1f, 0.2f, 1f);

    private bool isTransitioning = false;
    private Coroutine titleAnimationCoroutine;

    private void Start()
    {
        // Initialize UI state
        if (fadeOverlay != null)
        {
            fadeOverlay.color = new Color(0, 0, 0, 1);
            StartCoroutine(FadeIn());
        }

        // Auto-find components if not assigned
        AutoFindComponents();

        // Initialize settings group state
        if (settingsGroup != null)
        {
            settingsGroup.alpha = 0;
            settingsGroup.interactable = false;
            settingsGroup.blocksRaycasts = false;
        }

        // Start title animation
        if (titleText != null)
        {
            titleAnimationCoroutine = StartCoroutine(AnimateTitle());
        }
    }

    private void AutoFindComponents()
    {
        if (uiAudioSource == null)
        {
            uiAudioSource = GetComponent<AudioSource>();
            if (uiAudioSource == null)
            {
                uiAudioSource = gameObject.AddComponent<AudioSource>();
                uiAudioSource.playOnAwake = false;
            }
        }

        Canvas canvas = FindObjectOfType<Canvas>();

        if (fadeOverlay == null && canvas != null)
        {
            // Try to find or create fade overlay
            Transform existing = canvas.transform.Find("FadeOverlay");
            if (existing != null)
            {
                fadeOverlay = existing.GetComponent<Image>();
            }
            else
            {
                CreateFadeOverlay(canvas);
            }
        }

        if (titleText == null)
        {
            // Find title by looking for largest text
            TextMeshProUGUI[] texts = FindObjectsOfType<TextMeshProUGUI>();
            float maxSize = 0;
            foreach (var text in texts)
            {
                if (text.fontSize > maxSize)
                {
                    maxSize = text.fontSize;
                    titleText = text;
                }
            }
        }

        // Auto-find or create main menu group
        if (mainMenuGroup == null && canvas != null)
        {
            // Try to find existing main menu panel
            Transform mainMenuTransform = canvas.transform.Find("MainMenu");
            if (mainMenuTransform == null)
                mainMenuTransform = canvas.transform.Find("MainMenuPanel");
            if (mainMenuTransform == null)
                mainMenuTransform = canvas.transform.Find("MenuPanel");

            if (mainMenuTransform != null)
            {
                mainMenuGroup = mainMenuTransform.GetComponent<CanvasGroup>();
                if (mainMenuGroup == null)
                    mainMenuGroup = mainMenuTransform.gameObject.AddComponent<CanvasGroup>();
            }
            else
            {
                // Wrap existing buttons in a main menu group
                CreateMainMenuGroup(canvas);
            }
        }

        // Auto-create settings panel if not assigned
        if (settingsGroup == null && canvas != null)
        {
            Transform settingsTransform = canvas.transform.Find("SettingsPanel");
            if (settingsTransform != null)
            {
                settingsGroup = settingsTransform.GetComponent<CanvasGroup>();
            }
            else
            {
                CreateSettingsPanel(canvas);
            }
        }
    }

    private void CreateMainMenuGroup(Canvas canvas)
    {
        // Find all buttons that might be menu buttons
        Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
        if (buttons.Length == 0) return;

        // Find the common parent of menu buttons, or use the first button's parent
        Transform menuParent = buttons[0].transform.parent;

        // Add CanvasGroup to the parent if it doesn't have one
        mainMenuGroup = menuParent.GetComponent<CanvasGroup>();
        if (mainMenuGroup == null)
        {
            mainMenuGroup = menuParent.gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void CreateSettingsPanel(Canvas canvas)
    {
        // Create settings panel
        GameObject settingsPanel = new GameObject("SettingsPanel");
        settingsPanel.transform.SetParent(canvas.transform, false);

        // Setup RectTransform to fill screen
        RectTransform panelRT = settingsPanel.AddComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;

        // Add background image
        Image panelImage = settingsPanel.AddComponent<Image>();
        panelImage.color = settingsPanelColor;

        // Add CanvasGroup
        settingsGroup = settingsPanel.AddComponent<CanvasGroup>();
        settingsGroup.alpha = 0;
        settingsGroup.interactable = false;
        settingsGroup.blocksRaycasts = false;

        // Create content container
        GameObject content = new GameObject("Content");
        content.transform.SetParent(settingsPanel.transform, false);

        RectTransform contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0.5f, 0.5f);
        contentRT.anchorMax = new Vector2(0.5f, 0.5f);
        contentRT.sizeDelta = new Vector2(400, 350);
        contentRT.anchoredPosition = Vector2.zero;

        // Create title
        CreateSettingsText(content.transform, "SETTINGS", new Vector2(0, 120), 36, 6f);

        // Create frame rate setting
        CreateFrameRateSetting(content.transform, new Vector2(0, 40));

        // Create back button
        CreateSettingsButton(content.transform, "Back", new Vector2(0, -80), CloseSettings);

        // Add SettingsUI component for frame rate handling
        settingsPanel.AddComponent<SettingsUI>();
    }

    private void CreateSettingsText(Transform parent, string text, Vector2 position, float fontSize, float letterSpacing = 0)
    {
        GameObject textObj = new GameObject(text + "Text");
        textObj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = settingsTextColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.characterSpacing = letterSpacing;

        RectTransform rt = textObj.GetComponent<RectTransform>();
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(350, 50);
    }

    private void CreateFrameRateSetting(Transform parent, Vector2 position)
    {
        // Container for the frame rate row
        GameObject container = new GameObject("FrameRateSetting");
        container.transform.SetParent(parent, false);

        RectTransform containerRT = container.AddComponent<RectTransform>();
        containerRT.anchoredPosition = position;
        containerRT.sizeDelta = new Vector2(350, 40);

        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(container.transform, false);

        TextMeshProUGUI labelTMP = labelObj.AddComponent<TextMeshProUGUI>();
        labelTMP.text = "Frame Rate:";
        labelTMP.fontSize = 20;
        labelTMP.color = settingsTextColor;
        labelTMP.alignment = TextAlignmentOptions.MidlineLeft;

        RectTransform labelRT = labelObj.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0, 0);
        labelRT.anchorMax = new Vector2(0.5f, 1);
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;

        // Dropdown will be created by SettingsUI component
        // Create a placeholder that SettingsUI will use
        GameObject dropdownContainer = new GameObject("DropdownContainer");
        dropdownContainer.transform.SetParent(container.transform, false);

        RectTransform dropdownRT = dropdownContainer.AddComponent<RectTransform>();
        dropdownRT.anchorMin = new Vector2(0.5f, 0);
        dropdownRT.anchorMax = new Vector2(1, 1);
        dropdownRT.offsetMin = Vector2.zero;
        dropdownRT.offsetMax = Vector2.zero;
    }

    private void CreateSettingsButton(Transform parent, string text, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        GameObject btnObj = new GameObject(text + "Button");
        btnObj.transform.SetParent(parent, false);

        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = settingsButtonColor;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;
        btn.onClick.AddListener(action);

        // Color transitions
        ColorBlock colors = btn.colors;
        colors.normalColor = settingsButtonColor;
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.7f, 0.6f, 0.9f, 1f);
        colors.fadeDuration = 0.1f;
        btn.colors = colors;

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(200, 45);

        // Button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 22;
        tmp.color = settingsButtonTextColor;
        tmp.alignment = TextAlignmentOptions.Center;

        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
    }

    private void CreateFadeOverlay(Canvas canvas)
    {
        GameObject overlayObj = new GameObject("FadeOverlay");
        overlayObj.transform.SetParent(canvas.transform, false);
        overlayObj.transform.SetAsLastSibling();

        fadeOverlay = overlayObj.AddComponent<Image>();
        fadeOverlay.color = new Color(0, 0, 0, 0);
        fadeOverlay.raycastTarget = false;

        RectTransform rt = overlayObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private IEnumerator AnimateTitle()
    {
        if (titleText == null) yield break;

        float baseSize = titleText.fontSize;
        float time = 0;

        while (true)
        {
            time += Time.deltaTime * titlePulseSpeed;
            float pulse = 1f + Mathf.Sin(time) * titlePulseAmount;
            titleText.fontSize = baseSize * pulse;

            // Subtle color shift
            float hue = (Mathf.Sin(time * 0.5f) + 1f) * 0.02f;
            titleText.color = Color.HSVToRGB(0.7f + hue, 0.1f, 1f);

            yield return null;
        }
    }

    public void PlayGame()
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionToGame());
    }

    private IEnumerator TransitionToGame()
    {
        isTransitioning = true;

        PlaySound(startGameSound ?? clickSound);

        // Fade out
        yield return StartCoroutine(FadeOut());

        // Load scene
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenSettings()
    {
        if (isTransitioning) return;
        PlaySound(clickSound);
        StartCoroutine(ShowSettings());
    }

    public void CloseSettings()
    {
        if (isTransitioning) return;
        PlaySound(clickSound);
        StartCoroutine(HideSettings());
    }

    private IEnumerator ShowSettings()
    {
        if (settingsGroup == null || mainMenuGroup == null) yield break;

        float duration = 0.3f;
        float elapsed = 0;

        settingsGroup.gameObject.SetActive(true);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smooth = t * t * (3f - 2f * t); // Smoothstep

            mainMenuGroup.alpha = 1f - smooth;
            settingsGroup.alpha = smooth;

            yield return null;
        }

        mainMenuGroup.interactable = false;
        mainMenuGroup.blocksRaycasts = false;
        settingsGroup.interactable = true;
        settingsGroup.blocksRaycasts = true;
    }

    private IEnumerator HideSettings()
    {
        if (settingsGroup == null || mainMenuGroup == null) yield break;

        float duration = 0.3f;
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smooth = t * t * (3f - 2f * t);

            settingsGroup.alpha = 1f - smooth;
            mainMenuGroup.alpha = smooth;

            yield return null;
        }

        settingsGroup.interactable = false;
        settingsGroup.blocksRaycasts = false;
        mainMenuGroup.interactable = true;
        mainMenuGroup.blocksRaycasts = true;
    }

    public void QuitGame()
    {
        if (isTransitioning) return;
        PlaySound(clickSound);
        StartCoroutine(QuitWithFade());
    }

    private IEnumerator QuitWithFade()
    {
        isTransitioning = true;
        yield return StartCoroutine(FadeOut());

        Debug.Log("Quitting game...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / transitionDuration);
            fadeOverlay.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        fadeOverlay.color = new Color(0, 0, 0, 0);
    }

    private IEnumerator FadeOut()
    {
        if (fadeOverlay == null) yield break;

        float elapsed = 0;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = elapsed / transitionDuration;
            fadeOverlay.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        fadeOverlay.color = new Color(0, 0, 0, 1);
    }

    public void PlayHoverSound()
    {
        PlaySound(hoverSound);
    }

    public void PlayClickSound()
    {
        PlaySound(clickSound);
    }

    private void PlaySound(AudioClip clip)
    {
        if (uiAudioSource != null && clip != null)
        {
            uiAudioSource.PlayOneShot(clip, 0.5f);
        }
    }
}
