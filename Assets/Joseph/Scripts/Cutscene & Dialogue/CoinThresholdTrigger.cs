using UnityEngine;
using System.Collections;

public class CoinThresholdTrigger : MonoBehaviour
{
    public enum NarrativeUI { DialoguePanel, CutscenePanel }

    [Header("Trigger Condition")]
    [Tooltip("The amount of coins required to trigger the narrative.")]
    public int targetCoins = 100;
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
    private bool _isWaitingToTrigger = false;
    private bool _isSubscribed = false;

    private void OnEnable()
    {
        Subscribe();
    }

    private void Subscribe()
    {
        if (_isSubscribed || PlayerWallet.Instance == null) return;
        PlayerWallet.Instance.OnCoinsChanged.AddListener(CheckCoins);
        _isSubscribed = true;
    }

    private void OnDisable()
    {
        if (PlayerWallet.Instance != null && _isSubscribed)
        {
            PlayerWallet.Instance.OnCoinsChanged.RemoveListener(CheckCoins);
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
            Debug.LogError($"CoinThresholdTrigger on {gameObject.name} is missing a unique Trigger ID. It will not save/load correctly.");
        }
        if (triggerOnce && NarrativeStateManager.Instance != null && NarrativeStateManager.Instance.IsTriggered(triggerID)) _hasTriggered = true;
        Subscribe();
        // Initial check in case the player already meets the criteria
        if (PlayerWallet.Instance != null)
        {
            CheckCoins(PlayerWallet.Instance.coins);
        }
    }

    private void CheckCoins(int currentCoins)
    {
        if (_hasTriggered && triggerOnce) return;
        if (SaveController.shouldLoadGame) return;
        
        if (currentCoins >= targetCoins)
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