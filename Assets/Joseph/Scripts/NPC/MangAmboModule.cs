using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class TieredDialogue
{
    public string tierName;
    public DialogueLine[] lines;

    [Header("Milestone Reward")]
    public int rewardCoins;
    public ItemData rewardItem;
    public int rewardAmount = 1;
}

public class MangAmboModule : NPCModule
{
    [Serializable]
    private class ClaimedTiersWrapper
    {
        public List<string> tiers = new List<string>();
    }

    [Header("Persistence ID")]
    [SerializeField] private string moduleSaveID = "MangAmbo_ClaimedTiers";

    [Header("Dialogue Content")]
    [SerializeField] private DialogueLine[] defaultDialogue;
    [SerializeField] private List<TieredDialogue> tieredDialogues = new List<TieredDialogue>();

    private readonly HashSet<string> claimedTiers = new HashSet<string>();

    private void Awake()
    {
        LoadClaimedTiers();
    }

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

        if (ReactiveOceanManager.Instance != null)
        {
            OceanTier currentTier = ReactiveOceanManager.Instance.GetCurrentTier();
            if (currentTier != null)
            {
                TieredDialogue match = tieredDialogues.Find(t => t.tierName == currentTier.tierName);
                if (match != null && match.lines != null && match.lines.Length > 0)
                {
                    linesToDisplay = match.lines;
                    if (!claimedTiers.Contains(match.tierName))
                    {
                        activeTierMatch = match;
                    }
                }
            }
        }

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
        if (claimedTiers.Contains(tieredData.tierName)) return;

        bool hasCoins = tieredData.rewardCoins > 0;
        bool hasItem = tieredData.rewardItem != null;

        if (!hasCoins && !hasItem) return;

        if (hasItem && Inventory.Instance != null)
        {
            bool added = Inventory.Instance.AddItem(tieredData.rewardItem, tieredData.rewardAmount);
            if (!added)
            {
                UIManager.Instance?.ShowMessage("Inventory full! Free up space to claim your reward.");
                return;
            }
        }

        if (hasCoins)
        {
            PlayerWallet.Instance?.AddCoins(tieredData.rewardCoins);
        }

        claimedTiers.Add(tieredData.tierName);
        SaveClaimedTiers();

        UIManager.Instance?.ShowMessage("Mang Ambo gave you a reward for your efforts!");
    }

    private void SaveClaimedTiers()
    {
        ClaimedTiersWrapper wrapper = new ClaimedTiersWrapper();
        wrapper.tiers.AddRange(claimedTiers);
        string json = JsonUtility.ToJson(wrapper);
        
        PlayerPrefs.SetString(moduleSaveID, json);
        PlayerPrefs.Save();
    }

    private void LoadClaimedTiers()
    {
        claimedTiers.Clear();
        if (PlayerPrefs.HasKey(moduleSaveID))
        {
            string json = PlayerPrefs.GetString(moduleSaveID);
            ClaimedTiersWrapper wrapper = JsonUtility.FromJson<ClaimedTiersWrapper>(json);
            if (wrapper != null && wrapper.tiers != null)
            {
                foreach (string t in wrapper.tiers)
                {
                    claimedTiers.Add(t);
                }
            }
        }
    }
}