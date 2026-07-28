using UnityEngine;
using System.Collections;

public class StaminaThresholdTrigger : MonoBehaviour
{
    public enum NarrativeUI { DialoguePanel, CutscenePanel }

    [Header("Trigger Condition")]
    [Tooltip("The amount of stamina below which the narrative triggers.")]
    public float threshold = 0f;
    [Tooltip("If true, this will only ever play once. If false, it acts as a reminder every time stamina hits the threshold.")]
    public bool triggerOnce = true;
    [Tooltip("Unique ID for this trigger. Required for saving/loading its triggered state.")]
    [SerializeField]
    public string triggerID;

    [Header("Narrative Configuration")]
    public NarrativeUI uiToUse = NarrativeUI.DialoguePanel;
    
    [Tooltip("First time: Full cutscene data to play.")]
    public CutsceneData cutscene;
    
    [Tooltip("First time: Simple dialogue lines to play if no cutscene data is provided.")]
    public DialogueLine[] lines;

    [Header("Reminder Configuration (Optional)")]
    [Tooltip("Subsequent times: If left empty, the original lines will be used.")]
    public CutsceneData reminderCutscene;
    public DialogueLine[] reminderLines;

    private bool _hasTriggeredAtLeastOnce = false;
    private bool _isCurrentlyExhausted = false;
    private bool _isWaitingToTrigger = false;
    private bool _isSubscribed = false;

    private void OnEnable()
    {
        Subscribe();
    }

    private void Subscribe()
    {
        if (_isSubscribed || StaminaManager.Instance == null) return;
        StaminaManager.Instance.OnStaminaChanged += CheckStamina;
        _isSubscribed = true;
    }

    private void OnDisable()
    {
        if (StaminaManager.Instance != null && _isSubscribed)
        {
            StaminaManager.Instance.OnStaminaChanged -= CheckStamina;
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
            //Debug.LogError($"StaminaThresholdTrigger on {gameObject.name} is missing a unique Trigger ID. It will not save/load correctly.");
        }
        if (triggerOnce && NarrativeStateManager.Instance != null && NarrativeStateManager.Instance.IsTriggered(triggerID)) _hasTriggeredAtLeastOnce = true;
        Subscribe();

        // If we are already below the threshold upon starting (e.g. loading a save), 
        // mark as exhausted to prevent immediate triggering.
        if (StaminaManager.Instance != null && StaminaManager.Instance.GetStamina() <= threshold)
        {
            _isCurrentlyExhausted = true;
        }

        // Initial check logic from CoinThresholdTrigger
        if (StaminaManager.Instance != null)
        {
            CheckStamina(StaminaManager.Instance.GetStamina(), 100f);
        }
    }

    private void CheckStamina(float currentStamina, float maxStamina)
    {
        if (_hasTriggeredAtLeastOnce && triggerOnce) return;
        if (SaveController.shouldLoadGame) return;
        //Debug.Log($"[StaminaThresholdTrigger:{gameObject.name}] CheckStamina called. Current: {currentStamina}, Threshold: {threshold}. HasTriggered: {_hasTriggeredAtLeastOnce}, TriggerOnce: {triggerOnce}.");

        if (currentStamina <= threshold)
        {
            // Only trigger if we haven't already notified the player for THIS specific drop to 0
            if (!_isCurrentlyExhausted && !_isWaitingToTrigger)
            {
                // Lock the exhaustion state immediately to prevent repeated triggers
                _isCurrentlyExhausted = true;
        
                StartCoroutine(WaitForGameNormalStateAndTrigger());
            }
        }
        else if (currentStamina > threshold + 5f) 
        {
            // Reset the exhaustion flag once stamina recovers slightly above the threshold
            _isCurrentlyExhausted = false;
        }
    }

    private IEnumerator WaitForGameNormalStateAndTrigger()
    {
        _isWaitingToTrigger = true;

        // Wait until the game state is Normal and no other UI is open to avoid overlapping.
        while (true)
        {
            bool isGameNormal = GameManager.Instance == null || GameManager.Instance.currentState == GameState.Normal;
            bool isUIClosed = UIManager.Instance == null || !UIManager.Instance.IsUIOpen();
            bool isNarrativeInactive = NarrativeStateManager.Instance == null || !NarrativeStateManager.Instance.IsNarrativeActive;
            bool isNotLoading = !SaveController.shouldLoadGame;

            if (isGameNormal && isUIClosed && isNarrativeInactive && isNotLoading) break;
            yield return null;
        }

        TriggerNarrative();
        _isWaitingToTrigger = false;
    }

    private void TriggerNarrative()
    {
        // Engage the global narrative lock
        if (NarrativeStateManager.Instance != null)
            NarrativeStateManager.Instance.IsNarrativeActive = true;

        // Determine if we should use the first-time content or the reminder content
        // Use the persisted state from NarrativeStateManager for 'useReminder' logic
        bool useReminder = NarrativeStateManager.Instance != null && NarrativeStateManager.Instance.IsTriggered(triggerID) && (reminderCutscene != null || (reminderLines != null && reminderLines.Length > 0));
        
        _hasTriggeredAtLeastOnce = true;

        if (uiToUse == NarrativeUI.CutscenePanel)
        {
            CutsceneData targetCutscene = useReminder ? reminderCutscene : cutscene;
            DialogueLine[] targetLines = useReminder ? reminderLines : lines;

            if (targetCutscene != null) CutsceneManager.Instance?.StartCutscene(targetCutscene);
            else if (targetLines != null && targetLines.Length > 0) CutsceneManager.Instance?.StartCutscene(targetLines);
        }
        else
        {
            CutsceneData targetCutscene = useReminder ? reminderCutscene : cutscene;
            DialogueLine[] targetLines = useReminder ? reminderLines : lines;

            if (targetCutscene != null) DialogueManager.Instance?.ShowDialogue(targetCutscene);
            else if (targetLines != null && targetLines.Length > 0) DialogueManager.Instance?.ShowDialogue(targetLines);
        }

        NarrativeStateManager.Instance?.SetTriggered(triggerID, true); // Persist state
    }
}