using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance;

    void Awake()
    {
        Instance = this;
    }

    public bool isToolEquipped
    {
        get
        {
            ItemData held = PlayerController.Instance?.GetHeldItem();
            return held != null && held.itemType == ItemType.Tool;
        }
    }

    public bool hasFishingRodEquipped
    {
        get
        {
            ItemData held = PlayerController.Instance?.GetHeldItem();
            // Checks if the held item exists, is classified as a Tool, and is a fishing rod
            return held != null && held.itemType == ItemType.Tool && held.itemName.ToLower().Contains("rod");
        }
    }

    public bool hasDynamiteEquipped
    {
        get
        {
            ItemData held = PlayerController.Instance?.GetHeldItem();
            if (held == null) return false;
            
            bool isDynamiteType = held is DynamiteData;
            bool nameContainsDynamite = held.itemName.ToLower().Contains("dynamite");
            //DEBUG THINGGG
            //Debug.Log($"[EquipmentManager] Checking held item: {held.itemName} (Type: {held.GetType().Name}). IsDynamiteType: {isDynamiteType}, NameContainsDynamite: {nameContainsDynamite}");
            return isDynamiteType || nameContainsDynamite;
        }
    }
}