using UnityEngine;
using System.Collections.Generic;
using System;

[CreateAssetMenu(menuName = "Quests/Quest")]
public class Quest : ScriptableObject
{
    public string questID;
    public string questName;
    public string description;

    public QuestType questType;

    public List<QuestObjective> objectives;
    public List<QuestReward> rewards;

    public bool consumeItemsOnComplete;
    public bool useEndingAsReward;
    [Header("Ending Cutscenes")]
    public CutsceneData goodEndingCutscene;
    public CutsceneData badEndingCutscene;
    public int goodEndingThreshold = 50; // Required sustainability for the good ending

    [Header("Recycle Settings")]
    public bool canRecycle;
    public float recycleCooldown = 300f; // Time in seconds before it reappears

    [Header("Time Limit Settings")]
    public int timeLimitDays = -1; // -1 for no limit

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(questID))
        {
            questID = Guid.NewGuid().ToString();
        }
    }
#endif
}

[Serializable]
public class QuestReward
{
    public ItemData item;
    public int amount;
    public int coins;
    public int sustainabilityBonus;
    public bool refillStamina;
}

[System.Serializable]
public class QuestObjective
{
    public ItemData targetItem;
    public string description;
    public ObjectiveType type;

    public List<ItemData> targetItems = new();

    public int requiredAmount;
    public int currentAmount;

    public bool isCompleted => currentAmount >= requiredAmount;
}

public enum ObjectiveType
{
    CollectItem,
    CollectFish,
    talkNPC,
    CollectCurrency,
    Custom,
    CollectAllArtifacts
}

public enum QuestType
{
    Main,
    Side
}

[System.Serializable]
public class QuestProgress
{
    public Quest quest;
    public List<QuestObjective> objectives;
    public bool isClaimed;
    public float claimTime;
    public bool isActive;
    public int acceptDay;

    public QuestProgress(Quest quest)
    {
        this.quest = quest;
        this.acceptDay = GameManager.Instance != null ? GameManager.Instance.currentDay : 1;
        objectives = new List<QuestObjective>();

        foreach (var obj in quest.objectives)
        {
            objectives.Add(new QuestObjective
            {
                targetItem = obj.targetItem,
                description = obj.description,
                type = obj.type,
                targetItems = obj.targetItems != null
                    ? new List<ItemData>(obj.targetItems)
                    : new List<ItemData>(),
                requiredAmount = obj.requiredAmount,
                currentAmount = 0
            });
        }
    }

    public bool isCompleted => objectives.TrueForAll(o => o.isCompleted);

    public bool IsExpired(int currentDay)
    {
        if (quest.timeLimitDays <= 0) return false;
        // If today is the day it should have been finished, it's expired
        return currentDay >= (acceptDay + quest.timeLimitDays);
    }

    public void ResetProgress()
    {
        isClaimed = false;
        claimTime = 0;
        foreach (var obj in objectives)
        {
            obj.currentAmount = 0;
        }
    }
}