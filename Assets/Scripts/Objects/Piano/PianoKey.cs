using UnityEngine;
using NeutronCat.MusicalInstrument;
using System.Collections;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(AudioSource))]
public class PianoKey : MonoBehaviour, IInteractable, IInteractablePrompt, IInteractableFocus
{
    [Header("Piano Key Settings")]
    public string noteName;                  // e.g., "C4", "D4S", "A0"
    public PianoController piano;
    [SerializeField] private float holdTime = 0.2f;

    [Header("Audio")]
    public AudioClip noteClip;
    private AudioSource audioSource;

    private KeyNote note;
    private Vector3 startScale;

    private void Awake()
    {
        if (!System.Enum.TryParse(noteName, out note))
            Debug.LogError($"Invalid note name '{noteName}' on {name}. Use enum values like C4, D4S, A0.");

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound for UI-like feedback

        startScale = transform.localScale;
    }

    // -------------------- PROMPT (shown on hover) --------------------
    public string GetPrompt() => $"Press E to play {noteName}";

    // -------------------- INTERACT (only when E is pressed) ----------
    public void Interact()
    {
        if (piano == null)
        {
            Debug.LogWarning($"[{nameof(PianoKey)}] No PianoController assigned on {name}.");
            return;
        }
        StartCoroutine(PressAndRelease());
    }

    private IEnumerator PressAndRelease()
    {
        // Actual "press"
        piano.KeyDown(note);

        if (noteClip != null)
        {
            audioSource.clip = noteClip;
            audioSource.Play();
        }

        yield return new WaitForSeconds(holdTime);

        // Release
        piano.KeyUp(note);
    }

    // -------------------- OPTIONAL FOCUS/BLUR (no press here) --------
    // These do NOT press the key; they can be used for harmless visuals only.
    public void OnFocus() { /* e.g., transform.localScale = startScale * 1.03f; */ }
    public void OnLoseFocus() { /* e.g., transform.localScale = startScale; */ }
}
