using UnityEngine;
using UnityEngine.UI;

public class ReelMinigame : MonoBehaviour
{
    public static ReelMinigame Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Slider progressBar;
    [SerializeField] private Slider tensionBar;
    [SerializeField] private Image tensionFillImage;
    [SerializeField] private Image fishIcon; // Fish target icon moving along the bar

    [Header("Juice & Shake Settings")]
    [SerializeField] private Transform uiPanelTransform;
    [SerializeField] private float shakeIntensity = 4f;

    [Header("Tension Mechanics")]
    [SerializeField] private float pullStrength = 0.95f;
    [SerializeField] private float fishPullDownSpeed = 0.65f;
    [SerializeField] private float lineDamping = 0.08f;

    [Header("Safe Zone Visual Colors")]
    [SerializeField] private Color safeColor = new Color(0.2f, 0.8f, 0.3f);
    [SerializeField] private Color dangerColor = new Color(0.9f, 0.2f, 0.2f);
    [SerializeField] private Color rageColor = new Color(0.9f, 0.5f, 0.1f);

    [Header("Fish Movement AI")]
    [SerializeField] private float targetShiftIntervalMin = 0.6f;
    [SerializeField] private float targetShiftIntervalMax = 1.4f;

    // Runtime Dynamic Values
    private float fishIconRadius; // Half-width window derived from FishData.safeZoneWidth
    private float minSafeTension;
    private float maxSafeTension;
    private float currentSafeCenter = 0.5f;
    private float targetSafeCenter = 0.5f;

    private float currentStruggleIntensity;
    private float currentRageChance;
    private float currentRageMultiplier;

    private float progress;
    private float maxProgress;
    private float targetTension;
    private float currentTension;
    private float tensionVelocity;
    
    private float reelPower;
    private float fishResistance;
    private float fishStruggleOffset;
    private float struggleTimer;
    private float comboMultiplier = 1.0f;
    private bool isRaging;

    private bool active;
    private FishData currentFish;
    private Vector3 originalPanelPos;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (uiPanelTransform != null)
            originalPanelPos = uiPanelTransform.localPosition;
    }

    private void Update()
    {
        if (!active) return;

        bool isReeling = InputHandler.Instance != null && InputHandler.Instance.ClickHeld;

        UpdateFishAI();
        CalculateTension(isReeling);
        CalculateProgress(isReeling);
        UpdateUI();
        ApplyJuiceEffects();
        CheckWinLossConditions();
    }

    private void UpdateFishAI()
    {
        struggleTimer -= Time.deltaTime;
        if (struggleTimer <= 0)
        {
            struggleTimer = Random.Range(targetShiftIntervalMin, targetShiftIntervalMax);

            // Shift the fish's center position across the slider bounds (0.15 to 0.85)
            targetSafeCenter = Random.Range(0.15f, 0.85f);

            isRaging = Random.value < currentRageChance;

            if (isRaging)
            {
                fishStruggleOffset = currentStruggleIntensity * currentRageMultiplier;
            }
            else
            {
                fishStruggleOffset = Random.Range(-currentStruggleIntensity, currentStruggleIntensity);
            }
        }

        // Smoothly translate the fish icon position
        currentSafeCenter = Mathf.MoveTowards(currentSafeCenter, targetSafeCenter, Time.deltaTime * 0.8f);

        // Derive hit detection window around the fish icon
        minSafeTension = Mathf.Clamp(currentSafeCenter - fishIconRadius, 0.02f, 0.98f);
        maxSafeTension = Mathf.Clamp(currentSafeCenter + fishIconRadius, 0.02f, 0.98f);

        float decayRate = isRaging ? 1.0f : 1.6f;
        fishStruggleOffset = Mathf.MoveTowards(fishStruggleOffset, 0f, Time.deltaTime * decayRate);
        if (Mathf.Abs(fishStruggleOffset) < 0.05f) isRaging = false;
    }

    private void CalculateTension(bool isReeling)
    {
        if (isReeling)
        {
            targetTension += pullStrength * Time.deltaTime;
        }
        else
        {
            targetTension -= fishPullDownSpeed * Time.deltaTime;
        }

        float desiredTension = Mathf.Clamp01(targetTension + fishStruggleOffset);
        currentTension = Mathf.SmoothDamp(currentTension, desiredTension, ref tensionVelocity, lineDamping);
        
        targetTension = Mathf.Clamp01(targetTension);
    }

    private void CalculateProgress(bool isReeling)
    {
        bool inSafeZone = currentTension >= minSafeTension && currentTension <= maxSafeTension;

        if (inSafeZone)
        {
            if (isReeling)
            {
                comboMultiplier = Mathf.Min(comboMultiplier + Time.deltaTime * 0.5f, 2.0f);
                progress += reelPower * comboMultiplier * Time.deltaTime;
            }
        }
        else
        {
            comboMultiplier = 1.0f;
            float penaltyMultiplier = (currentTension > maxSafeTension) ? 2.8f : 1.8f;
            progress -= fishResistance * penaltyMultiplier * Time.deltaTime;
        }

        progress = Mathf.Clamp(progress, 0f, maxProgress);
    }

    private void ApplyJuiceEffects()
    {
        if (uiPanelTransform == null) return;

        bool inDanger = currentTension > maxSafeTension || currentTension < minSafeTension;

        if (inDanger || isRaging)
        {
            Vector3 randomOffset = (Vector3)Random.insideUnitCircle * (shakeIntensity * (isRaging ? 1.5f : 1.0f));
            uiPanelTransform.localPosition = originalPanelPos + randomOffset;
        }
        else
        {
            uiPanelTransform.localPosition = Vector3.Lerp(uiPanelTransform.localPosition, originalPanelPos, Time.deltaTime * 10f);
        }
    }

    private void CheckWinLossConditions()
    {
        if (progress >= maxProgress)
        {
            EndMinigame(true);
        }
        else if (currentTension >= 0.96f || currentTension <= 0.02f || progress <= 0f)
        {
            EndMinigame(false);
        }
    }

    private void UpdateUI()
    {
        if (progressBar != null && maxProgress > 0)
        {
            progressBar.value = progress / maxProgress;
        }

        if (tensionBar != null)
        {
            tensionBar.value = currentTension;

            if (tensionFillImage != null)
            {
                if (isRaging)
                {
                    tensionFillImage.color = rageColor;
                }
                else
                {
                    bool isDangerous = currentTension < minSafeTension || currentTension > maxSafeTension;
                    tensionFillImage.color = isDangerous ? dangerColor : safeColor;
                }
            }

            PositionFishIcon();
        }
    }

    private void PositionFishIcon()
    {
        if (fishIcon == null || tensionBar == null) return;

        RectTransform barRect = tensionBar.GetComponent<RectTransform>();
        RectTransform iconRect = fishIcon.rectTransform;

        float barWidth = barRect.rect.width;
        
        // Calculate X position relative to tension bar center
        float xPos = (currentSafeCenter - 0.5f) * barWidth;

        iconRect.anchoredPosition = new Vector2(xPos, iconRect.anchoredPosition.y);
    }

    public void StartMinigame(FishData fish)
    {
        if (fish == null)
        {
            Debug.LogError("[ReelMinigame] Cannot start with null FishData.");
            return;
        }

        currentFish = fish;
        if (panel != null) panel.SetActive(true);

        // Assign current fish icon to UI
        if (fishIcon != null && fish.icon != null)
        {
            fishIcon.sprite = fish.icon;
        }

        float artifactReelStrength = Inventory.Instance != null 
            ? Inventory.Instance.GetTotalArtifactBonus(a => a.reelStrengthBonus) 
            : 0f;

        float artifactPullPower = Inventory.Instance != null 
            ? Inventory.Instance.GetTotalArtifactBonus(a => a.pullPowerBonus) 
            : 0f;

        FishingRodData rod = PlayerController.Instance != null 
            ? PlayerController.Instance.GetHeldItem() as FishingRodData 
            : null;

        float rodStrength = rod != null ? rod.reelStrength : 1.0f;
        float rodPower = rod != null ? rod.pullPower : 1.0f;

        maxProgress = fish.baseCatchDifficulty;
        currentStruggleIntensity = fish.struggleIntensity;
        currentRageChance = fish.rageChance;
        currentRageMultiplier = fish.rageMultiplier;

        // Radius window surrounding the fish icon center
        fishIconRadius = Mathf.Clamp(fish.safeZoneWidth, 0.1f, 0.5f) / 2f;
        currentSafeCenter = 0.5f;
        targetSafeCenter = 0.5f;

        reelPower = (rodPower + artifactPullPower) * 22f;
        fishResistance = fish.escapeResistance / Mathf.Max(rodStrength + artifactReelStrength, 0.1f);

        targetTension = 0.5f;
        currentTension = 0.5f;
        tensionVelocity = 0f;
        fishStruggleOffset = 0f;
        struggleTimer = 0.5f;
        comboMultiplier = 1.0f;
        isRaging = false;

        progress = maxProgress * 0.30f;
        active = true;

        UpdateUI();
    }

    private void EndMinigame(bool success)
    {
        active = false;
        if (uiPanelTransform != null) uiPanelTransform.localPosition = originalPanelPos;
        if (panel != null) panel.SetActive(false);

        if (FishingManager.Instance != null)
        {
            FishingManager.Instance.FinishReel(success, currentFish);
        }
    }
}