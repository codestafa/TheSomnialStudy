using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PlayerInteraction : MonoBehaviour
{
    [Header("References")][SerializeField] private Camera playerCamera;

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

    [Header("Reticle")]
    [SerializeField] private Graphic reticleGraphic;
    [SerializeField] private Color reticleNormal = Color.white;
    [SerializeField] private Color reticleHover = new Color(1f, 1f, 0f, 1f); // visible hover color

    [Header("UI")]
    public UnityEvent<string> onHoverPromptChanged;

    [Header("Debug")][SerializeField] private bool debugMode = false;

    private IInteractable currentTarget;
    private float lastInteractTime = -999f;
    private string lastPromptSent = "";
    private bool lastHoverState = false;

    private void Awake()
    {
        if (!playerCamera) playerCamera = Camera.main;
        if (!playerCamera) Debug.LogWarning("PlayerInteraction: No camera assigned and no Camera.main found.");
        SetReticleHover(false);
    }

    private void OnDisable()
    {
        if (!string.IsNullOrEmpty(lastPromptSent)) { onHoverPromptChanged?.Invoke(""); lastPromptSent = ""; }
        currentTarget = null;
        SetReticleHover(false);
    }

    private void Update()
    {
        DetectInteractable();

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

        var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        bool hitSomething;
        RaycastHit hit;

        if (useSpherecast)
        {
            hitSomething = Physics.SphereCast(ray, spherecastRadius, out hit, interactDistance, ~0, triggerInteraction);
            if (debugMode) Debug.DrawRay(ray.origin, ray.direction * (hitSomething ? hit.distance : interactDistance), Color.cyan);
        }
        else
        {
            hitSomething = Physics.Raycast(ray, out hit, interactDistance, ~0, triggerInteraction);
            if (debugMode) Debug.DrawRay(ray.origin, ray.direction * (hitSomething ? hit.distance : interactDistance), Color.yellow);
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
                    // ***** CONDITIONAL GATE *****
                    if (candidate is IInteractableConditional cond && !cond.CanInteract())
                    {
                        // treat as not interactable right now
                        found = null;
                    }
                    else
                    {
                        found = candidate;
                    }
                }
                else if (debugMode) Debug.Log($"[PlayerInteraction] '{c.gameObject.name}' blocked by interactable layer mask.");
            }
            else if (debugMode)
            {
                Debug.Log($"[PlayerInteraction] Hit '{hit.collider.name}', no IInteractable on it or parents.");
            }
        }

        if (!ReferenceEquals(found, currentTarget))
        {
            if (enableFocusCallbacks && currentTarget is IInteractableFocus oldFocus)
            {
                try { oldFocus.OnLoseFocus(); } catch (System.Exception ex) { Debug.LogError(ex); }
            }

            currentTarget = found;
            SetReticleHover(currentTarget != null);

            if (enableFocusCallbacks && currentTarget is IInteractableFocus newFocus)
            {
                try { newFocus.OnFocus(); } catch (System.Exception ex) { Debug.LogError(ex); }
            }

            string prompt = "";
            if (showPromptOnHover && currentTarget is IInteractablePrompt withPrompt)
            {
                try { prompt = withPrompt.GetPrompt() ?? ""; } catch (System.Exception ex) { Debug.LogError(ex); }
            }
            if (prompt != lastPromptSent) { onHoverPromptChanged?.Invoke(prompt); lastPromptSent = prompt; }
        }

        if (currentTarget == null && lastPromptSent != "")
        {
            onHoverPromptChanged?.Invoke("");
            lastPromptSent = "";
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
