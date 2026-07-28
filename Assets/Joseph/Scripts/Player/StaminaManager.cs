using UnityEngine;
using System;

public class StaminaManager : MonoBehaviour
{
    public static StaminaManager Instance;

    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaRegenRate = 5f; // Stamina per second
    [SerializeField] private float fishingStaminaCost = 10f;
    [SerializeField] private float dynamiteStaminaCost = 25f;

    private float currentStamina;

    // Event to notify UI or other systems about stamina changes
    public event Action<float, float> OnStaminaChanged; // currentStamina, maxStamina

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        currentStamina = maxStamina;
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    void Update()
    {
        if (currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }
    }

    public void RefillStamina()
    {
        currentStamina = maxStamina;
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    public void SetStamina(float amount)
    {
        currentStamina = Mathf.Clamp(amount, 0, maxStamina);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    public float GetStamina() => currentStamina;

    public bool CanAffordFishing() => currentStamina >= fishingStaminaCost;
    public bool CanAffordDynamite() => currentStamina >= dynamiteStaminaCost;

    public void ConsumeFishingStamina()
    {
        ConsumeStamina(fishingStaminaCost);
    }

    public void ConsumeDynamiteStamina()
    {
        ConsumeStamina(dynamiteStaminaCost);
    }

    private void ConsumeStamina(float amount)
    {
        currentStamina -= amount;
        currentStamina = Mathf.Max(currentStamina, 0);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }
}