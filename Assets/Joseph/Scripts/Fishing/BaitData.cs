using UnityEngine;

[CreateAssetMenu(fileName = "New Bait", menuName = "Inventory/Bait")]
public class BaitData : ItemData
{
    [Header("Bait Stats")]
    [Tooltip("Multiplier for how much faster a fish will bite.")]
    public float catchRateBonus = 1.5f;

    [Tooltip("Adds extra time (seconds) to the reaction window when a fish bites.")]
    public float biteWindowExtension = 0.5f;

    [Tooltip("Reduces fish struggle intensity during the reeling minigame (e.g. 0.1 reduces struggle by 10%).")]
    public float struggleReduction = 0.0f;
}