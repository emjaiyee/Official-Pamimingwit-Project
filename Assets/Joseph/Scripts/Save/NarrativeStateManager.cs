using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Helper class for serializing Dictionary<string, bool>
[System.Serializable]
public class NarrativeTriggerState
{
    public string id;
    public bool triggered;

    public NarrativeTriggerState(string id, bool triggered)
    {
        this.id = id;
        this.triggered = triggered;
    }
}

[DefaultExecutionOrder(-150)]
public class NarrativeStateManager : MonoBehaviour
{
    public static NarrativeStateManager Instance;

    /// <summary>
    /// Global lock to prevent multiple dialogues or cutscenes 
    /// from starting at the exact same time.
    /// </summary>
    public bool IsNarrativeActive { get; set; }

    private Dictionary<string, bool> _triggeredNarratives = new Dictionary<string, bool>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Persist across scenes
    }

    public void SetTriggered(string id, bool state)
    {
        if (string.IsNullOrEmpty(id)) { Debug.LogWarning("Attempted to set triggered state for a narrative with an empty ID."); return; }
        _triggeredNarratives[id] = state;
    }

    public bool IsTriggered(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        return _triggeredNarratives.ContainsKey(id) && _triggeredNarratives[id];
    }

    public List<NarrativeTriggerState> GetSaveData() => new List<NarrativeTriggerState>(_triggeredNarratives.Select(kvp => new NarrativeTriggerState(kvp.Key, kvp.Value)));

    public void LoadSaveData(List<NarrativeTriggerState> saveData)
    {
        _triggeredNarratives.Clear();
        if (saveData == null) return;

        foreach (var entry in saveData)
        {
            _triggeredNarratives[entry.id] = entry.triggered;
        }
    }
}