using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class TieredDialogue
{
    public string tierName; // Must match the tierName in ReactiveOceanManager
    public DialogueLine[] lines;

    [Header("Milestone Reward")]
    public int rewardCoins;
    public ItemData rewardItem;
    public int rewardAmount = 1;
}

public class MangAmboModule : NPCModule
{
    [Header("Dialogue Content")]
    [SerializeField] private DialogueLine[] defaultDialogue;
    [SerializeField] private List<TieredDialogue> tieredDialogues = new List<TieredDialogue>();

    private List<string> claimedTiers = new List<string>();

    public override string GetInteractionPrompt()
    {
        return "Speak with Mang Ambo [E]";
    }

    public override void OnInteract()
    {
        if (DialogueManager.Instance == null)
        {
            Debug.LogWarning("MangAmboModule: DialogueManager instance not found in scene.");
            return;
        }

        DialogueLine[] linesToDisplay = defaultDialogue;
        TieredDialogue activeTierMatch = null;

        // Query the ReactiveOceanManager for the current state
        if (ReactiveOceanManager.Instance != null)
        {
            OceanTier currentTier = ReactiveOceanManager.Instance.GetCurrentTier();
            if (currentTier != null)
            {
                // Look for dialogue specific to this tier name
                activeTierMatch = tieredDialogues.Find(t => t.tierName == currentTier.tierName);

                bool alreadyClaimed = activeTierMatch != null && claimedTiers.Contains(activeTierMatch.tierName);

                // Only show tier-specific dialogue if it hasn't been "completed" (rewarded) yet.
                // Otherwise, fall back to default dialogue so he doesn't repeat the milestone speech.
                if (activeTierMatch != null && !alreadyClaimed && activeTierMatch.lines != null && activeTierMatch.lines.Length > 0)
                {
                    linesToDisplay = activeTierMatch.lines;
                }
                else if (activeTierMatch == null)
                {
                    Debug.LogFormat("[MangAmboModule] No specific dialogue found for tier: {0}. Using default.", currentTier.tierName);
                }
            }
        }

        // Show dialogue and attempt to give reward once the conversation ends
        DialogueManager.Instance.ShowDialogue(linesToDisplay, () => 
        {
            if (activeTierMatch != null)
            {
                TryGiveReward(activeTierMatch);
            }
        });
    }

    private void TryGiveReward(TieredDialogue tieredData)
    {
        // Don't give the reward if it's already been claimed for this tier
        if (claimedTiers.Contains(tieredData.tierName)) return;

        bool hasCoins = tieredData.rewardCoins > 0;
        bool hasItem = tieredData.rewardItem != null;

        // If there's nothing to give, just return
        if (!hasCoins && !hasItem) return;

        if (hasCoins)
        {
            PlayerWallet.Instance?.AddCoins(tieredData.rewardCoins);
        }

        if (hasItem && Inventory.Instance != null)
        {
            Inventory.Instance.AddItem(tieredData.rewardItem, tieredData.rewardAmount);
        }

        claimedTiers.Add(tieredData.tierName);
        UIManager.Instance?.ShowMessage($"Mang Ambo gave you a reward for your efforts!");
    }
}