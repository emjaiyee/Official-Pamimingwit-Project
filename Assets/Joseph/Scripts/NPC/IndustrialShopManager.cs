using UnityEngine;

public class IndustrialShopManager : MonoBehaviour
{
    public static IndustrialShopManager Instance;

    [Header("Settings")]
    public float sellMultiplier = 0.7f; // Industrial shops pay less for standard goods
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

        int totalValue = Mathf.RoundToInt(invItem.item.price * sellMultiplier * invItem.amount);

        string prompt = $"Sell {invItem.amount}x {invItem.item.itemName} for {totalValue} coins?";

        UIManager.Instance?.ShowChoice(
            prompt,
            "Sell",
            () =>
            {
                slot.AnimatePopOut(() => 
                {
                    if (invItem == null || invItem.item == null) return;

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