using UnityEngine;

[CreateAssetMenu(fileName = "New Artifact", menuName = "Inventory/Artifact")]
public class ArtifactData : ItemData
{
    [Header("Passive Fishing Bonuses (Additive)")]
    [Tooltip("Increases how fast fish bite (adds to rod multiplier).")]
    public float catchRateBonus = 0f;
    [Tooltip("Reduces minigame decay speed (adds to rod reel strength).")]
    public float reelStrengthBonus = 0f;
    [Tooltip("Increases chance for Silver/Gold fish quality.")]
    public float qualityLuckBonus = 0f;
    [Tooltip("Increases progress gained per click in the minigame.")]
    public float pullPowerBonus = 0f;
}