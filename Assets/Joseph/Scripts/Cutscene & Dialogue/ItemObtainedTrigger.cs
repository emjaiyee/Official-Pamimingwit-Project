using UnityEngine;
using System.Collections;

public class ItemObtainedTrigger : MonoBehaviour
{
    public enum NarrativeUI { DialoguePanel, CutscenePanel }

    [Header("Trigger Condition")]
    [Tooltip("The item that must be in the inventory to trigger the narrative.")]
    public ItemData targetItem;
    [Tooltip("Unique ID for this trigger. Required for saving/loading its triggered state.")]
    [SerializeField]
    public string triggerID;
    public bool triggerOnce = true;

    [Header("Narrative Configuration")]
    public NarrativeUI uiToUse = NarrativeUI.DialoguePanel;
    
    [Tooltip("Optional: Full cutscene data to play.")]
    public CutsceneData cutscene;
    
    [Tooltip("Optional: Simple dialogue lines to play if no cutscene data is provided.")]
    public DialogueLine[] lines;

    private bool _hasTriggered = false;
    private bool _isWaitingToTrigger = false; // New flag to prevent multiple coroutines
    private bool _isSubscribed = false;

    private void OnEnable()
    {
        Subscribe();
    }

    private void Subscribe()
    {
        if (_isSubscribed || Inventory.Instance == null) return;
        Inventory.Instance.OnInventoryChanged += CheckInventory;
        _isSubscribed = true;
    }

    private void OnDisable()
    {
        if (Inventory.Instance != null && _isSubscribed)
        {
            Inventory.Instance.OnInventoryChanged -= CheckInventory;
            _isSubscribed = false;
        }
    }

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
            Debug.LogError($"ItemObtainedTrigger on {gameObject.name} is missing a unique Trigger ID. It will not save/load correctly.");
        }
        if (triggerOnce && NarrativeStateManager.Instance != null && NarrativeStateManager.Instance.IsTriggered(triggerID)) _hasTriggered = true;
        // Fallback subscription if Inventory wasn't ready during OnEnable
        Subscribe();

        // Perform an initial check in case the player already has the item (e.g., from a save file)
        CheckInventory();
    }

    private void CheckInventory()
    {
        if (_hasTriggered && triggerOnce) return;
        if (SaveController.shouldLoadGame) return;
        if (targetItem == null || Inventory.Instance == null) return;

        bool found = false;
        foreach (var slot in Inventory.Instance.itemList)
        {
            if (slot.item != null && slot.item == targetItem)
            {
                found = true;
                break;
            }
        }

        if (found)
        {   // If the item is found and we're not already waiting to trigger
            if (!_isWaitingToTrigger)
            {
                StartCoroutine(WaitForGameNormalStateAndTrigger());
            }
        }
    }

    private IEnumerator WaitForGameNormalStateAndTrigger()
    {
        _isWaitingToTrigger = true;

        // Wait until the game state is Normal and no other UI is open.
        // If GameManager or UIManager are missing, we skip the check to avoid an infinite loop.
        while (true)
        {
            bool isGameNormal = GameManager.Instance == null || GameManager.Instance.currentState == GameState.Normal;
            bool isUIClosed = UIManager.Instance == null || !UIManager.Instance.IsUIOpen();
            bool isNarrativeInactive = NarrativeStateManager.Instance == null || !NarrativeStateManager.Instance.IsNarrativeActive;
            bool isNotLoading = !SaveController.shouldLoadGame;

            if (isGameNormal && isUIClosed && isNarrativeInactive && isNotLoading) break;

            yield return null;
        }

        Debug.Log($"[ItemObtainedTrigger] Conditions met. Triggering narrative for {targetItem.itemName}.");
        TriggerNarrative();
        _isWaitingToTrigger = false; // Reset flag after triggering
    }

    private void TriggerNarrative()
    {
        if (targetItem == null) return;
        
        // Engage the global narrative lock
        if (NarrativeStateManager.Instance != null)
            NarrativeStateManager.Instance.IsNarrativeActive = true;

        _hasTriggered = true; // Set local flag
        NarrativeStateManager.Instance?.SetTriggered(triggerID, true); // Persist state

        if (uiToUse == NarrativeUI.CutscenePanel)
        {
            if (cutscene != null) CutsceneManager.Instance?.StartCutscene(cutscene);
            else if (lines != null && lines.Length > 0) CutsceneManager.Instance?.StartCutscene(lines);
        }
        else
        {
            if (cutscene != null) DialogueManager.Instance?.ShowDialogue(cutscene);
            else if (lines != null && lines.Length > 0) DialogueManager.Instance?.ShowDialogue(lines);
        }
    }
}