using UnityEngine;

public class AlingCoraModule : NPCModule
{
    [SerializeField] private DialogueLine[] dialogueLines;

    public override string GetInteractionPrompt()
    {
        return "Speak with Aling Cora [E]";
    }

    public override void OnInteract()
    {
        if (DialogueManager.Instance != null)
        {
            // Start dialogue and pass the shop opening logic as a callback
            DialogueManager.Instance.ShowDialogue(dialogueLines, OpenShop);
        }
        else
        {
            OpenShop();
        }
    }

    private void OpenShop()
    {
        Debug.Log("Aling Cora: Shop Opened!");
        UIManager.Instance?.OpenShop();
    }
}
