using System;

[Serializable]
public class InventoryItem
{
    public ItemData item;
    public int amount;
    public FishQuality quality;

    public InventoryItem(ItemData item, int amount, FishQuality quality = FishQuality.Bronze)
    {
        this.item = item;
        this.amount = amount;
        this.quality = quality;
    }

    public InventoryItem Clone()
    {
        return new InventoryItem(this.item, this.amount, this.quality);
    }
}