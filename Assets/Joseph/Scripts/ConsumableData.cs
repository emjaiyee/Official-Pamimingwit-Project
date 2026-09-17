using UnityEngine;

[CreateAssetMenu(fileName = "New Consumable", menuName = "Inventory/Consumable")]
public class ConsumableData : ItemData
{
    [Header("Consumable Settings")]
    [Min(0f)]
    public float staminaRestoreAmount = 25f;

    private void OnValidate()
    {
        itemType = ItemType.Consumable;
        stackable = true;
        if (staminaRestoreAmount < 0f) staminaRestoreAmount = 0f;
    }
}
