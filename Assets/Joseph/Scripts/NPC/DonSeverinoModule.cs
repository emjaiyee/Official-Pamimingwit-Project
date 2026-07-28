using UnityEngine;

public class DonSeverinoModule : NPCModule
{
    [SerializeField] private DialogueLine[] dialogueLines;

    public override string GetInteractionPrompt()
    {
        return "Speak with Don Severino [E]";
    }

    public override void OnInteract()
    {
        if (DialogueManager.Instance != null)
        {
            // Start dialogue and pass the industrial shop opening logic as a callback
            DialogueManager.Instance.ShowDialogue(dialogueLines, OpenIndustrialShop);
        }
        else
        {
            OpenIndustrialShop();
        }
    }

    private void OpenIndustrialShop()
    {
        Debug.Log("Don Severino: Industrial Shop Opened!");
        UIManager.Instance?.OpenIndustrialShop(); // Note: Ensure this method exists in your UIManager
    }
}