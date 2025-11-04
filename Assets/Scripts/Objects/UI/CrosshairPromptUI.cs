using UnityEngine;
using TMPro;

public class CrosshairPromptUI : MonoBehaviour
{
    [SerializeField] TMP_Text text;          // assign your TMP_Text here
    [SerializeField] CanvasGroup group;      // optional (for fade)
    [SerializeField] bool startHidden = true;

    void Awake()
    {
        // Hide at startup if requested
        if (startHidden) ApplyVisible(false);
        if (text && startHidden) text.text = "";
    }

    // Hook PlayerInteraction.onHoverPromptChanged -> SetPrompt (Dynamic string)
    public void SetPrompt(string prompt)
    {
        if (text) text.text = prompt ?? "";
        ApplyVisible(!string.IsNullOrEmpty(prompt));
    }

    void ApplyVisible(bool show)
    {
        if (group)
        {
            group.alpha = show ? 1f : 0f;
            group.blocksRaycasts = show;
            group.interactable = show;
        }
        else
        {
            gameObject.SetActive(show);
        }
    }
}
