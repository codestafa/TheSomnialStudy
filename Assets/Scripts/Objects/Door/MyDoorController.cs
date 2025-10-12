using UnityEngine;

public class MyDoorController : MonoBehaviour, IInteractable
{
    [SerializeField] private Animator animator;

    private bool isOpen;

    void Awake()
    {
        isOpen = false;
        if (!animator) animator = GetComponent<Animator>();

        // Example of using TryGetComponent for debugging/verification
        if (TryGetComponent<IInteractable>(out var interactable))
        {
            Debug.Log($"{name} has an IInteractable component: {interactable}");
        }
        else
        {
            Debug.LogWarning($"{name} does NOT have an IInteractable component!");
        }
    }

    public void Interact()
    {
        isOpen = !isOpen;
        animator.SetBool("isOpen", isOpen);
    }

    // 👇 Needed for your raycast script to show crosshair
    public bool CanInteract => true; // only show crosshair if the door is closed
}
