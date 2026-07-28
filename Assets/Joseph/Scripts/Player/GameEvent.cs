using System;

public static class GameEvents
{
    // Fishing
    public static Action<FishData> OnFishHooked;
    public static Action<ItemData> OnItemCaught;
    public static Action OnFishEscaped;

    // Inventory
    public static Action<ItemData> OnItemAdded;

    // UI messages
    public static Action<string> OnMessage;
}