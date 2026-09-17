using UnityEngine;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    [Header("Settings")]
    public float sellMultiplier = 0.5f; // Items sell for 50% of their base price
    public ItemData[] shopStock;
    public ShopStockEntry[] dynamicStock;

    void Awake()
    {
        Instance = this;
        InitializeDynamicStock();
        GameManager.OnDayAdvanced += RefreshDynamicStock;
    }

    private void OnDestroy()
    {
        GameManager.OnDayAdvanced -= RefreshDynamicStock;
    }

    private void InitializeDynamicStock()
    {
        if (dynamicStock == null || dynamicStock.Length == 0)
        {
            dynamicStock = new ShopStockEntry[shopStock != null ? shopStock.Length : 0];
            for (int i = 0; i < dynamicStock.Length; i++)
                dynamicStock[i] = new ShopStockEntry { item = shopStock[i] };
        }

        RefreshDynamicStock();
    }

    private void RefreshDynamicStock()
    {
        int day = GameManager.Instance != null ? GameManager.Instance.currentDay : 1;
        foreach (ShopStockEntry entry in dynamicStock)
            entry?.RefreshForDay(day);
    }

    public List<ShopStockEntry> GetAvailableStock()
    {
        RefreshDynamicStock();
        List<ShopStockEntry> available = new();
        foreach (ShopStockEntry entry in dynamicStock)
        {
            if (entry != null && entry.item != null && entry.currentStock > 0)
                available.Add(entry);
        }
        return available;
    }

    public int GetPrice(ItemData item)
    {
        ShopStockEntry entry = FindEntry(item);
        return entry != null ? entry.GetPrice() : item != null ? item.price : 0;
    }

    private ShopStockEntry FindEntry(ItemData item)
    {
        RefreshDynamicStock();
        foreach (ShopStockEntry entry in dynamicStock)
            if (entry != null && entry.item == item) return entry;
        return null;
    }

    public void BuyItem(ItemData item)
    {
        if (item == null || PlayerWallet.Instance == null) return;

        ShopStockEntry entry = FindEntry(item);
        if (entry == null || entry.currentStock <= 0)
        {
            UIManager.Instance?.ShowMessage("This item is out of stock today.");
            return;
        }

        int price = entry.GetPrice();
        if (PlayerWallet.Instance.SpendCoins(price))
        {
            if (Inventory.Instance.AddItem(item, 1))
            {
                entry.RecordPurchase();
                UIManager.Instance?.ShowMessage($"Purchased {item.itemName}!");
                RefreshOpenShop();
            }
            else
            {
                // Refund if inventory is full
                PlayerWallet.Instance.AddCoins(price);
                UIManager.Instance?.ShowMessage("Inventory Full!");
            }

            
        }
        else
        {
            UIManager.Instance?.ShowMessage("Not enough coins!");
        }
    }

    public void RefreshOpenShop()
    {
        if (UIManager.Instance != null && UIManager.Instance.currentShopType == UIManager.ActiveShopType.General)
        UIManager.Instance.OpenShop();
    }

    public void SellItem(ItemSlotUI slot)
    {
        InventoryItem invItem = slot.GetItem();
        if (invItem == null || invItem.item == null) return;

        // Calculate quality multiplier
        float qualityMultiplier = invItem.quality switch
        {
            FishQuality.Gold => 3.0f,
            FishQuality.Silver => 1.5f,
            FishQuality.Bronze => 0.8f, // Slightly less for basic quality
            _ => 1.0f
        };

        // Calculate value based on stack size
        int totalValue = Mathf.RoundToInt(GetPrice(invItem.item) * qualityMultiplier * sellMultiplier * invItem.amount);

        string prompt = $"Sell {invItem.amount}x {invItem.item.itemName} for {totalValue} coins?";

        UIManager.Instance?.ShowChoice(
            prompt,
            "Sell",
            () =>
            {
                slot.AnimatePopOut(() => 
                {
                    if (invItem == null || invItem.item == null) return;

                    // If the item is a protected species, apply penalty
                    if (invItem.item is FishData fish && fish.isProtectedSpecies)
                    {
                        SustainabilityManager.Instance?.Add(fish.sustainabilityPenalty * invItem.amount);
                    }

                    FindEntry(invItem.item)?.RecordSale(invItem.amount);
                    PlayerWallet.Instance.AddCoins(totalValue);
                    invItem.item = null;
                    invItem.amount = 0;
                    Inventory.Instance?.OnInventoryChanged?.Invoke();
                    RefreshOpenShop();
                });
            },
            "Cancel",
            null
        );
    }
}