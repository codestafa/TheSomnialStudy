using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Handles player health/caught state for chase sequences.
/// For a horror game, this tracks whether the player has been "caught" by the AI.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private bool invulnerable = false;

    [Header("Events")]
    public UnityEvent OnCaught;
    public UnityEvent OnRespawn;

    [Header("Respawn")]
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private float respawnDelay = 2f;

    public bool IsAlive { get; private set; } = true;
    public bool IsCaught => !IsAlive;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Called when player is caught by AI enemy
    /// </summary>
    public void GetCaught()
    {
        if (!IsAlive || invulnerable) return;

        IsAlive = false;
        Debug.Log("[PlayerHealth] Player caught!");

        // Disable player controls
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.Freeze();
        }

        OnCaught?.Invoke();

        // Auto-respawn after delay if respawn point is set
        if (respawnPoint != null)
        {
            Invoke(nameof(Respawn), respawnDelay);
        }
    }

    /// <summary>
    /// Respawn player at checkpoint
    /// </summary>
    public void Respawn()
    {
        if (IsAlive) return;

        IsAlive = true;
        Debug.Log("[PlayerHealth] Player respawned");

        // Teleport to respawn point
        if (respawnPoint != null && PlayerController.Instance != null)
        {
            PlayerController.Instance.TeleportTo(respawnPoint.position, respawnPoint.rotation);
        }

        // Re-enable controls
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.Unfreeze();
        }

        OnRespawn?.Invoke();
    }

    /// <summary>
    /// Set the respawn point dynamically (checkpoints)
    /// </summary>
    public void SetRespawnPoint(Transform point)
    {
        respawnPoint = point;
    }

    /// <summary>
    /// Toggle invulnerability (for debugging/cutscenes)
    /// </summary>
    public void SetInvulnerable(bool value)
    {
        invulnerable = value;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
