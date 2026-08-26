using UnityEngine;
using UnityEngine.UI;

public class ReelMinigame : MonoBehaviour
{
    public static ReelMinigame Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Slider bar;
    [SerializeField] private GameObject pullSign;

    [Header("Juice Settings")]
    [SerializeField] private float pulseSpeed = 10f;
    [SerializeField] private float pulseAmount = 0.15f;
    [SerializeField] private Vector3 signOffset = Vector3.zero;

    private float progress;
    private float maxProgress;
    private float decaySpeed;
    private float currentPullPower = 1f;
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

        HandleInput();
        HandleDecay();
        UpdateUI();
    }

    private void LateUpdate()
    {
        if (!active) return;
        UpdatePullSignPosition();
    }

    private void HandleInput()
    {
        if (InputHandler.Instance != null && InputHandler.Instance.ClickDown)
        {
            progress += currentPullPower;

            if (progress >= maxProgress)
            {
                EndMinigame(true);
            }
        }
    }

    private void HandleDecay()
    {
        if (!active) return;

        progress -= decaySpeed * Time.deltaTime;
        progress = Mathf.Clamp(progress, 0, maxProgress);

        if (progress <= 0)
        {
            EndMinigame(false);
        }
    }

    private void UpdateUI()
    {
        if (bar != null && maxProgress > 0)
        {
            bar.value = progress / maxProgress;
        }

        if (pullSign != null && pullSign.activeSelf)
        {
            float scale = 1f + (Mathf.Sin(Time.time * pulseSpeed) * pulseAmount);
            pullSign.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    private void UpdatePullSignPosition()
    {
        if (pullSign != null && pullSign.activeSelf && PlayerController.Instance != null)
        {
            pullSign.transform.position = PlayerController.Instance.transform.position + signOffset;
        }
    }

    private float GetDecayMultiplier(FishWeight weight)
    {
        return weight switch
        {
            FishWeight.Small => 2.5f,
            FishWeight.Medium => 5.0f,
            FishWeight.Heavy => 9.0f,
            _ => 1.0f
        };
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
        if (pullSign != null) pullSign.SetActive(true);

        maxProgress = Random.Range(fish.minClicks, fish.maxClicks);
        float baseDecay = GetDecayMultiplier(fish.weightClass);

        // Fetch bonuses safely
        float artifactReelStrength = Inventory.Instance != null 
            ? Inventory.Instance.GetTotalArtifactBonus(a => a.reelStrengthBonus) 
            : 0f;

        float artifactPullPower = Inventory.Instance != null 
            ? Inventory.Instance.GetTotalArtifactBonus(a => a.pullPowerBonus) 
            : 0f;

        FishingRodData rod = (PlayerController.Instance != null) 
            ? PlayerController.Instance.GetHeldItem() as FishingRodData 
            : null;

        float rodReelStrength = rod != null ? rod.reelStrength : 1.0f;
        float rodPullPower = rod != null ? rod.pullPower : 1.0f;

        // Stat Calculations with floor guards
        float totalReelStrength = Mathf.Max(rodReelStrength + artifactReelStrength, 0.1f);
        decaySpeed = baseDecay / totalReelStrength;
        currentPullPower = Mathf.Max(rodPullPower + artifactPullPower, 0.1f);

        progress = maxProgress * 0.2f;
        active = true;

        UpdateUI();
    }

    private void EndMinigame(bool success)
    {
        active = false;
        if (panel != null) panel.SetActive(false);

        if (pullSign != null)
        {
            pullSign.SetActive(false);
            pullSign.transform.localScale = Vector3.one;
        }

        if (FishingManager.Instance != null)
        {
            FishingManager.Instance.FinishReel(success, currentFish);
        }
    }
}