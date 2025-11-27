using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using NeutronCat.MusicalInstrument;

/// <summary>
/// Detects when a specific sequence of piano keys is pressed and triggers events.
/// Works with existing PianoKey and PianoController scripts - no modifications needed!
/// </summary>
public class PianoKeySequenceDetector : MonoBehaviour
{
    [System.Serializable]
    public class KeySequence
    {
        [Tooltip("Name of this sequence (for debugging)")]
        public string sequenceName = "Secret Melody";
        
        [Tooltip("The exact sequence of notes that must be pressed")]
        public List<KeyNote> noteSequence = new List<KeyNote>();
        
        [Tooltip("Maximum time (in seconds) between key presses before the sequence resets")]
        public float timeoutDuration = 5f;
        
        [Tooltip("Allow extra keys to be pressed between sequence keys")]
        public bool strictSequence = true;
        
        [Tooltip("Event triggered when this sequence is completed")]
        public UnityEvent onSequenceComplete;

        [HideInInspector]
        public int currentIndex = 0;
        
        [HideInInspector]
        public float lastPressTime = 0f;
    }

    [Header("Piano Reference")]
    [SerializeField] private PianoController pianoController;

    [Header("Sequences to Detect")]
    [SerializeField] private List<KeySequence> sequences = new List<KeySequence>();

    [Header("Debug Settings")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool showCurrentProgress = true;

    [Header("Global Events")]
    [SerializeField] private UnityEvent onAnySequenceComplete;

    private List<KeyNote> recentlyPressedKeys = new List<KeyNote>();
    private int lastCommandCount = 0;

    private void Start()
    {
        // Auto-find piano controller if not assigned
        if (pianoController == null)
        {
            pianoController = FindObjectOfType<PianoController>();
        }

        if (pianoController == null)
        {
            Debug.LogError("PianoKeySequenceDetector: No PianoController found! Please assign one.");
            enabled = false;
            return;
        }

        // Find all PianoKey components and add notifier components
        PianoKey[] allKeys = FindObjectsOfType<PianoKey>();
        
        foreach (var key in allKeys)
        {
            var notifier = key.gameObject.GetComponent<PianoKeyPressNotifier>();
            if (notifier == null)
            {
                notifier = key.gameObject.AddComponent<PianoKeyPressNotifier>();
            }
            notifier.Initialize(this, key);
        }

        if (showDebugLogs)
        {
            Debug.Log($"PianoKeySequenceDetector: Monitoring {allKeys.Length} piano keys for {sequences.Count} sequences.");
        }
    }

    private void Update()
    {
        // Check for timeouts on all sequences
        foreach (var sequence in sequences)
        {
            if (sequence.currentIndex > 0 && Time.time - sequence.lastPressTime > sequence.timeoutDuration)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"Sequence '{sequence.sequenceName}' timed out. Resetting progress.");
                }
                ResetSequence(sequence);
            }
        }
    }

    /// <summary>
    /// Called by PianoKeyPressNotifier when a key is pressed
    /// </summary>
    public void OnKeyPressed(KeyNote note, string noteName)
    {
        // Track recently pressed keys for debugging
        if (showCurrentProgress)
        {
            recentlyPressedKeys.Add(note);
            if (recentlyPressedKeys.Count > 10)
            {
                recentlyPressedKeys.RemoveAt(0);
            }
        }

        if (showDebugLogs)
        {
            Debug.Log($"Key pressed: {noteName} ({note})");
        }

        // Check all sequences
        foreach (var sequence in sequences)
        {
            CheckSequence(sequence, note);
        }
    }

    private void CheckSequence(KeySequence sequence, KeyNote pressedNote)
    {
        if (sequence.noteSequence.Count == 0)
        {
            Debug.LogWarning($"Sequence '{sequence.sequenceName}' has no notes defined!");
            return;
        }

        // Get the expected note at current position
        KeyNote expectedNote = sequence.noteSequence[sequence.currentIndex];

        // Check if the pressed note matches the expected note
        if (pressedNote == expectedNote)
        {
            // Correct note pressed!
            sequence.currentIndex++;
            sequence.lastPressTime = Time.time;

            if (showDebugLogs)
            {
                Debug.Log($"Sequence '{sequence.sequenceName}': Progress {sequence.currentIndex}/{sequence.noteSequence.Count}");
            }

            // Check if sequence is complete
            if (sequence.currentIndex >= sequence.noteSequence.Count)
            {
                OnSequenceCompleted(sequence);
                ResetSequence(sequence);
            }
        }
        else if (sequence.strictSequence && sequence.currentIndex > 0)
        {
            // Wrong note pressed in strict mode - reset
            if (showDebugLogs)
            {
                Debug.Log($"Sequence '{sequence.sequenceName}' broken. Expected: {expectedNote}, Got: {pressedNote}. Resetting.");
            }
            ResetSequence(sequence);
        }
        // In non-strict mode, wrong notes are simply ignored
    }

    private void OnSequenceCompleted(KeySequence sequence)
    {
        if (showDebugLogs)
        {
            Debug.Log($"✓ Sequence '{sequence.sequenceName}' COMPLETED!");
        }

        // Trigger sequence-specific event
        sequence.onSequenceComplete?.Invoke();

        // Trigger global event
        onAnySequenceComplete?.Invoke();
    }

    private void ResetSequence(KeySequence sequence)
    {
        sequence.currentIndex = 0;
        sequence.lastPressTime = 0f;
    }

    /// <summary>
    /// Manually reset all sequences
    /// </summary>
    public void ResetAllSequences()
    {
        foreach (var sequence in sequences)
        {
            ResetSequence(sequence);
        }

        recentlyPressedKeys.Clear();
        
        if (showDebugLogs)
        {
            Debug.Log("All sequences have been reset.");
        }
    }

    /// <summary>
    /// Get current progress of a specific sequence (0.0 to 1.0)
    /// </summary>
    public float GetSequenceProgress(string sequenceName)
    {
        KeySequence sequence = sequences.Find(s => s.sequenceName == sequenceName);
        if (sequence == null || sequence.noteSequence.Count == 0) return 0f;

        return (float)sequence.currentIndex / sequence.noteSequence.Count;
    }

    private void OnGUI()
    {
        if (!showCurrentProgress) return;

        // Show recently pressed keys
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        GUILayout.Label("Recently Pressed Keys:", GUI.skin.box);
        
        foreach (var note in recentlyPressedKeys)
        {
            GUILayout.Label(note.ToString());
        }

        GUILayout.Space(10);

        // Show progress for each sequence
        foreach (var sequence in sequences)
        {
            if (sequence.noteSequence.Count > 0)
            {
                GUILayout.Label($"{sequence.sequenceName}: {sequence.currentIndex}/{sequence.noteSequence.Count}", GUI.skin.box);
                
                // Show next expected note
                if (sequence.currentIndex < sequence.noteSequence.Count)
                {
                    GUILayout.Label($"  Next: {sequence.noteSequence[sequence.currentIndex]}", GUI.skin.label);
                }
            }
        }

        GUILayout.EndArea();
    }
}

/// <summary>
/// Helper component that gets automatically added to each PianoKey to notify the detector.
/// This is added at runtime and doesn't modify the original PianoKey script.
/// Detects key presses by monitoring AudioSource play events.
/// </summary>
[RequireComponent(typeof(PianoKey))]
public class PianoKeyPressNotifier : MonoBehaviour
{
    private PianoKey pianoKey;
    private PianoKeySequenceDetector detector;
    private AudioSource audioSource;
    private bool wasPlaying = false;
    private KeyNote note;

    public void Initialize(PianoKeySequenceDetector det, PianoKey key)
    {
        detector = det;
        pianoKey = key;
        audioSource = GetComponent<AudioSource>();

        // Parse the note from the PianoKey's noteName
        if (!System.Enum.TryParse(pianoKey.noteName, out note))
        {
            Debug.LogError($"Invalid note name '{pianoKey.noteName}' on {name}");
        }
    }

    private void Update()
    {
        if (audioSource == null || detector == null) return;

        // Detect when the audio starts playing (key was just pressed)
        if (audioSource.isPlaying && !wasPlaying)
        {
            detector.OnKeyPressed(note, pianoKey.noteName);
        }

        wasPlaying = audioSource.isPlaying;
    }
}
