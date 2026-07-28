using UnityEngine;

/// <summary>
/// Handles the world interaction for the Quest Board. 
/// Requires an 'Interactable' component on the same GameObject to function.
/// </summary>
public class QuestBoard : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactPrompt = "Check Quest Board [E]";

    public string GetInteractPrompt()
    {
        return interactPrompt;
    }

    public void Interact()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ToggleQuest();
        }
    }
}