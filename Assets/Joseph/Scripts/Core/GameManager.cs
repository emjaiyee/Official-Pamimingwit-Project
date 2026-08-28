using UnityEngine;
using System;
using TMPro;
using UnityEngine.Rendering;

public enum GameState
{
    Normal,
    Fishing,
    UI
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState currentState { get; private set; } = GameState.Normal;
    public int currentDay { get; set; } = 1;

    [Header("Player Spawn Setup")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform houseSpawnPoint;

    [Header("Tax System")]
    public int baseTaxAmount = 50;
    public int currentTaxAmount = 50;
    public int taxInterval = 7;

    [Header("Day/Night Cycle")]
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private Volume postProcessingVolume;

    [SerializeField] private float timeMultiplier = 60f;
    private float seconds;
    private int minutes;
    private int hours = 6;

    [Header("Late Night Debuff Settings")]
    [Tooltip("Hour at which late night stamina drain activates (e.g., 22 = 10:00 PM).")]
    [SerializeField] private int lateNightStartHour = 22;
    [Tooltip("Stamina drained per second during late hours.")]
    [SerializeField] private float lateNightStaminaDrainRate = 2.5f;

    public bool IsTransitioningDay { get; private set; }
    public bool IsLateNight => (hours >= lateNightStartHour || hours < 6);

    public static event Action OnDayAdvanced;
    public static event Action OnPlayerPassedOut;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (postProcessingVolume == null)
        {
            postProcessingVolume = GetComponent<Volume>();
        }

        // Auto-assign player reference if missing in Inspector
        if (playerTransform == null && PlayerController.Instance != null)
        {
            playerTransform = PlayerController.Instance.transform;
        }

        ResetTime();
    }

    private void FixedUpdate()
    {
        if (currentState == GameState.UI || IsTransitioningDay) return;

        CalculateTime();
        DisplayTime();
        HandleLateNightDebuff();
    }

    public void CalculateTime()
    {
        seconds += Time.fixedDeltaTime * timeMultiplier;
        
        if (seconds >= 60f)
        {
            seconds = 0f;
            minutes += 1;

            if (minutes >= 60)
            {
                minutes = 0;
                hours += 1;

                if (hours >= 24)
                {
                    hours = 0;
                }

                if (hours == 6 && !IsTransitioningDay)
                {
                    TriggerMorningTransition(false);
                    return;
                }
            }
        }

        ControlPPV();
    }

    private void HandleLateNightDebuff()
    {
        if (IsLateNight && StaminaManager.Instance != null)
        {
            StaminaManager.Instance.ApplyLateNightDrain(lateNightStaminaDrainRate * Time.fixedDeltaTime);
        }
    }

    public void PassOutFromFatigue()
    {
        if (IsTransitioningDay) return;

        Debug.LogWarning("[GameManager] Player passed out from exhaustion!");

        // 1. Force state & lock movement immediately
        SetState(GameState.UI);
        PlayerController.Instance?.LockMovement();

        // 2. Halt all active minigames and close open UI panels
        if (FishingManager.Instance != null) FishingManager.Instance.CancelFishing();
        if (UIManager.Instance != null) UIManager.Instance.CloseAllStandardPanels();

        // 3. Flag stamina fatigue for the next day
        if (StaminaManager.Instance != null)
        {
            StaminaManager.Instance.SetFatiguedForNextDay(true);
        }

        OnPlayerPassedOut?.Invoke();
        TriggerMorningTransition(true);
    }

    private void TriggerMorningTransition(bool passedOut)
    {
        IsTransitioningDay = true;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.StartDayTransition(() => {
                IsTransitioningDay = false;
            }, passedOut);
        }
        else
        {
            AdvanceDay();
            IsTransitioningDay = false;
        }
    }

    public void MovePlayerToHouse()
    {
        if (houseSpawnPoint == null)
        {
            Debug.LogWarning("[GameManager] House Spawn Point is unassigned in the Inspector!");
            return;
        }

        Transform targetPlayer = playerTransform != null 
            ? playerTransform 
            : (PlayerController.Instance != null ? PlayerController.Instance.transform : GameObject.FindWithTag("Player")?.transform);

        if (targetPlayer != null)
        {
            targetPlayer.position = houseSpawnPoint.position;
        }
    }

    public void ControlPPV()
    {
        if (postProcessingVolume == null) return;

        if (hours == 5)
        {
            postProcessingVolume.weight = 1f - ((float)minutes / 60f);
        }
        else if (hours >= 6 && hours < 17)
        {
            postProcessingVolume.weight = 0f;
        }
        else if (hours == 17)
        {
            if (minutes < 30)
            {
                postProcessingVolume.weight = 0f;
            }
            else
            {
                postProcessingVolume.weight = (float)(minutes - 30) / 30f;
            }
        }
        else if (hours == 18)
        {
            if (minutes < 30)
            {
                postProcessingVolume.weight = 0.5f + ((float)minutes / 60f);
            }
            else
            {
                postProcessingVolume.weight = 1f;
            }
        }
        else
        {
            postProcessingVolume.weight = 1f;
        }
    }

    public void DisplayTime()
    {
        if (dayText != null) dayText.text = $"Day: {currentDay}";

        int displayHour = hours % 12;
        if (displayHour == 0) displayHour = 12;

        string amPm = hours >= 12 ? "PM" : "AM";

        if (timeText != null)
        {
            timeText.text = string.Format("{0:00}:{1:00} {2}", displayHour, minutes, amPm);
        }
    }

    public void AdvanceDay()
    {
        currentDay++;
        MovePlayerToHouse();
        OnDayAdvanced?.Invoke();
        ResetTime();
    }

    public void ResetTime()
    {
        hours = 6;
        minutes = 0;
        seconds = 0f;
        ControlPPV();
    }

    public string ProcessTax()
    {
        if (currentDay % taxInterval == 0)
        {
            if (PlayerWallet.Instance != null && PlayerWallet.Instance.SpendCoins(currentTaxAmount))
            {
                int deducted = currentTaxAmount;
                currentTaxAmount = baseTaxAmount;
                return $"It's tax day, {deducted} selyo is deducted from your wallet";
            }
            else
            {
                currentTaxAmount *= 2;
                return $"Insufficient funds! Tax doubles to {currentTaxAmount} selyo for next time.";
            }
        }
        else
        {
            int daysLeft = taxInterval - (currentDay % taxInterval);
            return $"{daysLeft} days till tax, ready {currentTaxAmount} selyo";
        }
    }

    public void SetState(GameState newState)
    {
        currentState = newState;
    }
}