using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;

    public int inventorySize = 24;
    public List<InventoryItem> itemList = new List<InventoryItem>();

    public Action OnInventoryChanged;

    private Dictionary<int, int> itemsCountCache = new();
    public event Action OnInventoryChangedExtended;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        InitializeInventory();
    }

    private void InitializeInventory()
    {
        itemList.Clear();
        for (int i = 0; i < inventorySize; i++)
        {
            itemList.Add(new InventoryItem(null, 0, FishQuality.None));
        }
    }

    public bool AddItem(ItemData item, int amount = 1, FishQuality quality = FishQuality.Bronze)
    {
        if (item == null) return false;

        if (item.stackable)
        {
            foreach (var invItem in itemList)
            {
                if (invItem.item == item && invItem.quality == quality && invItem.amount < item.maxStack)
                {
                    invItem.amount += amount;
                    OnInventoryChanged?.Invoke();
                    LateInventoryUpdate();
                    return true;
                }
            }
        }

        for (int i = 0; i < itemList.Count; i++)
        {
            if (itemList[i].item == null)
            {
                itemList[i].item = item;
                itemList[i].amount = amount;
                itemList[i].quality = quality;
                OnInventoryChanged?.Invoke();
                LateInventoryUpdate();
                return true;
            }
        }

        return false;
    }

    public void RemoveItem(ItemData item, int amount)
    {
        if (item == null || amount <= 0) return;

        int remainingToRemove = amount;
        for (int i = 0; i < itemList.Count; i++)
        {
            if (itemList[i].item == item)
            {
                int take = Mathf.Min(itemList[i].amount, remainingToRemove);
                itemList[i].amount -= take;
                remainingToRemove -= take;

                if (itemList[i].amount <= 0)
                {
                    itemList[i].item = null;
                    itemList[i].amount = 0;
                    itemList[i].quality = FishQuality.None;
                }

                if (remainingToRemove <= 0) break;
            }
        }

        OnInventoryChanged?.Invoke();
        LateInventoryUpdate();
    }

    public bool TryUseSelectedHotbarConsumable()
    {
        if (HotbarManager.Instance == null || HotbarManager.Instance.selectedIndex < 0 || HotbarManager.Instance.selectedIndex >= itemList.Count)
            return false;

        return TryUseInventoryItem(itemList[HotbarManager.Instance.selectedIndex]);
    }

    public bool TryUseInventoryItem(InventoryItem slotItem)
    {
        if (slotItem == null || slotItem.item == null || !(slotItem.item is ConsumableData consumable))
            return false;

        if (StaminaManager.Instance == null)
            return false;

        float maxStamina = StaminaManager.Instance.GetEffectiveMaxStamina();
        if (StaminaManager.Instance.GetStamina() >= maxStamina)
        {
            UIManager.Instance?.ShowMessage($"{consumable.itemName} can't be used right now. You're already at full stamina.");
            return false;
        }

        if (!StaminaManager.Instance.RestoreStamina(consumable.staminaRestoreAmount))
        {
            UIManager.Instance?.ShowMessage($"{consumable.itemName} can't be used right now.");
            return false;
        }

        slotItem.amount--;
        if (slotItem.amount <= 0)
        {
            slotItem.item = null;
            slotItem.amount = 0;
            slotItem.quality = FishQuality.None;
        }

        OnInventoryChanged?.Invoke();
        LateInventoryUpdate();
        return true;
    }

    private void LateInventoryUpdate()
    {
        RebuildItemCount();
    }

    public void RebuildItemCount()
    {
        itemsCountCache.Clear();

        foreach (var slot in itemList)
        {
            if (slot.item == null) continue;

            int id = slot.item.ID;

            if (!itemsCountCache.ContainsKey(id))
                itemsCountCache[id] = 0;

            itemsCountCache[id] += slot.amount;
        }

        OnInventoryChangedExtended?.Invoke();
    }

    public float GetTotalArtifactBonus(Func<ArtifactData, float> bonusSelector)
    {
        float total = 0f;
        foreach (var slot in itemList)
        {
            if (slot.item is ArtifactData artifact)
            {
                total += bonusSelector(artifact) * slot.amount;
            }
        }
        return total;
    }

    public Dictionary<int, int> GetItemCounts() => itemsCountCache;

    public List<InventorySaveData> GetInventoryItems()
    {
        List<InventorySaveData> invData = new();

        for (int i = 0; i < itemList.Count; i++)
        {
            if (itemList[i].item != null)
            {
                invData.Add(new InventorySaveData
                {
                    ItemID = itemList[i].item.ID,
                    slotIndex = i,
                    amount = itemList[i].amount,
                    quality = itemList[i].quality
                });
            }
        }

        return invData;
    }

    public void SetInventoryItems(List<InventorySaveData> saveData)
    {
        InitializeInventory();

        foreach (var data in saveData)
        {
            if (data.slotIndex < itemList.Count)
            {
                ItemData item = GetItemDataByID(data.ItemID);

                if (item != null)
                {
                    itemList[data.slotIndex].item = item;
                    itemList[data.slotIndex].amount = data.amount;
                    itemList[data.slotIndex].quality = data.quality;
                }
            }
        }

        OnInventoryChanged?.Invoke();
        LateInventoryUpdate();
    }

    private ItemData GetItemDataByID(int id)
    {
        foreach (ItemData item in Resources.LoadAll<ItemData>(""))
        {
            if (item.ID == id)
                return item;
        }

        return null;
    }
}