using System;
using UnityEngine;

[Flags]
public enum ShopDay
{
    None = 0,
    Monday = 1 << 0,
    Tuesday = 1 << 1,
    Wednesday = 1 << 2,
    Thursday = 1 << 3,
    Friday = 1 << 4,
    Saturday = 1 << 5,
    Sunday = 1 << 6,
    All = Monday | Tuesday | Wednesday | Thursday | Friday | Saturday | Sunday
}

[Serializable]
public class ShopStockEntry
{
    public ItemData item;
    [Min(0)] public int dailyStock = 5;
    [Min(0)] public int stockVariance;
    public ShopDay availableDays = ShopDay.All;
    [Range(0f, 2f)] public float demandSensitivity = 0.5f;

    [NonSerialized] public int currentStock;
    [NonSerialized] public float demand;
    [NonSerialized] public int initializedDay = -1;

    public bool IsAvailableToday(int day)
    {
        ShopDay today = (ShopDay)(1 << ((Mathf.Max(1, day) - 1) % 7));
        return (availableDays & today) != 0;
    }

    public void RefreshForDay(int day)
    {
        if (initializedDay == day) return;

        initializedDay = day;
        currentStock = IsAvailableToday(day)
            ? dailyStock + (stockVariance > 0 ? UnityEngine.Random.Range(-stockVariance, stockVariance + 1) : 0)
            : 0;
        currentStock = Mathf.Max(0, currentStock);
        demand = 0f;
    }

    public int GetPrice()
    {
        if (item == null) return 0;

        float supplyFactor = dailyStock <= 0 ? 1f : 1f + ((dailyStock - currentStock) / (float)dailyStock) * demandSensitivity;
        float demandFactor = 1f + demand * demandSensitivity;
        return Mathf.Max(1, Mathf.RoundToInt(item.price * supplyFactor * demandFactor));
    }

    public void RecordPurchase()
    {
        currentStock = Mathf.Max(0, currentStock - 1);
        demand += 0.1f;
    }

    public void RecordSale(int amount)
    {
        currentStock += Mathf.Max(0, amount);
        demand = Mathf.Max(0f, demand - (0.1f * amount));
    }
}
