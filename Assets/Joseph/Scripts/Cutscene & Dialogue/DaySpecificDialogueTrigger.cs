using UnityEngine;
using System.Collections;

public class DaySpecificDialogueTrigger : MonoBehaviour
{
    public enum NarrativeUI { DialoguePanel, CutscenePanel }

    [Header("Trigger Condition")]
    public int targetDay = 4;
    public bool triggerOnce = true;
    [Tooltip("Unique ID for this trigger. Required for saving/loading its triggered state.")]
    [SerializeField]
    public string triggerID;

    [Header("Narrative Configuration")]
    public NarrativeUI uiToUse = NarrativeUI.DialoguePanel;
    
    [Tooltip("Optional: Full cutscene data to play.")]
    public CutsceneData cutscene;
    
    [Tooltip("Optional: Simple dialogue lines to play if no cutscene data is provided.")]
    public DialogueLine[] lines;

    private bool _hasTriggered = false;
    private bool _isWaitingToTrigger = false;

    private void OnEnable()
    {
        GameManager.OnDayAdvanced += CheckDay;
    }

    private void OnDisable()
    {
        GameManager.OnDayAdvanced -= CheckDay;
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
            Debug.LogError($"DaySpecificDialogueTrigger on {gameObject.name} is missing a unique Trigger ID. It will not save/load correctly.");
        }
        if (triggerOnce && NarrativeStateManager.Instance != null && NarrativeStateManager.Instance.IsTriggered(triggerID)) _hasTriggered = true;
        // Perform an initial check in case the game starts on the target day
        CheckDay();
    }

    private void CheckDay()
    {
        if (_hasTriggered && triggerOnce) return;
        if (SaveController.shouldLoadGame) return;
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.currentDay == targetDay)
        {
            if (!_isWaitingToTrigger)
            {
                StartCoroutine(WaitForGameNormalStateAndTrigger());
            }
        }
    }

    private IEnumerator WaitForGameNormalStateAndTrigger()
    {
        _isWaitingToTrigger = true;

        // Wait until the game state is Normal and no other UI is open to avoid overlapping with the day transition.
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