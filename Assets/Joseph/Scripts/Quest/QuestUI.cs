using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestUI : MonoBehaviour
{
    public Transform questListContent;
    public Transform mainQuestListContent;
    public GameObject questEntryPrefab;
    public ScrollRect scrollRect;

    // ✅ AUTO REFRESH WHEN PANEL OPENS
    private void OnEnable()
    {
        UpdateQuestUI();
    }

    public void UpdateQuestUI()
    {
        if (questListContent == null)
        {
            Debug.LogWarning("QuestUI: questListContent not assigned!");
            return;
        }

        if (QuestController.instance == null)
        {
            Debug.LogWarning("QuestUI: No QuestController!");
            return;
        }

        // 🔥 CLEAR OLD UI FROM BOTH LISTS
        foreach (Transform child in questListContent)
        {
            Destroy(child.gameObject);
        }
        if (mainQuestListContent != null)
        {
            foreach (Transform child in mainQuestListContent) Destroy(child.gameObject);
        }

        var quests = QuestController.instance.allProgress;

        Debug.Log($"[QuestUI] Found {quests.Count} quests");

        foreach (var quest in quests)
        {
            if (quest == null || quest.quest == null || !quest.isActive || quest.isClaimed) continue;

            // Decide which container to use
            Transform targetContainer = (quest.quest.questType == QuestType.Main && mainQuestListContent != null) 
                ? mainQuestListContent 
                : questListContent;

            GameObject entry = Instantiate(questEntryPrefab, targetContainer);
            
            // Find UI components directly on the card prefab
            TMP_Text nameText = entry.transform.Find("QuestNameText")?.GetComponent<TMP_Text>();
            TMP_Text descText = entry.transform.Find("QuestDescriptionText")?.GetComponent<TMP_Text>();
            TMP_Text objText = entry.transform.Find("QuestObjectivesText")?.GetComponent<TMP_Text>();
            TMP_Text rewardText = entry.transform.Find("QuestRewardText")?.GetComponent<TMP_Text>();
            Button cardButton = entry.GetComponent<Button>();

            if (nameText != null) nameText.text = quest.quest.questName;
            if (descText != null) descText.text = quest.quest.description;
            
            // ✅ SHOW OBJECTIVES (Consolidated into one text block)
            if (objText != null)
            {
                string objectivesSummary = "";

                // Show Days Remaining for side quests with a time limit
                if (quest.quest.questType == QuestType.Side && quest.quest.timeLimitDays > 0)
                {
                    int daysRemaining = (quest.acceptDay + quest.quest.timeLimitDays) - GameManager.Instance.currentDay;
                    daysRemaining = Mathf.Max(0, daysRemaining);
                    objectivesSummary += $"<color=red>Time Limit: {daysRemaining} days left</color>\n";
                }

                foreach (var objective in quest.objectives)
                {
                    objectivesSummary += $"- {objective.description} ({objective.currentAmount}/{objective.requiredAmount})\n";
                }
                objText.text = objectivesSummary.TrimEnd();
            }

            // ✅ SHOW REWARDS
            if (rewardText != null)
            {
                string rewardString = "Rewards: ";

                if (quest.quest.useEndingAsReward)
                {
                    rewardString += "Special Ending";
                }
                else
                {
                    bool hasRewards = false;

                    if (quest.quest.rewards != null)
                    {
                        foreach (var reward in quest.quest.rewards)
                        {
                            if (reward.coins > 0) 
                            { 
                                if (hasRewards) rewardString += ", ";
                                rewardString += $"{reward.coins} Coins"; 
                                hasRewards = true; 
                            }
                            if (reward.item != null) 
                            { 
                                if (hasRewards) rewardString += ", ";
                                rewardString += $"{reward.item.itemName} x{reward.amount}"; 
                                hasRewards = true; 
                            }
                            if (reward.sustainabilityBonus != 0)
                            {
                                if (hasRewards) rewardString += ", ";
                                rewardString += $"{(reward.sustainabilityBonus > 0 ? "+" : "")}{reward.sustainabilityBonus} Sustainability";
                                hasRewards = true;
                            }
                            if (reward.refillStamina)
                            {
                                if (hasRewards) rewardString += ", ";
                                rewardString += "Stamina Refill";
                                hasRewards = true;
                            }
                        }
                    }

                    if (!hasRewards) rewardString += "None";
                }

                rewardText.text = rewardString;
            }

            // ✅ BUTTON FUNCTIONALITY
            if (cardButton != null)
            {
                // Only allow clicking/claiming if the quest is actually finished
                cardButton.interactable = quest.isCompleted;
                
                cardButton.onClick.RemoveAllListeners();
                cardButton.onClick.AddListener(() => QuestController.instance.ClaimQuest(quest));
            }
        }

        // ✅ REFRESH LAYOUT AND SCROLLBAR
        if (scrollRect != null && scrollRect.content != null)
        {
            // Ensure the vertical scrolling functionality is active
            scrollRect.vertical = true;

            // Rebuild child containers first so the parent content knows their new size
            if (mainQuestListContent != null && mainQuestListContent is RectTransform mainRT)
                LayoutRebuilder.ForceRebuildLayoutImmediate(mainRT);
            
            if (questListContent != null && questListContent is RectTransform sideRT)
                LayoutRebuilder.ForceRebuildLayoutImmediate(sideRT);

            // Force Unity to calculate the new height of the main content container
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
        }
        
        // Reset scroll to the top
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
    }
}