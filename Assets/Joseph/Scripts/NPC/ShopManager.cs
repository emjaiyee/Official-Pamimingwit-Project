using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    [Header("Settings")]
    public float sellMultiplier = 0.5f; // Items sell for 50% of their base price
    public ItemData[] shopStock;

    void Awake()
    {
        Instance = this;
    }

    public void BuyItem(ItemData item)
    {
        if (item == null || PlayerWallet.Instance == null) return;

        if (PlayerWallet.Instance.SpendCoins(item.price))
        {
            if (Inventory.Instance.AddItem(item, 1))
            {
                UIManager.Instance?.ShowMessage($"Purchased {item.itemName}!");
            }
            else
            {
                // Refund if inventory is full
                PlayerWallet.Instance.AddCoins(item.price);
                UIManager.Instance?.ShowMessage("Inventory Full!");
            }
        }
        else
        {
            UIManager.Instance?.ShowMessage("Not enough coins!");
        }
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
        int totalValue = Mathf.RoundToInt(invItem.item.price * qualityMultiplier * sellMultiplier * invItem.amount);

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

                    PlayerWallet.Instance.AddCoins(totalValue);
                    invItem.item = null;
                    invItem.amount = 0;
                    Inventory.Instance?.OnInventoryChanged?.Invoke();
                });
            },
            "Cancel",
            null
        );
    }
}