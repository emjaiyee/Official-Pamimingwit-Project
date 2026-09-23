using UnityEngine;

public class TrashModule : NPCModule
{
    [Header("Persistence")]
    [Tooltip("Unique ID for this specific trash object. Required if this is a permanent scene object (e.g. for an objective).")]
    public string worldObjectID;

    [Header("Minigame Fallback Reference")]
    [Tooltip("Optional direct reference to the CleaningMiniGameManager UI component in the scene canvas.")]
    [SerializeField] private CleaningMiniGameManager minigameOverride;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // If this is the Prefab Asset itself, keep the ID empty so 
        // instances are forced to generate their own unique IDs.
        if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this))
        {
            worldObjectID = "";
            return;
        }

        if (string.IsNullOrEmpty(worldObjectID))
        {
            worldObjectID = $"Trash_{gameObject.name}_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif

    [ContextMenu("Generate Unique ID")]
    private void GenerateID() => worldObjectID = $"Trash_{gameObject.name}_{System.Guid.NewGuid().ToString().Substring(0, 8)}";

    public override string GetInteractionPrompt()
    {
        return "Inspect Trash [E]";
    }

    public override void OnInteract()
    {
        if (UIManager.Instance == null) return;

        UIManager.Instance.ShowChoice(
            "What will you do with this trash?",
            "Throw in Ocean", 
            ChoiceThrowInOcean, 
            "Clean Up", 
            ChoiceCleanUp
        );
    }

    // Called when "Throw in Ocean" is chosen
    public void ChoiceThrowInOcean()
    {
        Debug.Log("Trash thrown in ocean. Sustainability decreased.");
        if (SustainabilityManager.Instance != null)
            SustainabilityManager.Instance.Add(-5);

        SaveController.RegisterDestruction(worldObjectID);
            
        ObjectiveCutsceneTrigger.NotifyProgress(gameObject);
        Destroy(gameObject);
    }

    // Called when "Clean Up" is chosen
    public void ChoiceCleanUp()
    {
        CleaningMiniGameManager manager = minigameOverride != null
            ? minigameOverride
            : CleaningMiniGameManager.Instance;

        if (manager == null)
        {
            manager = FindFirstObjectByType<CleaningMiniGameManager>(FindObjectsInactive.Include);
        }

        if (manager != null)
        {
            manager.StartGame(gameObject);
        }
        else
        {
            Debug.LogWarning("[TrashModule] CleaningMiniGameManager instance missing from scene. Performing direct cleanup fallback.");

            // Direct cleanup fallback
            if (SustainabilityManager.Instance != null)
                SustainabilityManager.Instance.Add(5);

            SaveController.RegisterDestruction(worldObjectID);
            ObjectiveCutsceneTrigger.NotifyProgress(gameObject);
            Destroy(gameObject);
        }
    }
}