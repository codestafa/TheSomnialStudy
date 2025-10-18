using UnityEngine;

/// <summary>
/// Handles player interaction with objects that implement the IInteractable interface.
/// Performs a forward Raycast from the camera to detect nearby interactable objects.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField, Tooltip("The camera used to cast the interaction ray.")]
    private Camera playerCamera;

    [SerializeField, Tooltip("Maximum distance at which the player can interact.")]
    private float interactDistance = 3f;

    [SerializeField, Tooltip("Key used to interact with objects.")]
    private KeyCode interactKey = KeyCode.E;

    [SerializeField, Tooltip("Draws debug ray in Scene view.")]
    private bool debugMode = true;

    private IInteractable currentTarget;

    private void Update()
    {
        DetectInteractable();

        if (currentTarget != null && Input.GetKeyDown(interactKey))
        {
            currentTarget.Interact();
        }
    }

    /// <summary>
    /// Performs a raycast from the center of the player's camera to find interactable objects.
    /// </summary>
    private void DetectInteractable()
    {
        if (!playerCamera)
        {
            Debug.LogWarning("PlayerInteraction: No player camera assigned!");
            return;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            if (debugMode)
                Debug.DrawRay(ray.origin, ray.direction * interactDistance, Color.green);

            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                if (interactable != currentTarget)
                {
                    currentTarget = interactable;
                    if (debugMode)
                        Debug.Log($"Looking at interactable: {hit.collider.name}");
                }
            }
            else
            {
                currentTarget = null;
            }
        }
        else
        {
            if (debugMode)
                Debug.DrawRay(ray.origin, ray.direction * interactDistance, Color.red);

            currentTarget = null;
        }
    }
}
