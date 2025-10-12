using UnityEngine;
using NeutronCat.MusicalInstrument;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class PianoKey : MonoBehaviour, IInteractable
{
    [Header("Piano Key Settings")]
    public string noteName;                  // e.g. "C4", "D4S", "A0"
    public PianoController piano;
    [SerializeField] private float holdTime = 0.2f;

    [Header("Audio")]
    public AudioClip noteClip;               // Assign your mp3/wav here
    private AudioSource audioSource;

    private KeyNote note;

    void Awake()
    {
        if (!System.Enum.TryParse(noteName, out note))
        {
            Debug.LogError($"Invalid note name '{noteName}' on {gameObject.name}. " +
                           "Use enum format like C4, D4S, A0.");
        }

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;   // 0 = 2D sound, 1 = 3D sound
    }

    public void Interact()
    {
        if (piano == null)
        {
            Debug.LogWarning("No PianoController assigned!");
            return;
        }

        StartCoroutine(PressAndRelease());
    }

    private IEnumerator PressAndRelease()
    {
        // Animate key
        piano.KeyDown(note);

        // Play sound
        if (noteClip != null)
        {
            audioSource.clip = noteClip;
            audioSource.Play();
        }

        yield return new WaitForSeconds(holdTime);

        piano.KeyUp(note);
    }
}
