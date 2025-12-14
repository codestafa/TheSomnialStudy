using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

/// <summary>
/// Adds dreamy hover and click effects to menu buttons.
/// Attach to each button for atmospheric UI feedback.
/// </summary>
[RequireComponent(typeof(Button))]
public class MenuButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Scale Animation")]
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float clickScale = 0.95f;
    [SerializeField] private float animationSpeed = 8f;

    [Header("Color Animation")]
    [SerializeField] private Color normalColor = new Color(0.9f, 0.9f, 0.95f, 1f);
    [SerializeField] private Color hoverColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color textNormalColor = new Color(0.2f, 0.15f, 0.25f, 1f);
    [SerializeField] private Color textHoverColor = new Color(0.1f, 0.05f, 0.15f, 1f);

    [Header("Glow Effect")]
    [SerializeField] private bool enableGlow = true;
    [SerializeField] private float glowIntensity = 0.3f;

    [Header("Audio")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;

    private Button button;
    private Image buttonImage;
    private TextMeshProUGUI buttonText;
    private RectTransform rectTransform;
    private Vector3 originalScale;
    private float targetScale = 1f;
    private Color targetColor;
    private Color targetTextColor;
    private AudioSource audioSource;
    private Shadow glowShadow;
    private bool isHovered = false;

    private void Awake()
    {
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        buttonText = GetComponentInChildren<TextMeshProUGUI>();
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;

        targetColor = normalColor;
        targetTextColor = textNormalColor;

        // Find or create audio source
        audioSource = GetComponentInParent<AudioSource>();
        if (audioSource == null)
        {
            MenuController menuController = FindObjectOfType<MenuController>();
            if (menuController != null)
            {
                audioSource = menuController.GetComponent<AudioSource>();
            }
        }

        // Apply initial colors
        if (buttonImage != null) buttonImage.color = normalColor;
        if (buttonText != null) buttonText.color = textNormalColor;

        // Add glow effect
        if (enableGlow)
        {
            SetupGlow();
        }
    }

    private void SetupGlow()
    {
        glowShadow = GetComponent<Shadow>();
        if (glowShadow == null)
        {
            glowShadow = gameObject.AddComponent<Shadow>();
        }
        glowShadow.effectColor = new Color(0.6f, 0.5f, 0.8f, 0f);
        glowShadow.effectDistance = new Vector2(0, -2);
    }

    private void Update()
    {
        // Smooth scale animation
        float currentScaleMultiplier = rectTransform.localScale.x / originalScale.x;
        float newScaleMultiplier = Mathf.Lerp(currentScaleMultiplier, targetScale, Time.deltaTime * animationSpeed);
        rectTransform.localScale = originalScale * newScaleMultiplier;

        // Smooth color animation
        if (buttonImage != null)
        {
            buttonImage.color = Color.Lerp(buttonImage.color, targetColor, Time.deltaTime * animationSpeed);
        }

        if (buttonText != null)
        {
            buttonText.color = Color.Lerp(buttonText.color, targetTextColor, Time.deltaTime * animationSpeed);
        }

        // Glow animation
        if (enableGlow && glowShadow != null)
        {
            float targetGlowAlpha = isHovered ? glowIntensity : 0f;
            Color glowColor = glowShadow.effectColor;
            glowColor.a = Mathf.Lerp(glowColor.a, targetGlowAlpha, Time.deltaTime * animationSpeed);
            glowShadow.effectColor = glowColor;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!button.interactable) return;

        isHovered = true;
        targetScale = hoverScale;
        targetColor = hoverColor;
        targetTextColor = textHoverColor;

        PlaySound(hoverSound);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        targetScale = 1f;
        targetColor = normalColor;
        targetTextColor = textNormalColor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!button.interactable) return;

        targetScale = clickScale;
        PlaySound(clickSound);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetScale = isHovered ? hoverScale : 1f;
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, 0.5f);
        }
    }

    /// <summary>
    /// Call this to trigger a pulse animation (e.g., when button becomes available)
    /// </summary>
    public void Pulse()
    {
        StartCoroutine(PulseAnimation());
    }

    private IEnumerator PulseAnimation()
    {
        float duration = 0.3f;
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float pulse = 1f + Mathf.Sin(t * Mathf.PI) * 0.1f;
            rectTransform.localScale = originalScale * pulse;
            yield return null;
        }

        rectTransform.localScale = originalScale;
    }
}
