using UnityEngine;
using System.Collections;

public class DialogueTrigger : MonoBehaviour
{
    public enum TriggerType { OnStart, OnCollision, Manual }
    public enum NarrativeUI { DialoguePanel, CutscenePanel }
    
    [Header("Trigger Configuration")]
    public TriggerType type;
    public NarrativeUI uiToUse = NarrativeUI.DialoguePanel;
    public bool triggerOnce = true;
    [Tooltip("Unique ID for this trigger. Required for saving/loading its triggered state.")]
    public string triggerID;
    private bool hasTriggered;

    [Header("Content")]
    [Tooltip("If assigned, this will play a full cutscene. If empty, it will use the Dialogue Lines below.")]
    public CutsceneData cutscene;
    public DialogueLine[] lines;

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

    IEnumerator Start()
    {
        if (SaveController.shouldLoadGame)
        {
            while (SaveController.shouldLoadGame) yield return null;
            yield return new WaitForSeconds(0.1f);
        }

        if (string.IsNullOrEmpty(triggerID))
        {
            Debug.LogError($"DialogueTrigger on {gameObject.name} is missing a unique Trigger ID. It will not save/load correctly.");
        }

        // Sync local triggered state with the loaded save data
        if (triggerOnce && NarrativeStateManager.Instance != null && NarrativeStateManager.Instance.IsTriggered(triggerID))
        {
            hasTriggered = true;
        }

        if (type == TriggerType.OnStart)
        {
            if (hasTriggered && triggerOnce) yield break;
            
            // Wait until the screen is clear, load is finished, and no other narrative is active
            while ((UIManager.Instance != null && UIManager.Instance.IsUIOpen()) || (NarrativeStateManager.Instance != null && NarrativeStateManager.Instance.IsNarrativeActive) || SaveController.shouldLoadGame)
            {
                yield return null;
            }

            // Stagger the check slightly to prevent frame-perfect race conditions
            yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
            if (!hasTriggered) Trigger();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (type == TriggerType.OnCollision && other.CompareTag("Player"))
        {
            StartCoroutine(WaitAndTrigger());
        }
    }

    private IEnumerator WaitAndTrigger()
    {
        // Wait for a clear UI and narrative state
        while ((UIManager.Instance != null && UIManager.Instance.IsUIOpen()) || (NarrativeStateManager.Instance != null && NarrativeStateManager.Instance.IsNarrativeActive) || SaveController.shouldLoadGame) yield return null;
        Trigger();
    }

    public void Trigger()
    {
        if (hasTriggered && triggerOnce) return;
        if (triggerOnce && NarrativeStateManager.Instance != null && NarrativeStateManager.Instance.IsTriggered(triggerID)) return;

        // Engage the global narrative lock
        if (NarrativeStateManager.Instance != null)
            NarrativeStateManager.Instance.IsNarrativeActive = true;

        hasTriggered = true; // Set local flag
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
    }
}