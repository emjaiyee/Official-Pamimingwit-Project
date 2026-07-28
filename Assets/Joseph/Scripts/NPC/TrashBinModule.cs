using UnityEngine;

public class TrashBinModule : NPCModule
{
    public override string GetInteractionPrompt()
    {
        return "Use Trash Bin [E]";
    }

    public override void OnInteract()
    {
        UIManager.Instance?.OpenTrashDisposal();
    }
}