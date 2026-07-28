using UnityEngine;

[CreateAssetMenu(fileName = "New Dynamite", menuName = "Inventory/Dynamite")]
public class DynamiteData : ItemData
{
    [Header("Dynamite Stats")]
    public int haulSize = 2;
    public int sustainabilityPenalty = -30;
}