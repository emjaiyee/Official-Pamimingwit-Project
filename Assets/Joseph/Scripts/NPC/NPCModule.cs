using UnityEngine;

/// <summary>
/// Base class for all NPC specializations (Trading, Quests, Dialog, etc.)
/// </summary>
public abstract class NPCModule : MonoBehaviour
{
    public abstract string GetInteractionPrompt();
    public abstract void OnInteract();
}