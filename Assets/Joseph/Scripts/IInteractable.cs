public interface IInteractable
{
    void Interact();
    string GetInteractPrompt(); // Optional: Returns "Talk", "Pick Up", or "Buy"
}