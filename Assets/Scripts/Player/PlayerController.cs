using UnityEngine;

/// <summary>
/// Central player controller that coordinates all player systems.
/// Attach to the player GameObject alongside FirstPersonController.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Component References")]
    [SerializeField] private FirstPersonController fpsController;
    [SerializeField] private PlayerNoise playerNoise;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("State")]
    [SerializeField] private bool controlsEnabled = true;

    // Public accessors for other systems
    public bool ControlsEnabled => controlsEnabled;
    public bool IsAlive => playerHealth == null || playerHealth.IsAlive;
    public Vector3 Position => transform.position;
    public Vector3 Velocity => rb != null ? rb.linearVelocity : Vector3.zero;
    public float CurrentSpeed => Velocity.magnitude;
    public bool IsMoving => CurrentSpeed > 0.1f;

    private Rigidbody rb;

    private void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody>();

        // Auto-find components if not assigned
        if (fpsController == null)
            fpsController = GetComponent<FirstPersonController>();

        if (playerNoise == null)
            playerNoise = GetComponent<PlayerNoise>();

        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();
    }

    private void Start()
    {
        // Ensure PlayerNoise component exists
        if (playerNoise == null)
        {
            playerNoise = gameObject.AddComponent<PlayerNoise>();
            Debug.Log("[PlayerController] Added missing PlayerNoise component");
        }
    }

    /// <summary>
    /// Enable or disable all player controls (movement, camera, interactions)
    /// </summary>
    public void SetControlsEnabled(bool enabled)
    {
        controlsEnabled = enabled;

        if (fpsController != null)
        {
            fpsController.playerCanMove = enabled;
            fpsController.cameraCanMove = enabled;
        }

        // Stop movement when controls disabled
        if (!enabled && rb != null)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }

    /// <summary>
    /// Freeze player completely (for cutscenes, death, etc.)
    /// </summary>
    public void Freeze()
    {
        SetControlsEnabled(false);

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }

    /// <summary>
    /// Unfreeze player and restore controls
    /// </summary>
    public void Unfreeze()
    {
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        SetControlsEnabled(true);
    }

    /// <summary>
    /// Lock cursor for gameplay
    /// </summary>
    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Unlock cursor for menus
    /// </summary>
    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Teleport player to a position
    /// </summary>
    public void TeleportTo(Vector3 position)
    {
        if (rb != null)
        {
            rb.position = position;
            rb.linearVelocity = Vector3.zero;
        }
        else
        {
            transform.position = position;
        }
    }

    /// <summary>
    /// Teleport player and set rotation
    /// </summary>
    public void TeleportTo(Vector3 position, Quaternion rotation)
    {
        TeleportTo(position);
        transform.rotation = rotation;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
