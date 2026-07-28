using UnityEngine;

public class CraftingResultSlot : ItemSlotUI
{
    public void CraftItem()
    {
        // This is where you'll trigger your crafting recipe logic
        Debug.Log("Crafting logic triggered for: " + (GetItem()?.item?.itemName ?? "Nothing"));
    }
}