using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// UI component for game settings. Attach to a settings panel.
/// Can use existing UI references or create UI programmatically.
/// </summary>
public class SettingsUI : MonoBehaviour
{
    [Header("Frame Rate Setting")]
    [Tooltip("Optional: Existing dropdown for frame rate. If null, one will be created.")]
    [SerializeField] private TMP_Dropdown frameRateDropdown;

    [Header("UI Generation Settings")]
    [Tooltip("Parent transform for generated UI elements. Uses this transform if null.")]
    [SerializeField] private Transform uiParent;

    [Header("Style")]
    [SerializeField] private Color labelColor = new Color(0.9f, 0.85f, 1f, 1f);
    [SerializeField] private Color dropdownColor = new Color(0.15f, 0.12f, 0.2f, 0.9f);

    private void Start()
    {
        if (uiParent == null)
        {
            // Look for DropdownContainer created by MenuController
            Transform dropdownContainer = transform.GetComponentInChildren<RectTransform>()?.Find("DropdownContainer");
            if (dropdownContainer == null)
            {
                // Search recursively
                dropdownContainer = FindChildRecursive(transform, "DropdownContainer");
            }
            uiParent = dropdownContainer != null ? dropdownContainer : transform;
        }

        SetupFrameRateDropdown();
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            Transform found = FindChildRecursive(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    private void OnEnable()
    {
        // Refresh UI when settings panel is shown
        if (frameRateDropdown != null)
        {
            frameRateDropdown.value = GameSettings.CurrentFrameRateIndex;
        }
    }

    private void SetupFrameRateDropdown()
    {
        if (frameRateDropdown == null)
        {
            CreateFrameRateUI();
        }

        if (frameRateDropdown != null)
        {
            // Clear and populate options
            frameRateDropdown.ClearOptions();
            frameRateDropdown.AddOptions(new System.Collections.Generic.List<string>(GameSettings.FrameRateLabels));

            // Set current value
            frameRateDropdown.value = GameSettings.CurrentFrameRateIndex;

            // Add listener
            frameRateDropdown.onValueChanged.AddListener(OnFrameRateChanged);
        }
    }

    private void CreateFrameRateUI()
    {
        // Check if we're inside a DropdownContainer (created by MenuController)
        bool hasContainer = uiParent.name == "DropdownContainer";

        if (hasContainer)
        {
            // Just create the dropdown, MenuController already created the label
            CreateDropdown(uiParent);
        }
        else
        {
            // Create full UI with container and label
            GameObject container = new GameObject("FrameRateSetting");
            container.transform.SetParent(uiParent, false);

            RectTransform containerRT = container.AddComponent<RectTransform>();
            containerRT.anchorMin = new Vector2(0.5f, 0.5f);
            containerRT.anchorMax = new Vector2(0.5f, 0.5f);
            containerRT.sizeDelta = new Vector2(300, 40);
            containerRT.anchoredPosition = new Vector2(0, 30);

            // Add horizontal layout
            HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 15;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            // Create label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(container.transform, false);

            TextMeshProUGUI labelTMP = labelObj.AddComponent<TextMeshProUGUI>();
            labelTMP.text = "Frame Rate:";
            labelTMP.fontSize = 20;
            labelTMP.color = labelColor;
            labelTMP.alignment = TextAlignmentOptions.MidlineRight;

            RectTransform labelRT = labelObj.GetComponent<RectTransform>();
            labelRT.sizeDelta = new Vector2(120, 35);

            // Create dropdown
            CreateDropdown(container.transform);
        }
    }

    private void CreateDropdown(Transform parent)
    {
        // Create dropdown object
        GameObject dropdownObj = new GameObject("FrameRateDropdown");
        dropdownObj.transform.SetParent(parent, false);

        RectTransform dropdownRT = dropdownObj.AddComponent<RectTransform>();

        // If parent is DropdownContainer, fill it; otherwise use fixed size
        if (parent.name == "DropdownContainer")
        {
            dropdownRT.anchorMin = Vector2.zero;
            dropdownRT.anchorMax = Vector2.one;
            dropdownRT.offsetMin = new Vector2(5, 2);
            dropdownRT.offsetMax = new Vector2(-5, -2);
        }
        else
        {
            dropdownRT.sizeDelta = new Vector2(150, 35);
        }

        Image dropdownBg = dropdownObj.AddComponent<Image>();
        dropdownBg.color = dropdownColor;

        frameRateDropdown = dropdownObj.AddComponent<TMP_Dropdown>();

        // Create label for selected value
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(dropdownObj.transform, false);

        TextMeshProUGUI labelTMP = labelObj.AddComponent<TextMeshProUGUI>();
        labelTMP.fontSize = 16;
        labelTMP.color = labelColor;
        labelTMP.alignment = TextAlignmentOptions.Center;

        RectTransform labelRT = labelObj.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = new Vector2(10, 0);
        labelRT.offsetMax = new Vector2(-25, 0);

        // Create arrow
        GameObject arrowObj = new GameObject("Arrow");
        arrowObj.transform.SetParent(dropdownObj.transform, false);

        TextMeshProUGUI arrowTMP = arrowObj.AddComponent<TextMeshProUGUI>();
        arrowTMP.text = "\u25BC"; // Down arrow
        arrowTMP.fontSize = 12;
        arrowTMP.color = labelColor;
        arrowTMP.alignment = TextAlignmentOptions.Center;

        RectTransform arrowRT = arrowObj.GetComponent<RectTransform>();
        arrowRT.anchorMin = new Vector2(1, 0);
        arrowRT.anchorMax = new Vector2(1, 1);
        arrowRT.pivot = new Vector2(1, 0.5f);
        arrowRT.sizeDelta = new Vector2(20, 0);
        arrowRT.anchoredPosition = new Vector2(-5, 0);

        // Create template for dropdown list
        GameObject templateObj = new GameObject("Template");
        templateObj.transform.SetParent(dropdownObj.transform, false);
        templateObj.SetActive(false);

        RectTransform templateRT = templateObj.AddComponent<RectTransform>();
        templateRT.anchorMin = new Vector2(0, 0);
        templateRT.anchorMax = new Vector2(1, 0);
        templateRT.pivot = new Vector2(0.5f, 1);
        templateRT.anchoredPosition = Vector2.zero;
        templateRT.sizeDelta = new Vector2(0, 150);

        Image templateBg = templateObj.AddComponent<Image>();
        templateBg.color = new Color(0.1f, 0.08f, 0.15f, 0.95f);

        ScrollRect scrollRect = templateObj.AddComponent<ScrollRect>();

        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(templateObj.transform, false);

        RectTransform viewportRT = viewport.AddComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = Vector2.zero;
        viewportRT.offsetMax = Vector2.zero;

        viewport.AddComponent<Mask>().showMaskGraphic = false;
        viewport.AddComponent<Image>();

        // Content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);

        RectTransform contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.sizeDelta = new Vector2(0, 0);

        // Item template
        GameObject itemObj = new GameObject("Item");
        itemObj.transform.SetParent(content.transform, false);

        RectTransform itemRT = itemObj.AddComponent<RectTransform>();
        itemRT.anchorMin = new Vector2(0, 0.5f);
        itemRT.anchorMax = new Vector2(1, 0.5f);
        itemRT.sizeDelta = new Vector2(0, 30);

        Toggle toggle = itemObj.AddComponent<Toggle>();

        // Item background
        GameObject itemBgObj = new GameObject("Item Background");
        itemBgObj.transform.SetParent(itemObj.transform, false);

        RectTransform itemBgRT = itemBgObj.AddComponent<RectTransform>();
        itemBgRT.anchorMin = Vector2.zero;
        itemBgRT.anchorMax = Vector2.one;
        itemBgRT.offsetMin = Vector2.zero;
        itemBgRT.offsetMax = Vector2.zero;

        Image itemBg = itemBgObj.AddComponent<Image>();
        itemBg.color = new Color(0.2f, 0.15f, 0.25f, 1f);

        // Item checkmark (highlight)
        GameObject checkmarkObj = new GameObject("Item Checkmark");
        checkmarkObj.transform.SetParent(itemObj.transform, false);

        RectTransform checkmarkRT = checkmarkObj.AddComponent<RectTransform>();
        checkmarkRT.anchorMin = Vector2.zero;
        checkmarkRT.anchorMax = Vector2.one;
        checkmarkRT.offsetMin = Vector2.zero;
        checkmarkRT.offsetMax = Vector2.zero;

        Image checkmark = checkmarkObj.AddComponent<Image>();
        checkmark.color = new Color(0.4f, 0.3f, 0.6f, 0.5f);

        // Item label
        GameObject itemLabelObj = new GameObject("Item Label");
        itemLabelObj.transform.SetParent(itemObj.transform, false);

        RectTransform itemLabelRT = itemLabelObj.AddComponent<RectTransform>();
        itemLabelRT.anchorMin = Vector2.zero;
        itemLabelRT.anchorMax = Vector2.one;
        itemLabelRT.offsetMin = new Vector2(10, 0);
        itemLabelRT.offsetMax = new Vector2(-10, 0);

        TextMeshProUGUI itemLabelTMP = itemLabelObj.AddComponent<TextMeshProUGUI>();
        itemLabelTMP.fontSize = 16;
        itemLabelTMP.color = labelColor;
        itemLabelTMP.alignment = TextAlignmentOptions.MidlineLeft;

        // Configure toggle
        toggle.targetGraphic = itemBg;
        toggle.graphic = checkmark;
        toggle.isOn = false;

        // Configure scroll rect
        scrollRect.content = contentRT;
        scrollRect.viewport = viewportRT;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        // Configure dropdown
        frameRateDropdown.captionText = labelTMP;
        frameRateDropdown.itemText = itemLabelTMP;
        frameRateDropdown.template = templateRT;
    }

    private void OnFrameRateChanged(int index)
    {
        GameSettings.SetFrameRateByIndex(index);
    }

    private void OnDestroy()
    {
        if (frameRateDropdown != null)
        {
            frameRateDropdown.onValueChanged.RemoveListener(OnFrameRateChanged);
        }
    }
}
