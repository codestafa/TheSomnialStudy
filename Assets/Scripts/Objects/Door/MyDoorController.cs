using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MyDoorController : MonoBehaviour, IInteractable, IInteractablePrompt, IInteractableConditional
{
    [SerializeField] private Animator animator;
    [SerializeField] private string isOpenBool = "isOpen"; // animator bool param name
    [SerializeField] private bool isOpen = false;          // initial state
    [SerializeField] private bool locked = false;          // example lock

    private void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (animator && HasBool(animator, isOpenBool)) animator.SetBool(isOpenBool, isOpen);
    }

    // ---- Gate: only hover/press when allowed ----
    public bool CanInteract()
    {
        // You can add more conditions (distance, key owned, power on, etc.)
        return !locked; // hover/press only if not locked
    }

    // ---- Prompt shown while hovering (optional) ----
    public string GetPrompt()
    {
        if (locked) return "Locked";
        return isOpen ? "Press E to close" : "Press E to open";
    }

    // ---- Action when E is pressed ----
    public void Interact()
    {
        if (!CanInteract()) return; // double-safety
        isOpen = !isOpen;
        if (animator && HasBool(animator, isOpenBool))
            animator.SetBool(isOpenBool, isOpen);
        // else: trigger playback via other params if that’s your setup
    }

    private static bool HasBool(Animator a, string param)
    {
        foreach (var p in a.parameters)
            if (p.type == AnimatorControllerParameterType.Bool && p.name == param) return true;
        return false;
    }
}
