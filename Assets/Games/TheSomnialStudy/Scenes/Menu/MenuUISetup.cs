using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Automatically sets up and enhances the menu UI at runtime.
/// Attach this to a GameObject in the Menu scene to auto-configure everything.
/// </summary>
public class MenuUISetup : MonoBehaviour
{
    [Header("Theme Colors")]
    [SerializeField] private Color primaryColor = new Color(0.7f, 0.6f, 0.9f, 1f);
    [SerializeField] private Color secondaryColor = new Color(0.9f, 0.85f, 1f, 1f);
    [SerializeField] private Color backgroundColor = new Color(0.05f, 0.03f, 0.1f, 1f);
    [SerializeField] private Color textColor = new Color(0.95f, 0.93f, 1f, 1f);
    [SerializeField] private Color buttonTextColor = new Color(0.15f, 0.1f, 0.2f, 1f);

    [Header("Typography")]
    [SerializeField] private float titleSize = 48f;
    [SerializeField] private float buttonTextSize = 28f;
    [SerializeField] private float letterSpacing = 8f;

    [Header("Button Styling")]
    [SerializeField] private Vector2 buttonSize = new Vector2(220f, 50f);
    [SerializeField] private float buttonSpacing = 15f;
    [SerializeField] private float buttonCornerRadius = 8f;

    [Header("Layout")]
    [SerializeField] private float titleYPosition = 180f;
    [SerializeField] private float buttonsStartY = 40f;

    private Canvas canvas;
    private MenuController menuController;

    private void Awake()
    {
        SetupMenu();
    }

    private void SetupMenu()
    {
        canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[MenuUISetup] No Canvas found!");
            return;
        }

        // Add core components
        SetupMenuController();
        SetupBackground();
        EnhanceTitle();
        EnhanceButtons();
        SetupCanvasScaler();
    }

    private void SetupMenuController()
    {
        menuController = FindObjectOfType<MenuController>();
        if (menuController == null)
        {
            // Find existing Menu script and add MenuController alongside it
            Menu oldMenu = FindObjectOfType<Menu>();
            if (oldMenu != null)
            {
                menuController = oldMenu.gameObject.AddComponent<MenuController>();
            }
            else
            {
                GameObject menuObj = new GameObject("MenuController");
                menuController = menuObj.AddComponent<MenuController>();
            }
        }

        // Add audio source if needed
        if (menuController.GetComponent<AudioSource>() == null)
        {
            AudioSource audio = menuController.gameObject.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 0f;
        }
    }

    private void SetupBackground()
    {
        // Add background effect if not present
        if (FindObjectOfType<MenuBackground>() == null)
        {
            GameObject bgObj = new GameObject("MenuBackgroundEffect");
            MenuBackground bg = bgObj.AddComponent<MenuBackground>();
        }
    }

    private void EnhanceTitle()
    {
        // Find title text (usually the largest text)
        TextMeshProUGUI[] texts = FindObjectsOfType<TextMeshProUGUI>();
        TextMeshProUGUI titleText = null;
        float maxSize = 0;

        foreach (var text in texts)
        {
            if (text.fontSize > maxSize && !IsButtonText(text))
            {
                maxSize = text.fontSize;
                titleText = text;
            }
        }

        if (titleText != null)
        {
            // Enhance title styling
            titleText.fontSize = titleSize;
            titleText.color = textColor;
            titleText.characterSpacing = letterSpacing;
            titleText.fontStyle = FontStyles.Normal;
            titleText.alignment = TextAlignmentOptions.Center;

            // Position title
            RectTransform rt = titleText.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, titleYPosition);

            // Add subtle shadow
            Shadow shadow = titleText.GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = titleText.gameObject.AddComponent<Shadow>();
            }
            shadow.effectColor = new Color(0.4f, 0.3f, 0.6f, 0.5f);
            shadow.effectDistance = new Vector2(2, -2);

            Debug.Log($"[MenuUISetup] Enhanced title: {titleText.text}");
        }
    }

    private bool IsButtonText(TextMeshProUGUI text)
    {
        return text.GetComponentInParent<Button>() != null;
    }

    private void EnhanceButtons()
    {
        Button[] buttons = FindObjectsOfType<Button>();

        // Sort buttons by priority: Play/Start first, Settings second, Quit last
        System.Array.Sort(buttons, (a, b) => GetButtonPriority(a) - GetButtonPriority(b));

        int buttonIndex = 0;
        foreach (var button in buttons)
        {
            EnhanceButton(button, buttonIndex);
            WireButtonToController(button);
            buttonIndex++;
        }
    }

    private int GetButtonPriority(Button button)
    {
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        string name = buttonText?.text?.ToLower() ?? button.name.ToLower();

        // Play/Start should be first (priority 0)
        if (name.Contains("play") || name.Contains("start") || name.Contains("begin"))
            return 0;
        // Settings/Options second (priority 1)
        if (name.Contains("setting") || name.Contains("option"))
            return 1;
        // Back/Close buttons (priority 2)
        if (name.Contains("back") || name.Contains("close"))
            return 2;
        // Quit/Exit last (priority 3)
        if (name.Contains("quit") || name.Contains("exit"))
            return 3;
        // Unknown buttons in the middle (priority 1)
        return 1;
    }

    private void EnhanceButton(Button button, int index)
    {
        // Get components
        Image buttonImage = button.GetComponent<Image>();
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        RectTransform rt = button.GetComponent<RectTransform>();

        // Resize button
        rt.sizeDelta = buttonSize;

        // Position buttons in a vertical layout
        float yPos = buttonsStartY - (index * (buttonSize.y + buttonSpacing));
        rt.anchoredPosition = new Vector2(0, yPos);

        // Style button image
        if (buttonImage != null)
        {
            buttonImage.color = secondaryColor;

            // Set up color transitions
            ColorBlock colors = button.colors;
            colors.normalColor = secondaryColor;
            colors.highlightedColor = Color.white;
            colors.pressedColor = primaryColor;
            colors.selectedColor = secondaryColor;
            colors.fadeDuration = 0.15f;
            button.colors = colors;
        }

        // Style button text
        if (buttonText != null)
        {
            buttonText.fontSize = buttonTextSize;
            buttonText.color = buttonTextColor;
            buttonText.characterSpacing = 2f;
            buttonText.fontStyle = FontStyles.Normal;
            buttonText.alignment = TextAlignmentOptions.Center;
        }

        // Add hover effect if not present
        if (button.GetComponent<MenuButtonEffect>() == null)
        {
            button.gameObject.AddComponent<MenuButtonEffect>();
        }

        Debug.Log($"[MenuUISetup] Enhanced button: {buttonText?.text ?? button.name}");
    }

    private void WireButtonToController(Button button)
    {
        if (menuController == null) return;

        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        string buttonName = buttonText?.text?.ToLower() ?? button.name.ToLower();

        // Clear existing listeners and add new ones
        button.onClick.RemoveAllListeners();

        if (buttonName.Contains("play") || buttonName.Contains("start") || buttonName.Contains("begin"))
        {
            button.onClick.AddListener(menuController.PlayGame);
            Debug.Log($"[MenuUISetup] Wired '{buttonName}' to PlayGame()");
        }
        else if (buttonName.Contains("setting") || buttonName.Contains("option"))
        {
            button.onClick.AddListener(menuController.OpenSettings);
            Debug.Log($"[MenuUISetup] Wired '{buttonName}' to OpenSettings()");
        }
        else if (buttonName.Contains("quit") || buttonName.Contains("exit"))
        {
            button.onClick.AddListener(menuController.QuitGame);
            Debug.Log($"[MenuUISetup] Wired '{buttonName}' to QuitGame()");
        }
        else if (buttonName.Contains("back") || buttonName.Contains("close"))
        {
            button.onClick.AddListener(menuController.CloseSettings);
            Debug.Log($"[MenuUISetup] Wired '{buttonName}' to CloseSettings()");
        }
    }

    private void SetupCanvasScaler()
    {
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }
    }

    /// <summary>
    /// Call this from a button to create a simple settings panel.
    /// </summary>
    public void CreateSettingsPanel()
    {
        if (canvas == null) return;

        // Check if settings panel already exists
        Transform existing = canvas.transform.Find("SettingsPanel");
        if (existing != null) return;

        // Create settings panel
        GameObject panel = new GameObject("SettingsPanel");
        panel.transform.SetParent(canvas.transform, false);

        // Add CanvasGroup for fade
        CanvasGroup cg = panel.AddComponent<CanvasGroup>();
        cg.alpha = 0;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        // Background
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.03f, 0.1f, 0.95f);

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Title
        GameObject titleObj = new GameObject("SettingsTitle");
        titleObj.transform.SetParent(panel.transform, false);
        TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "SETTINGS";
        titleTMP.fontSize = 36;
        titleTMP.color = textColor;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.characterSpacing = 8f;

        RectTransform titleRT = titleObj.GetComponent<RectTransform>();
        titleRT.anchoredPosition = new Vector2(0, 150);
        titleRT.sizeDelta = new Vector2(400, 60);

        // Back button
        CreateSettingsButton(panel.transform, "Back", new Vector2(0, -150), menuController.CloseSettings);

        // Assign to controller
        if (menuController != null)
        {
            // Use reflection or serialized field to assign
            // For now, the controller will find it by name
        }

        Debug.Log("[MenuUISetup] Created Settings Panel");
    }

    private void CreateSettingsButton(Transform parent, string text, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        GameObject btnObj = new GameObject(text + "Button");
        btnObj.transform.SetParent(parent, false);

        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = secondaryColor;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;
        btn.onClick.AddListener(action);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchoredPosition = position;
        rt.sizeDelta = buttonSize;

        // Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = buttonTextSize;
        tmp.color = buttonTextColor;
        tmp.alignment = TextAlignmentOptions.Center;

        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        // Add effect
        btnObj.AddComponent<MenuButtonEffect>();
    }
}
