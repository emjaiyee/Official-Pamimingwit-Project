using UnityEngine;

public class NPCManager : MonoBehaviour, IInteractable
{
    [Header("Identity")]
    public string npcName;

    private NPCModule _activeModule;

    private void Awake()
    {
        // Automatically find the specialist module attached to this NPC
        _activeModule = GetComponent<NPCModule>();
    }

    public void Interact()
    {
        if (_activeModule != null)
        {
            _activeModule.OnInteract();
        }
    }

    public string GetInteractPrompt()
    {
        return _activeModule != null ? _activeModule.GetInteractionPrompt() : "Talk";
    }
}