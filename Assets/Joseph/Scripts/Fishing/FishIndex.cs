using System.Collections.Generic;
using UnityEngine;

public class FishIndex : MonoBehaviour
{
    public static FishIndex Instance { get; private set; }

    private readonly HashSet<int> revealedFishIds = new HashSet<int>();
    private List<FishData> allFishCache;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadAllFish();
    }

    private void OnEnable()
    {
        GameEvents.OnItemCaught += HandleItemCaught;
    }

    private void OnDisable()
    {
        GameEvents.OnItemCaught -= HandleItemCaught;
    }

    public static FishIndex EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        GameObject obj = new GameObject("FishIndex");
        return obj.AddComponent<FishIndex>();
    }

    private void LoadAllFish()
    {
        if (allFishCache != null)
            return;

        FishData[] fishAssets = Resources.LoadAll<FishData>("Fishes");
        if (fishAssets == null || fishAssets.Length == 0)
            fishAssets = Resources.LoadAll<FishData>("");

        allFishCache = new List<FishData>(fishAssets);
    }

    public IReadOnlyList<FishData> GetAllFish()
    {
        LoadAllFish();
        return allFishCache;
    }

    public bool IsUnlocked(FishData fish)
    {
        return IsFishUnlocked(fish);
    }

    public bool IsUnlocked(int fishId)
    {
        return IsFishUnlocked(fishId);
    }

    public bool IsFishUnlocked(FishData fish)
    {
        return fish != null && revealedFishIds.Contains(fish.ID);
    }

    public bool IsFishUnlocked(int fishId)
    {
        return revealedFishIds.Contains(fishId);
    }

    public bool IsFishLocked(FishData fish)
    {
        return !IsFishUnlocked(fish);
    }

    public bool IsFishLocked(int fishId)
    {
        return !IsFishUnlocked(fishId);
    }

    public void RevealFish(FishData fish)
    {
        if (fish == null)
            return;

        revealedFishIds.Add(fish.ID);
    }

    public void RevealFish(int fishId)
    {
        if (fishId <= 0)
            return;

        revealedFishIds.Add(fishId);
    }

    public void RegisterCaughtFish(FishData fish)
    {
        RevealFish(fish);
    }

    public void RegisterCaughtFish(ItemData item)
    {
        HandleItemCaught(item);
    }

    public void LoadUnlockedFishIds(List<int> revealedIds)
    {
        revealedFishIds.Clear();

        if (revealedIds == null)
            return;

        foreach (int fishId in revealedIds)
        {
            if (fishId > 0)
                revealedFishIds.Add(fishId);
        }
    }

    public List<int> GetUnlockedFishIds()
    {
        List<int> ids = new List<int>();
        foreach (int fishId in revealedFishIds)
        {
            ids.Add(fishId);
        }

        ids.Sort();
        return ids;
    }

    public List<FishData> GetUnlockedFish()
    {
        LoadAllFish();
        List<FishData> unlocked = new List<FishData>();

        foreach (FishData fish in allFishCache)
        {
            if (fish != null && IsFishUnlocked(fish))
                unlocked.Add(fish);
        }

        return unlocked;
    }

    public List<FishData> GetLockedFish()
    {
        LoadAllFish();
        List<FishData> locked = new List<FishData>();

        foreach (FishData fish in allFishCache)
        {
            if (fish != null && !IsFishUnlocked(fish))
                locked.Add(fish);
        }

        return locked;
    }

    public int UnlockedCount => revealedFishIds.Count;

    public int LockedCount
    {
        get
        {
            LoadAllFish();
            int locked = 0;
            foreach (FishData fish in allFishCache)
            {
                if (fish != null && !IsFishUnlocked(fish))
                    locked++;
            }

            return locked;
        }
    }

    public void HandleItemCaught(ItemData item)
    {
        if (item is FishData fish)
            RevealFish(fish);
    }
}
