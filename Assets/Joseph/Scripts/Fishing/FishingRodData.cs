using UnityEngine;

[CreateAssetMenu(fileName = "New Rod", menuName = "Inventory/Fishing Rod")]
public class FishingRodData : ItemData
{
    [Header("Rod Stats")]
    [Tooltip("Higher values reduce wait time for a bite.")]
    public float catchRateMultiplier = 1.1f;

    [Tooltip("Reduces the difficulty (decay speed) of the reeling minigame. 1.0 is default.")]
    public float reelStrength = 1.0f;

    [Tooltip("Bonus added to the quality roll (e.g. 0.1 increases Gold/Silver chances by 10%).")]
    public float qualityLuckModifier = 0.0f;

    [Tooltip("How much progress is gained per click. 1.0 is default.")]
    public float pullPower = 1.0f;
}