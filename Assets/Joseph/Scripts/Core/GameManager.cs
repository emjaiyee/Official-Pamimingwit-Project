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
    public static GameManager Instance;

    public GameState currentState = GameState.Normal;
    public int currentDay = 1;

    [Header("Tax System")]
    public int baseTaxAmount = 50;
    public int currentTaxAmount = 50;
    public int taxInterval = 7;

    [Header("Day/Night Cycle")]
    public TextMeshProUGUI dayText;
    public TextMeshProUGUI timeText;
    public Volume postProcessingVolume;

    [SerializeField] private float timeMultiplier = 60f;
    private float seconds;
    private int minutes;
    private int hours;

    public static Action OnDayAdvanced;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (postProcessingVolume == null)
        {
            postProcessingVolume = GetComponent<Volume>();
        }

        hours = 6;
        minutes = 0;
        seconds = 0;
    }

    void FixedUpdate()
    {
        CalculateTime();
        DisplayTime();
    }

    public void CalculateTime()
    {
        seconds += Time.fixedDeltaTime * timeMultiplier;
        
        if (seconds >= 60)
        {
            seconds = 0;
            minutes += 1;
        }

        if (minutes >= 60)
        {
            minutes = 0;
            hours += 1;
        }

        if (hours == 6 && minutes == 0 && seconds == 0)
        {
            AdvanceDay();
        }

        ControlPPV();
    }

    public void ControlPPV()
    {
        if (postProcessingVolume == null) return;

        // (5:00 AM to 6:00 AM)
        if (hours == 5)
        {
            postProcessingVolume.weight = 1f - ((float)minutes / 60f);
        }
        // Full Day Time (6:00 AM to 5:29 PM)
        else if (hours >= 6 && hours < 17)
        {
            postProcessingVolume.weight = 0f;
        }
        // (Sunset starts at 5:30 PM)
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
        // (6:00 PM to 6:30 PM)
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
        // (6:30 PM to 4:59 AM)
        else
        {
            postProcessingVolume.weight = 1f;
        }
    }


    public void DisplayTime()
    {
        dayText.text = "Day: " + currentDay;

        int displayHour = hours % 12;
        if (displayHour == 0) displayHour = 12;

        string amPm = hours >= 12 ? "PM" : "AM";

        timeText.text = string.Format("{0:00}:{1:00} {2}", displayHour, minutes, amPm);
    }



    public void AdvanceDay()
    {
        currentDay++;
        // Call OnDayAdvanced
        OnDayAdvanced?.Invoke();
        Debug.Log("Starting Day " + currentDay);
    }

    public string ProcessTax()
    {
        // Check if it's tax day
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