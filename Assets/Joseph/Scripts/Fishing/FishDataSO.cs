using UnityEngine;

[CreateAssetMenu(menuName = "Pamimingwit/Fish")]
public class FishData : ItemData
{
    [Header("Bait & Rarity")]
    public BaitData requiredBait;
    [Range(1, 3)] public int rarity = 1;

    [Header("Physical Attributes")]
    public FishWeight weightClass = FishWeight.Small;
    public float minSize = 0.5f;
    public float maxSize = 3f;

    [Header("Wacky UI Scaling")]
    public float bronzeScale = 0.5f;
    public float silverScale = 1.2f;
    public float goldScale = 3.0f;

    [Header("Minigame Tuning (Difficulty)")]
    [Tooltip("Target progress needed to successfully catch this fish.")]
    public float baseCatchDifficulty = 100f;

    [Tooltip("How fast progress drains when tension is outside the safe zone.")]
    public float escapeResistance = 5.0f;

    [Tooltip("Intensity of random tension spikes/tugs.")]
    [Range(0.1f, 0.8f)] public float struggleIntensity = 0.25f;

    [Tooltip("Chance (0.0 to 1.0) that a struggle trigger turns into an intense Rage state.")]
    [Range(0.0f, 1.0f)] public float rageChance = 0.2f;

    [Tooltip("Multiplier applied to tension pull during a Rage state.")]
    public float rageMultiplier = 1.8f;

    [Tooltip("Width percentage of the safe tension zone (e.g. 0.3 = 30% safe window).")]
    [Range(0.15f, 0.6f)] public float safeZoneWidth = 0.4f;

    [Header("Sustainability")]
    public bool isProtectedSpecies = false;
    public int sustainabilityPenalty = -20;

    public float GetRandomSize()
    {
        return Random.Range(minSize, maxSize);
    }
}