using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryTrashBinUI : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        // Access the item being dragged via the static reference in UIDragHandler
        ItemSlotUI sourceSlot = UIDragHandler.draggedSlot;

        if (sourceSlot == null) return;
        
        // Ensure we are dragging from a valid inventory or hotbar slot
        if (sourceSlot.type != SlotType.Inventory && sourceSlot.type != SlotType.Hotbar) return;

        // Check inventory bounds
        if (Inventory.Instance == null || sourceSlot.index >= Inventory.Instance.itemList.Count) return;

        InventoryItem invItem = Inventory.Instance.itemList[sourceSlot.index];

        if (invItem == null || invItem.item == null) return;

        ItemData item = invItem.item;

        // Animate the item disappearing before actually removing it from the data
        sourceSlot.AnimatePopOut(() => 
        {
            // Disposal Logic with Sustainability impact
            if (item is FishData)
            {
                // Penalty for wasting biological resources in a bin
                SustainabilityManager.Instance?.Add(-5);
                UIManager.Instance?.ShowMessage($"Threw away {item.itemName}. That's wasteful!");
            }
            else if (item.itemType == ItemType.Junk)
            {
                // Reward for cleaning up junk correctly
                SustainabilityManager.Instance?.Add(1);
                UIManager.Instance?.ShowMessage($"Properly disposed of {item.itemName}.");
            }
            else
            {
                UIManager.Instance?.ShowMessage($"Discarded {item.itemName}.");
            }

            // Consume one from the stack
            invItem.amount--;
            if (invItem.amount <= 0)
            {
                invItem.item = null;
                invItem.amount = 0;
                invItem.quality = FishQuality.None;
            }

            // Update UI visuals
            Inventory.Instance.OnInventoryChanged?.Invoke();
            
            // Hide tooltip as the item is gone
            TooltipUI.Instance?.HideTooltip();
        });
    }
}