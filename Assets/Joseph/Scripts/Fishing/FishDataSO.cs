using UnityEngine;

[CreateAssetMenu(menuName = "Pamimingwit/Fish")]
public class FishData : ItemData
{
    [Header("Bait Preferences")]
    public BaitData requiredBait;

    [Range(1, 3)]
    public int rarity = 1;

    [Header("Wacky UI Scaling")]
    public float bronzeScale = 0.5f;
    public float silverScale = 1.2f;
    public float goldScale = 3.0f;

    public float minSize = 0.5f;
    public float maxSize = 3f;

    public FishWeight weightClass = FishWeight.Small;

    public int minBarriers = 1;
    public int maxBarriers = 3;

    public int minClicks = 10;
    public int maxClicks = 30;

    [Header("Sustainability")]
    public bool isProtectedSpecies = false;
    public int sustainabilityPenalty = -20;

    public float GetRandomSize()
    {
        return Random.Range(minSize, maxSize);
    }
}
