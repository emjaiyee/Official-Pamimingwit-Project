using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class ObjectiveCutsceneTrigger : MonoBehaviour
{
    public enum NarrativeUI { DialoguePanel, CutscenePanel }

    [Header("Objective Settings")]
    [Tooltip("The cutscene will trigger once all these objects are destroyed or null.")]
    public List<GameObject> targets = new List<GameObject>();
    
    [Header("Trigger Configuration")]
    public NarrativeUI uiToUse = NarrativeUI.DialoguePanel;

    [Header("Content")]
    [Tooltip("Unique ID for this trigger. Required for saving/loading its triggered state.")]
    [SerializeField]
    public string triggerID;
    [Tooltip("If assigned, this will play a full cutscene style. If empty, it will use the Dialogue Lines below.")]
    public CutsceneData cutscene;
    public DialogueLine[] lines;

    private bool hasTriggered = false;

    public static event Action<GameObject> OnProgressMade;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(triggerID))
        {
            triggerID = $"{gameObject.name}_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif

    [ContextMenu("Generate Unique ID")]
    private void GenerateID() => triggerID = $"{gameObject.name}_{System.Guid.NewGuid().ToString().Substring(0, 8)}";

    private void Awake()
    {
        // If the narrative for this objective was already completed, we don't need to do anything.
        // We check the NarrativeStateManager (which also needs to be loaded early).
        if (NarrativeStateManager.Instance != null && NarrativeStateManager.Instance.IsTriggered(triggerID))
        {
            hasTriggered = true;
            foreach (var t in targets) if (t != null) Destroy(t);
            this.enabled = false;
            return;
        }

        // Filter out targets that were destroyed in a previous session
        // This must happen in Awake to prevent them from being briefly visible.
        targets.RemoveAll(t => {
            if (t == null) return true;
            var trash = t.GetComponent<TrashModule>();
            if (trash != null && SaveController.destroyedObjectIDs.Contains(trash.worldObjectID))
            {
                Destroy(t);
                return true;
            }
            return false;
        });

        // If all targets are gone (either destroyed or already removed by Awake)
        // and the narrative hasn't triggered yet, start the narrative.
        if (targets.Count == 0 && !hasTriggered) StartCoroutine(WaitAndTriggerNarrative());
    }

    private IEnumerator Start()
    {
        // Wait for potential save loading to complete
        if (SaveController.shouldLoadGame)
        {
            while (SaveController.shouldLoadGame) yield return null;
            yield return new WaitForSeconds(0.1f);
        }

        if (string.IsNullOrEmpty(triggerID))
        {
            Debug.LogError($"ObjectiveCutsceneTrigger on {gameObject.name} is missing a unique Trigger ID. It will not save/load correctly.");
        }

        if (NarrativeStateManager.Instance != null && NarrativeStateManager.Instance.IsTriggered(triggerID))
        {
            hasTriggered = true;
            this.enabled = false;
        }
    }

    private void OnEnable()
    {
        OnProgressMade += CheckObjective;
    }

    private void OnDisable()
    {
        OnProgressMade -= CheckObjective;
    }

    private void CheckObjective(GameObject target)
    {
        if (SaveController.shouldLoadGame) return;
        if (string.IsNullOrEmpty(triggerID))
        {
            Debug.LogError($"ObjectiveCutsceneTrigger on {gameObject.name} is missing a unique Trigger ID. It will not save/load correctly.");
        }
        if (NarrativeStateManager.Instance != null && NarrativeStateManager.Instance.IsTriggered(triggerID)) hasTriggered = true;
        if (hasTriggered || targets.Count == 0) return;

        // Explicitly remove the target being destroyed and any other already-null references
        targets.RemoveAll(t => t == null || t == target);

        if (targets.Count == 0) StartCoroutine(WaitAndTriggerNarrative());
    }

    private IEnumerator WaitAndTriggerNarrative()
    {
        // Wait for a clear UI and narrative state
        while ((UIManager.Instance != null && UIManager.Instance.IsUIOpen()) || (NarrativeStateManager.Instance != null && NarrativeStateManager.Instance.IsNarrativeActive) || SaveController.shouldLoadGame) yield return null;
        TriggerNarrative();
    }

    public static void NotifyProgress(GameObject target) => OnProgressMade?.Invoke(target);

    private void TriggerNarrative()
    {
        hasTriggered = true;
        NarrativeStateManager.Instance?.SetTriggered(triggerID, true); // Persist state

        if (uiToUse == NarrativeUI.CutscenePanel)
        {
            if (cutscene != null) 
                CutsceneManager.Instance?.StartCutscene(cutscene);
            else if (lines != null && lines.Length > 0) 
                CutsceneManager.Instance?.StartCutscene(lines);
        }
        else // DialoguePanel
        {
            if (cutscene != null) 
                DialogueManager.Instance?.ShowDialogue(cutscene);
            else if (lines != null && lines.Length > 0) 
                DialogueManager.Instance?.ShowDialogue(lines);
        }
        
        // Disable the script so it stops responding to events
        this.enabled = false;
    }
}