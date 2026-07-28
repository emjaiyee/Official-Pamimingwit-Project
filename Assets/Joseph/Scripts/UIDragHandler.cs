using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class UIDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    private ItemSlotUI thisSlot;
    private Canvas mainCanvas;
    public static ItemSlotUI draggedSlot;
    public static int dragAmount;

    private float holdTimer;
    private float nextIncrementTime;
    private const float INITIAL_HOLD_DELAY = 0.5f;
    private const float INCREMENT_RATE = 0.05f;

    void Awake()
    {
        thisSlot = GetComponent<ItemSlotUI>();
        mainCanvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        InventoryItem item = GetItemFromSlot(thisSlot);
        if (item == null || item.item == null) return;

        draggedSlot = thisSlot;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Shift + RMB instantly grabs half the stack (minimum 1)
            if (Keyboard.current != null && Keyboard.current.shiftKey.isPressed)
                dragAmount = Mathf.Max(1, item.amount / 2);
            else
                dragAmount = 1;
        }
        else
        {
            dragAmount = item.amount;
        }

        holdTimer = 0;
        nextIncrementTime = 0;

        DragItemUI.Instance?.StartDrag(item.item, thisSlot.icon.color, thisSlot.GetBaseScale());
        TooltipUI.Instance?.HideTooltip();
    }

    public void OnDrag(PointerEventData eventData) { } // Handled by DragItemUI

    void Update()
    {
        // Only the slot instance currently being dragged should handle the increment logic
        if (draggedSlot != thisSlot) return;

        // Check if RMB is still being held
        if (Mouse.current != null && Mouse.current.rightButton.isPressed)
        {
            InventoryItem sourceItem = GetItemFromSlot(thisSlot);
            if (sourceItem == null || sourceItem.item == null) return;

            if (dragAmount < sourceItem.amount)
            {
                holdTimer += Time.deltaTime;

                // Wait for initial delay before rapid incrementing starts
                if (holdTimer >= INITIAL_HOLD_DELAY)
                {
                    nextIncrementTime += Time.deltaTime;
                    if (nextIncrementTime >= INCREMENT_RATE)
                    {
                        dragAmount++;
                        nextIncrementTime = 0;
                        // Note: If DragItemUI has an amount display, update it here:
                        // DragItemUI.Instance?.UpdateAmount(dragAmount);
                    }
                }
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        draggedSlot = null;
        DragItemUI.Instance?.StopDrag();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (draggedSlot == null || draggedSlot == thisSlot) return;

        InventoryItem sourceItem = GetItemFromSlot(draggedSlot);
        InventoryItem targetItem = GetItemFromSlot(thisSlot);

        if (sourceItem == null || sourceItem.item == null) return;

        int amountToMove = Mathf.Min(sourceItem.amount, dragAmount);

        // CASE 1: Target slot is empty - Move the selected amount
        if (targetItem == null || targetItem.item == null)
        {
            InventoryItem newItem = sourceItem.Clone();
            newItem.amount = amountToMove;
            SetItemInSlot(thisSlot, newItem);

            sourceItem.amount -= amountToMove;
            if (sourceItem.amount <= 0) ClearSlot(draggedSlot);
        }
        // CASE 2: Same item type - Try to stack
        else if (targetItem.item == sourceItem.item && sourceItem.item.stackable)
        {
            int roomLeft = targetItem.item.maxStack - targetItem.amount;
            int actualMove = Mathf.Min(amountToMove, roomLeft);

            if (actualMove > 0)
            {
                targetItem.amount += actualMove;
                sourceItem.amount -= actualMove;

                if (sourceItem.amount <= 0)
                    ClearSlot(draggedSlot);
            }
        }
        // CASE 3: Different items - Swap them (Only if dragging whole stack)
        else if (amountToMove == sourceItem.amount)
        {
            SwapSlots(draggedSlot, thisSlot, sourceItem, targetItem);
        }

        Inventory.Instance?.OnInventoryChanged?.Invoke();
    }

    private InventoryItem GetItemFromSlot(ItemSlotUI slot)
    {
        if (slot.type == SlotType.Inventory || slot.type == SlotType.Hotbar)
            return Inventory.Instance.itemList[slot.index];
        return slot.GetItem();
    }

    private void SetItemInSlot(ItemSlotUI slot, InventoryItem item)
    {
        if (slot.type == SlotType.Inventory || slot.type == SlotType.Hotbar)
            Inventory.Instance.itemList[slot.index] = item;
        else
            slot.SetItem(item);
    }

    private void ClearSlot(ItemSlotUI slot) => SetItemInSlot(slot, new InventoryItem(null, 0));

    private void SwapSlots(ItemSlotUI slotA, ItemSlotUI slotB, InventoryItem itemA, InventoryItem itemB)
    {
        SetItemInSlot(slotA, itemB.Clone());
        SetItemInSlot(slotB, itemA.Clone());
    }
}