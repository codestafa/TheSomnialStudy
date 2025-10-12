/// <summary>
/// Defines a contract for all interactable objects in the game.
/// Any class that implements this interface can be "interacted with"
/// by a player, NPC, or another system.
/// 
/// Example usage:
/// - Doors can implement IInteractable to open/close when interacted with.
/// - Items can implement IInteractable to be picked up.
/// - NPCs can implement IInteractable to trigger dialogue.
/// 
/// Interfaces in Unity are commonly used to enforce consistent behavior
/// across multiple unrelated classes.
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Called when the player (or another actor) interacts with this object.
    /// 
    /// The exact behavior is defined by the implementing class. 
    /// Examples:
    /// - Open a door
    /// - Pick up an item
    /// - Start dialogue
    /// - Trigger a cutscene or event
    /// </summary>
    void Interact();
}
