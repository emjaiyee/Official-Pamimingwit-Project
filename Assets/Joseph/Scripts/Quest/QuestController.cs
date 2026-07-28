using System.Collections.Generic;
using UnityEngine;

public class QuestController : MonoBehaviour
{
    public static QuestController instance { get; private set; }

    public List<Quest> allQuests = new();
    public List<QuestProgress> allProgress = new();

    private QuestUI questUI;
    private int totalArtifactsInGame;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Using Start ensures Singletons like Inventory.Instance are ready
        questUI = FindFirstObjectByType<QuestUI>(FindObjectsInactive.Include);
        InitializeQuests();
        SubscribeToManagers();
    }

    private void OnEnable()
    {
        GameEvents.OnItemCaught += HandleItemCaught;
        GameManager.OnDayAdvanced += HandleDayAdvanced;

        SubscribeToManagers();
    }

    private void OnDisable()
    {
        GameEvents.OnItemCaught -= HandleItemCaught;

        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChangedExtended -= CheckInventoryForQuest;

        if (PlayerWallet.Instance != null)
            PlayerWallet.Instance.OnCoinsChanged.RemoveListener(CheckCurrencyForQuest);

        GameManager.OnDayAdvanced -= HandleDayAdvanced;
    }

    private void SubscribeToManagers()
    {
        if (Inventory.Instance != null)
        {
            Inventory.Instance.OnInventoryChangedExtended -= CheckInventoryForQuest; // Prevent double-sub
            Inventory.Instance.OnInventoryChangedExtended += CheckInventoryForQuest;
        }

        if (PlayerWallet.Instance != null)
        {
            PlayerWallet.Instance.OnCoinsChanged.RemoveListener(CheckCurrencyForQuest);
            PlayerWallet.Instance.OnCoinsChanged.AddListener(CheckCurrencyForQuest);
        }
    }

    private void HandleDayAdvanced()
    {
        int currentDay = GameManager.Instance.currentDay;

        // 1. Check currently active side quests for expiration or completion
        foreach (var p in allProgress)
        {
            if (p.quest.questType == QuestType.Side && p.isActive)
            {
                if (p.IsExpired(currentDay) || p.isClaimed)
                {
                    p.isActive = false;
                    p.ResetProgress(); // Reset so it can be shuffled back into the pool later
                }
            }
        }

        // 2. Refill slots
        FillSideQuestSlots();
        RefreshQuestStates();
    }

    private void Update() { }

    private void InitializeQuests()
    {
        if (allProgress.Count > 0) return;

        // Note: Artifact assets must be inside a "Resources" folder for this to find them
        totalArtifactsInGame = Resources.LoadAll<ArtifactData>("").Length;

        // Ensure Main Quests are always active
        foreach (var quest in allQuests)
        {
            if (quest != null && quest.questType == QuestType.Main)
            {
                var p = new QuestProgress(quest);
                p.isActive = true;
                allProgress.Add(p);
            }
            else if (quest != null)
                allProgress.Add(new QuestProgress(quest));
        }

        // Randomly pick the starting 3 side quests
        FillSideQuestSlots();

        RefreshQuestStates();
    }

    private void FillSideQuestSlots()
    {
        int currentDay = GameManager.Instance != null ? GameManager.Instance.currentDay : 1;

        // 1. Count how many side quests are still active (persisting from previous days)
        List<QuestProgress> activeSide = allProgress.FindAll(p => p.quest.questType == QuestType.Side && p.isActive);
        int activatedCount = activeSide.Count;

        if (activatedCount >= 3) return;

        // 2. Get the pool of available side quests (those not currently active)
        List<QuestProgress> sidePool = allProgress.FindAll(p => p.quest.questType == QuestType.Side && !p.isActive);

        // 3. Shuffle the available pool to provide variety
        for (int i = 0; i < sidePool.Count; i++)
        {
            QuestProgress temp = sidePool[i];
            int randomIndex = Random.Range(i, sidePool.Count);
            sidePool[i] = sidePool[randomIndex];
            sidePool[randomIndex] = temp;
        }

        // 4. Activate new quests to fill the remaining slots up to 3
        for (int i = 0; i < sidePool.Count && activatedCount < 3; i++)
        {
            sidePool[i].isActive = true;
            sidePool[i].acceptDay = currentDay;
            sidePool[i].ResetProgress(); // Ensure any previous progress is cleared
            activatedCount++;
        }
    }

    private void RefreshQuestStates()
    {
        // Re-run checks for passive objectives (Items/Money)
        CheckInventoryForQuest();

        if (PlayerWallet.Instance != null)
            CheckCurrencyForQuest(PlayerWallet.Instance.coins);

        questUI?.UpdateQuestUI();
    }

    private void HandleItemCaught(ItemData item)
    {
        if (item == null) return;

        bool catchProgressMade = false;

        foreach (var quest in allProgress)
        {
            if (!quest.isActive || quest.isClaimed) continue;
            
            foreach (var objective in quest.objectives)
            {
                if (objective.type != ObjectiveType.CollectFish || objective.isCompleted)
                    continue;

                // Check if the item matches the specific target or is in the target list
                bool isTarget = false;
                bool hasSpecificTarget = objective.targetItem != null || (objective.targetItems != null && objective.targetItems.Count > 0);

                if (objective.targetItem == item) isTarget = true;
                else if (objective.targetItems != null && objective.targetItems.Contains(item)) isTarget = true;

                // If it's a match, or if NO specific target was defined (wildcard), increment
                if (isTarget || !hasSpecificTarget)
                {
                    objective.currentAmount++;
                    catchProgressMade = true;
                }
            }
        }

        // Since catching an item also adds it to inventory, we must sync inventory-possession quests too
        CheckInventoryForQuest();

        if (catchProgressMade) questUI?.UpdateQuestUI();
    }

    public void CheckInventoryForQuest()
    {
        if (Inventory.Instance == null) return;

        // Rebuild local counts for easier reference comparison
        Dictionary<ItemData, int> counts = new Dictionary<ItemData, int>();
        HashSet<ArtifactData> uniqueArtifactsFound = new HashSet<ArtifactData>();

        foreach (var slot in Inventory.Instance.itemList)
        {
            if (slot.item == null) continue;

            if (!counts.ContainsKey(slot.item)) counts[slot.item] = 0;
            counts[slot.item] += slot.amount;

            if (slot.item is ArtifactData artifact)
                uniqueArtifactsFound.Add(artifact);
        }

        foreach (var quest in allProgress)
        {
            if (!quest.isActive || quest.isClaimed) continue;

            foreach (var objective in quest.objectives)
            {
                if (objective.type == ObjectiveType.CollectItem)
                {
                    if (objective.targetItem == null) continue;
                    int count = counts.ContainsKey(objective.targetItem) ? counts[objective.targetItem] : 0;
                    objective.currentAmount = Mathf.Min(count, objective.requiredAmount);
                }
                else if (objective.type == ObjectiveType.CollectAllArtifacts)
                {
                    objective.requiredAmount = totalArtifactsInGame;
                    objective.currentAmount = uniqueArtifactsFound.Count;
                }
            }
        }

        questUI?.UpdateQuestUI();
    }

    public void CheckCurrencyForQuest(int currentCoins)
    {
        foreach (var quest in allProgress)
        {
            if (!quest.isActive || quest.isClaimed) continue;

            foreach (var objective in quest.objectives)
            {
                if (objective.type != ObjectiveType.CollectCurrency) continue;

                int newAmount = Mathf.Min(currentCoins, objective.requiredAmount);

                if (objective.currentAmount != newAmount)
                {
                    objective.currentAmount = newAmount;
                }
            }
        }

        questUI?.UpdateQuestUI();
    }

    public void ClaimQuest(QuestProgress progress)
    {
        if (progress == null || !progress.isCompleted || progress.isClaimed) return;

        // Handle item removal if specified
        if (progress.quest.consumeItemsOnComplete)
        {
            if (!HasRequiredItems(progress))
            {
                UIManager.Instance?.ShowMessage("Missing required items in inventory!");
                RefreshQuestStates(); // Force update to show the player they are no longer eligible
                return;
            }

            foreach (var objective in progress.objectives)
            {
                if (objective.targetItem != null)
                {
                    Inventory.Instance?.RemoveItem(objective.targetItem, objective.requiredAmount);
                }
            }
        }

        if (progress.quest.useEndingAsReward)
        {
            // Handle Endings based on Sustainability
            int currentSus = SustainabilityManager.Instance != null ? SustainabilityManager.Instance.CurrentSustainability : 0;
            
            CutsceneData endingToPlay = (currentSus >= progress.quest.goodEndingThreshold) 
                ? progress.quest.goodEndingCutscene 
                : progress.quest.badEndingCutscene;

            if (endingToPlay != null)
            {
                CutsceneManager.Instance?.StartCutscene(endingToPlay);
            }
        }
        else
        {
            // Grant standard rewards
            foreach (var reward in progress.quest.rewards)
            {
                if (reward.coins > 0) PlayerWallet.Instance?.AddCoins(reward.coins);
                if (reward.item != null && reward.amount > 0) Inventory.Instance?.AddItem(reward.item, reward.amount);
                if (reward.sustainabilityBonus != 0) SustainabilityManager.Instance?.Add(reward.sustainabilityBonus);
                if (reward.refillStamina) StaminaManager.Instance?.RefillStamina();
            }
        }

        progress.isClaimed = true;
        progress.claimTime = Time.time;
        
        UIManager.Instance?.ShowMessage($"Quest Complete: {progress.quest.questName}!");

        questUI?.UpdateQuestUI();
    }

    private bool HasRequiredItems(QuestProgress progress)
    {
        if (Inventory.Instance == null) return false;

        // Temporary dictionary to track how many we need vs how many we found in this check
        Dictionary<ItemData, int> counts = new Dictionary<ItemData, int>();
        foreach (var slot in Inventory.Instance.itemList)
        {
            if (slot.item == null) continue;
            if (!counts.ContainsKey(slot.item)) counts[slot.item] = 0;
            counts[slot.item] += slot.amount;
        }

        foreach (var objective in progress.objectives)
        {
            // We only care about objectives that target a specific item
            if (objective.targetItem != null)
            {
                int available = counts.ContainsKey(objective.targetItem) ? counts[objective.targetItem] : 0;
                if (available < objective.requiredAmount) return false;
            }
            
            // Also check for "Collect All Artifacts" possession
            if (objective.type == ObjectiveType.CollectAllArtifacts)
            {
                int artifactsFound = 0;
                foreach (var item in counts.Keys)
                {
                    if (item is ArtifactData) artifactsFound++;
                }
                if (artifactsFound < totalArtifactsInGame) return false;
            }
        }

        return true;
    }

    public List<QuestProgress> GetQuestSaveData()
    {
        return allProgress;
    }

    public void LoadQuestProgress(List<QuestProgress> savedQuests)
    {
        allProgress = savedQuests ?? new List<QuestProgress>();

        CheckInventoryForQuest();

        if (PlayerWallet.Instance != null)
            CheckCurrencyForQuest(PlayerWallet.Instance.coins);

        questUI?.UpdateQuestUI();
    }
}