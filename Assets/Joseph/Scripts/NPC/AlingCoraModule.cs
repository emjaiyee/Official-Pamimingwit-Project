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
        if (DialogueManager.Instance != null && dialogueLines != null && dialogueLines.Length > 0)
        {
            DialogueManager.Instance.ShowDialogue(dialogueLines, OpenShop);
        }
        else
        {
            OpenShop();
        }
    }

    private void OpenShop()
    {
        UIManager.Instance?.OpenShop();
    }
}