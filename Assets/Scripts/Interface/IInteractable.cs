public interface IInteractable
{
    void Interact();
}

public interface IInteractableFocus
{
    void OnFocus();
    void OnLoseFocus();
}

public interface IInteractablePrompt
{
    string GetPrompt();
}

/// <summary>
/// Optional gate: if implemented and returns false, the object is treated
/// as non-interactable for hover color/prompt and Interact() will be ignored.
/// </summary>
public interface IInteractableConditional
{
    bool CanInteract();
}
