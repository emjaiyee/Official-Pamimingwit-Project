using System;


public enum SoundType
{
    PanelOpen,
    PanelClose,
    Rooster,
    TaxPaid,
    TaxFailed,
    MorningAmbience,
    StopTransitionAudio
}

public static class GameEvents
{

    
    // Fishing
    public static Action<FishData> OnFishHooked;
    public static Action<ItemData> OnItemCaught;
    public static Action OnFishEscaped;

     public static Action<SoundType> OnPlaySound;

    public static void TriggerSound(SoundType soundType)
    {
        OnPlaySound?.Invoke(soundType);

        // TO CALL SOUND USE THIS FUNCTION!
    // GameEvents.TriggerSound(SoundType.TaxFailed);
    }

    // Inventory
    public static Action<ItemData> OnItemAdded;

    // UI messages
    public static Action<string> OnMessage;
}