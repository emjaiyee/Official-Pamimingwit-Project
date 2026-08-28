using UnityEngine;
using System;

public class StaminaManager : MonoBehaviour
{
    public static StaminaManager Instance { get; private set; }

    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaRegenRate = 5f;
    [SerializeField] private float fishingStaminaCost = 10f;
    [SerializeField] private float dynamiteStaminaCost = 25f;

    [Header("Exhaustion Debuff Settings")]
    [Tooltip("Multiplier applied to max stamina the day after passing out (e.g. 0.5 = 50% max stamina).")]
    [SerializeField] private float fatigueMaxStaminaMultiplier = 0.5f;

    private float currentStamina;
    private bool isFatiguedNextDay = false;
    private bool isPassingOut = false;

    public event Action<float, float> OnStaminaChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        GameManager.OnDayAdvanced += HandleDayAdvanced;
    }

    private void OnDisable()
    {
        GameManager.OnDayAdvanced -= HandleDayAdvanced;
    }

    private void Start()
    {
        currentStamina = maxStamina;
        OnStaminaChanged?.Invoke(currentStamina, GetEffectiveMaxStamina());
    }

    private void Update()
    {
        // Regenerate stamina if below effective max and not working late
        float effectiveMax = GetEffectiveMaxStamina();

        if (currentStamina < effectiveMax && (GameManager.Instance == null || !GameManager.Instance.IsLateNight))
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, effectiveMax);
            OnStaminaChanged?.Invoke(currentStamina, effectiveMax);
        }
    }

    private void HandleDayAdvanced()
    {
        isPassingOut = false;

        if (isFatiguedNextDay)
        {
            // Apply fatigue penalty for today
            currentStamina = GetEffectiveMaxStamina();
            UIManager.Instance?.ShowMessage("Feeling sluggish from passing out last night...");
            isFatiguedNextDay = false; // Reset flag for subsequent days
        }
        else
        {
            // Full recovery when sleeping normally
            RefillStamina();
        }

        OnStaminaChanged?.Invoke(currentStamina, GetEffectiveMaxStamina());
    }

    public void ApplyLateNightDrain(float amount)
    {
        if (isPassingOut) return;
        ConsumeStamina(amount);
    }

    public void SetFatiguedForNextDay(bool state)
    {
        isFatiguedNextDay = state;
    }

    public float GetEffectiveMaxStamina()
    {
        return isFatiguedNextDay ? (maxStamina * fatigueMaxStaminaMultiplier) : maxStamina;
    }

    public void RefillStamina()
    {
        currentStamina = GetEffectiveMaxStamina();
        OnStaminaChanged?.Invoke(currentStamina, GetEffectiveMaxStamina());
    }

    public void SetStamina(float amount)
    {
        currentStamina = Mathf.Clamp(amount, 0, GetEffectiveMaxStamina());
        OnStaminaChanged?.Invoke(currentStamina, GetEffectiveMaxStamina());
    }

    public float GetStamina() => currentStamina;

    public bool CanAffordFishing() => currentStamina >= fishingStaminaCost;
    public bool CanAffordDynamite() => currentStamina >= dynamiteStaminaCost;

    public void ConsumeFishingStamina() => ConsumeStamina(fishingStaminaCost);
    public void ConsumeDynamiteStamina() => ConsumeStamina(dynamiteStaminaCost);

    private void ConsumeStamina(float amount)
    {
        if (isPassingOut) return;

        currentStamina -= amount;
        
        if (currentStamina <= 0f)
        {
            currentStamina = 0f;
            isPassingOut = true;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.PassOutFromFatigue();
            }
        }

        OnStaminaChanged?.Invoke(currentStamina, GetEffectiveMaxStamina());
    }
}