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

    [Header("Tension Mechanics")]
    [Tooltip("How fast holding LMB pulls tension up toward red.")]
    [SerializeField] private float pullStrength = 0.85f;
    [Tooltip("How fast the fish pulls tension down toward zero when NOT holding LMB.")]
    [SerializeField] private float fishPullDownSpeed = 0.55f;
    [Tooltip("Line response damping (lower = snappy/heavy, higher = smooth).")]
    [SerializeField] private float lineDamping = 0.08f;

    [Header("Safe Zone Visual Colors")]
    [SerializeField] private Color safeColor = new Color(0.2f, 0.8f, 0.3f);
    [SerializeField] private Color dangerColor = new Color(0.9f, 0.2f, 0.2f);
    [SerializeField] private Color rageColor = new Color(0.9f, 0.5f, 0.1f);

    [Header("Struggle Timing")]
    [SerializeField] private float struggleIntervalMin = 0.8f;
    [SerializeField] private float struggleIntervalMax = 1.8f;

    // Runtime Dynamic Values (Derived from FishData)
    private float minSafeTension;
    private float maxSafeTension;
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
    private bool isRaging;

    private bool active;
    private FishData currentFish;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (!active) return;

        bool isReeling = InputHandler.Instance != null && InputHandler.Instance.ClickHeld;

        UpdateFishBehavior();
        CalculateTension(isReeling);
        CalculateProgress(isReeling);
        UpdateUI();
        CheckWinLossConditions();
    }

    private void UpdateFishBehavior()
    {
        struggleTimer -= Time.deltaTime;
        if (struggleTimer <= 0)
        {
            struggleTimer = Random.Range(struggleIntervalMin, struggleIntervalMax);

            isRaging = Random.value < currentRageChance;

            if (isRaging)
            {
                // Sudden violent surge upward toward snapping
                fishStruggleOffset = currentStruggleIntensity * currentRageMultiplier;
            }
            else
            {
                // Random tug displacement
                fishStruggleOffset = Random.Range(-currentStruggleIntensity, currentStruggleIntensity);
            }
        }

        float decayRate = isRaging ? 0.8f : 1.4f;
        fishStruggleOffset = Mathf.MoveTowards(fishStruggleOffset, 0f, Time.deltaTime * decayRate);
        if (Mathf.Abs(fishStruggleOffset) < 0.05f) isRaging = false;
    }

    private void CalculateTension(bool isReeling)
    {
        if (isReeling)
        {
            // Player pulls line up
            targetTension += pullStrength * Time.deltaTime;
        }
        else
        {
            // Fish overwhelms player and pulls tension down toward ZERO (escape)
            targetTension -= fishPullDownSpeed * Time.deltaTime;
        }

        float desiredTension = Mathf.Clamp01(targetTension + fishStruggleOffset);
        currentTension = Mathf.SmoothDamp(currentTension, desiredTension, ref tensionVelocity, lineDamping);
        
        // Clamp state variable target to stay bounded
        targetTension = Mathf.Clamp01(targetTension);
    }

    private void CalculateProgress(bool isReeling)
    {
        bool inSafeZone = currentTension >= minSafeTension && currentTension <= maxSafeTension;

        if (inSafeZone)
        {
            if (isReeling)
            {
                // Active reeling inside safe zone generates strong catch progress
                progress += reelPower * Time.deltaTime;
            }
            // When inside the safe zone without holding LMB, progress remains stable (0 decay)
        }
        else
        {
            // Progress decays ONLY when line tension crosses into danger/slack bounds
            float penaltyMultiplier = (currentTension > maxSafeTension) ? 2.5f : 1.5f;
            progress -= fishResistance * penaltyMultiplier * Time.deltaTime;
        }

        progress = Mathf.Clamp(progress, 0f, maxProgress);
    }

    private void CheckWinLossConditions()
    {
        if (progress >= maxProgress)
        {
            EndMinigame(true);
        }
        // Fail if line snaps (>= 0.96), line goes completely slack (<= 0.02), or progress hits 0
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
        }
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

        float halfZone = Mathf.Clamp(fish.safeZoneWidth, 0.1f, 0.8f) / 2f;
        minSafeTension = 0.5f - halfZone;
        maxSafeTension = 0.5f + halfZone;

        // Boosted reel power multiplier from 10f to 25f for faster bar growth
        reelPower = (rodPower + artifactPullPower) * 25f;
        fishResistance = fish.escapeResistance / Mathf.Max(rodStrength + artifactReelStrength, 0.1f);

        // Start tension inside safe zone
        targetTension = 0.5f;
        currentTension = 0.5f;
        tensionVelocity = 0f;
        fishStruggleOffset = 0f;
        struggleTimer = 0.5f;
        isRaging = false;

        progress = maxProgress * 0.30f;
        active = true;

        UpdateUI();
    }

    private void EndMinigame(bool success)
    {
        active = false;
        if (panel != null) panel.SetActive(false);

        if (FishingManager.Instance != null)
        {
            FishingManager.Instance.FinishReel(success, currentFish);
        }
    }
}