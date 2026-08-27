using UnityEngine;

[CreateAssetMenu(fileName = "New Rod", menuName = "Inventory/Fishing Rod")]
public class FishingRodData : ItemData
{
    [Header("Rod Stats")]
    [Tooltip("Higher values reduce wait time for a bite.")]
    public float catchRateMultiplier = 1.1f;

    [Tooltip("Reduces fish escape resistance and progress loss outside safe zone. Default is 1.0.")]
    public float reelStrength = 1.0f;

    [Tooltip("Bonus added to the quality roll (e.g. 0.1 increases Gold/Silver chances by 10%).")]
    public float qualityLuckModifier = 0.0f;

    [Tooltip("Multiplier for how fast progress fills while holding inside the safe zone. Default is 1.0.")]
    public float pullPower = 1.0f;

    [Tooltip("Reduces line damping for more precise tension control.")]
    public float controlHandling = 1.0f;
}