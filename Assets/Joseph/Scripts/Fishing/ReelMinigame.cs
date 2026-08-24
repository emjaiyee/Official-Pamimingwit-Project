using UnityEngine;
using UnityEngine.UI;

public class ReelMinigame : MonoBehaviour
{
    public static ReelMinigame Instance;

    [Header("UI")]
    public GameObject panel;
    public Slider bar;
    public GameObject pullSign;

    [Header("Juice Settings")]
    public float pulseSpeed = 10f;
    public float pulseAmount = 0.15f;
    public Vector3 signOffset = new Vector3(0, 0, 0);

    float progress;
    float maxProgress;
    float decaySpeed;
    float currentPullPower = 1f;

    bool active;

    FishData currentFish;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (!active) return;

        // Fish fights back
        progress -= decaySpeed * Time.deltaTime;
        progress = Mathf.Clamp(progress, 0, maxProgress);

        bar.value = progress / maxProgress;

        // Fail
        if (progress <= 0)
        {
            EndMinigame(false);
        }

        // Spam click (your universal input)
        if (InputHandler.Instance != null && InputHandler.Instance.ClickDown)
        {
            progress += currentPullPower;

            if (progress >= maxProgress)
            {
                EndMinigame(true);
            }
        }

        // Position and Pulse the "PULL!" sign
        if (pullSign != null && pullSign.activeSelf)
        {
            if (PlayerController.Instance != null)
            {
                pullSign.transform.position = PlayerController.Instance.transform.position + signOffset;
            }

            float s = 1f + (Mathf.Sin(Time.time * pulseSpeed) * pulseAmount);
            pullSign.transform.localScale = new Vector3(s, s, 1f);
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
        currentFish = fish;
        panel.SetActive(true);
        
        if (pullSign != null)
        {
            pullSign.SetActive(true);
        }

        maxProgress = Random.Range(fish.minClicks, fish.maxClicks);
        
        // Difficulty is now driven by the Weight Class enum
        decaySpeed = GetDecayMultiplier(fish.weightClass);

        // Apply Rod progression
        currentPullPower = 1f;
        float artifactReelStrength = 0f;
        float artifactPullPower = 0f;

        if (Inventory.Instance != null)
        {
            artifactReelStrength = Inventory.Instance.GetTotalArtifactBonus(a => a.reelStrengthBonus);
            artifactPullPower = Inventory.Instance.GetTotalArtifactBonus(a => a.pullPowerBonus);
        }

        if (PlayerController.Instance != null && PlayerController.Instance.GetHeldItem() is FishingRodData rod)
        {
            decaySpeed /= Mathf.Max(rod.reelStrength + artifactReelStrength, 0.1f);
            currentPullPower = rod.pullPower + artifactPullPower;
        }
        else
        {
            decaySpeed /= Mathf.Max(1.0f + artifactReelStrength, 0.1f);
            currentPullPower = 1.0f + artifactPullPower;
        }

        // Start with less progress (20% instead of 30%) to increase initial tension
        progress = maxProgress * 0.2f;

        active = true;
    }

    void EndMinigame(bool success)
    {
        active = false;
        panel.SetActive(false);
        
        if (pullSign != null)
        {
            pullSign.SetActive(false);
            pullSign.transform.localScale = Vector3.one; // Reset scale for next time
        }

        if (FishingManager.Instance != null)
        {
            FishingManager.Instance.FinishReel(success, currentFish);
        }
    }
}