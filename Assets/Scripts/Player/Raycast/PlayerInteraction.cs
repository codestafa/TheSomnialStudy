using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PlayerInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;

    [Header("Detection")]
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private bool useSpherecast = true;
    [SerializeField] private float spherecastRadius = 0.15f;
    [SerializeField] private LayerMask interactableLayers = ~0;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

    [Header("Input")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactCooldown = 0.2f;

    [Header("Hover Behavior")]
    [SerializeField] private bool enableFocusCallbacks = false;
    [SerializeField] private bool showPromptOnHover = true;

    [Header("Performance")]
    [Tooltip("How often to check for interactables (seconds). Lower = more responsive, higher = better performance.")]
    [SerializeField] private float detectionInterval = 0.05f; // 20 checks per second instead of every frame

    [Header("Reticle")]
    [SerializeField] private Graphic reticleGraphic;
    [SerializeField] private Color reticleNormal = Color.white;
    [SerializeField] private Color reticleHover = new Color(1f, 1f, 0f, 1f);

    [Header("UI")]
    public UnityEvent<string> onHoverPromptChanged;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    private IInteractable currentTarget;
    private float lastInteractTime = -999f;
    private float lastDetectionTime = -999f;
    private string lastPromptSent = "";
    private bool lastHoverState = false;

    // Cached values to avoid repeated calculations
    private Ray cachedRay;
    private bool hasCachedRay = false;

    private void Awake()
    {
        if (!playerCamera) playerCamera = Camera.main;
        if (!playerCamera) Debug.LogWarning("PlayerInteraction: No camera assigned and no Camera.main found.");
        SetReticleHover(false);
    }

    private void OnDisable()
    {
        if (!string.IsNullOrEmpty(lastPromptSent))
        {
            onHoverPromptChanged?.Invoke("");
            lastPromptSent = "";
        }
        currentTarget = null;
        SetReticleHover(false);
    }

    private void Update()
    {
        // Throttle detection checks for performance
        if (Time.time - lastDetectionTime >= detectionInterval)
        {
            lastDetectionTime = Time.time;
            DetectInteractable();
            hasCachedRay = false; // Invalidate ray cache
        }

        // Input handling can still happen every frame for responsiveness
        if (currentTarget != null && Input.GetKeyDown(interactKey) &&
            Time.time - lastInteractTime >= interactCooldown)
        {
            lastInteractTime = Time.time;
            try { currentTarget.Interact(); }
            catch (System.Exception ex) { Debug.LogError($"[PlayerInteraction] Interact threw: {ex}"); }
        }
    }

    private void DetectInteractable()
    {
        if (!playerCamera) return;

        // Create ray once per detection cycle
        if (!hasCachedRay)
        {
            cachedRay = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            hasCachedRay = true;
        }

        bool hitSomething;
        RaycastHit hit;

        // Perform raycast/spherecast
        if (useSpherecast)
        {
            hitSomething = Physics.SphereCast(cachedRay, spherecastRadius, out hit, interactDistance, interactableLayers, triggerInteraction);
            if (debugMode) Debug.DrawRay(cachedRay.origin, cachedRay.direction * (hitSomething ? hit.distance : interactDistance), Color.cyan, detectionInterval);
        }
        else
        {
            hitSomething = Physics.Raycast(cachedRay, out hit, interactDistance, interactableLayers, triggerInteraction);
            if (debugMode) Debug.DrawRay(cachedRay.origin, cachedRay.direction * (hitSomething ? hit.distance : interactDistance), Color.yellow, detectionInterval);
        }

        IInteractable found = null;

        if (hitSomething)
        {
            var candidate = hit.collider.GetComponentInParent<IInteractable>();
            if (candidate is Component c)
            {
                bool layerAllowed = (interactableLayers.value & (1 << c.gameObject.layer)) != 0;
                if (layerAllowed)
                {
                    // Conditional gate check
                    if (candidate is IInteractableConditional cond && !cond.CanInteract())
                    {
                        found = null;
                    }
                    else
                    {
                        found = candidate;
                    }
                }
                else if (debugMode)
                {
                    Debug.Log($"[PlayerInteraction] '{c.gameObject.name}' blocked by interactable layer mask.");
                }
            }
            else if (debugMode)
            {
                Debug.Log($"[PlayerInteraction] Hit '{hit.collider.name}', no IInteractable on it or parents.");
            }
        }

        // Only update UI if target changed
        if (!ReferenceEquals(found, currentTarget))
        {
            HandleTargetChange(found);
        }
    }

    private void HandleTargetChange(IInteractable newTarget)
    {
        // Handle losing focus on old target
        if (enableFocusCallbacks && currentTarget is IInteractableFocus oldFocus)
        {
            try { oldFocus.OnLoseFocus(); }
            catch (System.Exception ex) { Debug.LogError($"[PlayerInteraction] OnLoseFocus threw: {ex}"); }
        }

        currentTarget = newTarget;
        SetReticleHover(currentTarget != null);

        // Handle gaining focus on new target
        if (enableFocusCallbacks && currentTarget is IInteractableFocus newFocus)
        {
            try { newFocus.OnFocus(); }
            catch (System.Exception ex) { Debug.LogError($"[PlayerInteraction] OnFocus threw: {ex}"); }
        }

        // Update prompt
        string prompt = "";
        if (showPromptOnHover && currentTarget is IInteractablePrompt withPrompt)
        {
            try { prompt = withPrompt.GetPrompt() ?? ""; }
            catch (System.Exception ex) { Debug.LogError($"[PlayerInteraction] GetPrompt threw: {ex}"); }
        }

        if (prompt != lastPromptSent)
        {
            onHoverPromptChanged?.Invoke(prompt);
            lastPromptSent = prompt;
        }
    }

    private void SetReticleHover(bool hovering)
    {
        if (!reticleGraphic) return;
        if (hovering == lastHoverState) return;

        reticleGraphic.color = hovering ? reticleHover : reticleNormal;
        lastHoverState = hovering;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!playerCamera) return;
        var origin = playerCamera.transform.position;
        var dir = playerCamera.transform.forward;
        Gizmos.color = Color.white;
        Gizmos.DrawLine(origin, origin + dir * interactDistance);
        if (useSpherecast)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
            Gizmos.DrawWireSphere(origin + dir * interactDistance, spherecastRadius);
        }
    }
#endif
}