using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

public class FishingManager : MonoBehaviour
{
    public static FishingManager Instance;

    public enum FishingState
    {
        Idle,
        Aiming,
        Waiting,
        Biting,
        Result
    }

    public FishingState state = FishingState.Idle;

    [Header("Fishing Setup")]
    public bool debugMode = false;
    public LayerMask waterLayer;
    public GameObject bobberPrefab;
    public Transform player;

    [Header("Visuals")]
    public LineRenderer fishingLine;
    public Transform rodTip;
    [SerializeField] private int lineResolution = 15;
    [SerializeField] private float lineSagAmount = 0.3f;
    public GameObject dynamitePrefab; 
    [SerializeField] private GameObject explosionParticlePrefab;
    [SerializeField] private float shakeDuration = 0.2f;
    [SerializeField] private float shakeMagnitude = 0.15f;

    [Header("Fish")]
    public ItemData[] fishPool;

    [Header("Casting")]
    public float maxHoldTime = 1.5f;

    [Header("Bite Window")]
    public float minBiteWindow = 1.0f;
    public float maxBiteWindow = 3.0f;
    public float loseBaitChance = 0.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip castSFX;
    [SerializeField] private AudioClip pullSFX;

    float holdTime;
    bool inputLocked;

    private Vector3 pendingTargetPos;
    private ItemData pendingItem;
    private bool pendingIsDynamite;
    private bool isCastPending; 

    FishingBobber currentBobber;

    private ArtifactData pendingArtifact;
    FishData hookedFish;

    private FishData runtimeArtifactStruggle;
    private AudioSource audioSource;

    // Decoupled Events
    public static event Action<FishData> OnFishHooked;
    public static event Action OnFishEscaped;
    public static event Action<float, float> OnCameraShakeRequested;

    // Persistent References
    private PlayerController cachedPlayer;
    private Inventory cachedInventory;

    void Awake()
    {
        Instance = this;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (fishingLine != null)
        {
            fishingLine.useWorldSpace = true;
        }
    }

    void Start()
    {
        cachedPlayer = PlayerController.Instance;
        cachedInventory = Inventory.Instance;
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 screenPos = Mouse.current.position.ReadValue();
        screenPos.z = -Camera.main.transform.position.z;
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(screenPos);
        mousePos.z = 0;
        return mousePos;
    }

    void Update()
    {
        HandleInput();
    }

    void LateUpdate()
    {
        UpdateFishingLine();
    }

    void HandleInput()
    {
        if (inputLocked) return;

        if (GameManager.Instance != null && GameManager.Instance.currentState == GameState.UI)
            return;

        bool hasRod = EquipmentManager.Instance != null && EquipmentManager.Instance.hasFishingRodEquipped;
        bool hasDynamite = EquipmentManager.Instance != null && EquipmentManager.Instance.hasDynamiteEquipped;
        ItemData heldItem = cachedPlayer?.GetHeldItem();
        bool holdingFish = heldItem is FishData;
        bool holdingJunk = heldItem != null && heldItem.itemType == ItemType.Junk;

        if (!debugMode && !hasRod && !hasDynamite && !holdingFish && !holdingJunk)
            return;

        if (UIManager.Instance != null && UIManager.Instance.IsPointerOverUI())
            return;

        if (InputHandler.Instance == null) return;

        var input = InputHandler.Instance;

        if (input.ClickDown)
        {
            if (state == FishingState.Idle)
            {
                if (holdingFish || holdingJunk)
                {
                    Vector3 mouse = GetMouseWorldPosition();

                    if (Physics2D.OverlapCircle(mouse, 0.2f, waterLayer))
                    {
                        string actionLabel = holdingFish ? "Release Fish" : "Discard Junk";
                        string prompt = holdingFish ? "Do you want to release this fish back into the water?" : "Do you want to discard this junk?";

                        UIManager.Instance?.ShowChoice(
                            prompt,
                            actionLabel,
                            () => ConfirmReleaseFish(heldItem),
                            "Keep Item",
                            null
                        );
                        return;
                    }
                }

                if (hasRod && FindBaitInInventory() == null)
                {
                    UIManager.Instance?.ShowMessage("You need bait to use the rod!");
                    return;
                }

                if (StaminaManager.Instance != null)
                {
                    if (hasDynamite && !StaminaManager.Instance.CanAffordDynamite())
                    {
                        UIManager.Instance?.ShowMessage("Not enough stamina to use dynamite!");
                        return;
                    }
                    else if (hasRod && !StaminaManager.Instance.CanAffordFishing())
                    {
                        UIManager.Instance?.ShowMessage("Not enough stamina to fish!");
                        return;
                    }
                }
                StartAiming(hasDynamite, heldItem);
            }
            else if (state == FishingState.Biting)
            {
                StartReel();
            }
        }

        if (input.ClickHeld && state == FishingState.Aiming)
        {
            ChargeCast();
        }

        if (input.ClickUp && state == FishingState.Aiming)
        {
            CastRod();
        }
    }

    public Vector3 GetRodTipPosition()
    {
        if (rodTip == null) return player.position;

        Vector3 worldPos = rodTip.position;

        if (cachedPlayer != null && cachedPlayer.GetComponent<SpriteRenderer>().flipX)
        {
            Vector3 localPos = player.InverseTransformPoint(worldPos);
            localPos.x = -localPos.x;
            return player.TransformPoint(localPos);
        }

        return worldPos;
    }

    void UpdateFishingLine()
    {
        if (fishingLine == null) return;

        if (currentBobber != null)
        {
            Vector3 startPos = GetRodTipPosition();
            Vector3 endPos = currentBobber.transform.position;

            startPos.z = 0;
            endPos.z = 0;

            fishingLine.enabled = true;
            fishingLine.positionCount = lineResolution;

            bool isLineTense = currentBobber.IsFlying || state == FishingState.Biting || state == FishingState.Result;
            float currentSag = isLineTense ? 0.02f : lineSagAmount;

            for (int i = 0; i < lineResolution; i++)
            {
                float t = i / (float)(lineResolution - 1);
                Vector3 pos = Vector3.Lerp(startPos, endPos, t);

                float sag = Mathf.Sin(t * Mathf.PI) * currentSag;
                pos.y -= sag;

                fishingLine.SetPosition(i, pos);
            }
        }
        else
        {
            fishingLine.enabled = false;
        }
    }

    void StartAiming(bool isDynamite, ItemData heldItem)
    {
        Vector3 mouse = GetMouseWorldPosition();
        Vector3 dir = (mouse - player.position).normalized;
        Vector3 initialTarget = player.position + dir * 1f; 

        if (!Physics2D.OverlapCircle(initialTarget, 0.2f, waterLayer))
            return;

        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameState.Fishing);

        cachedPlayer?.StartAiming(isDynamite);

        state = FishingState.Aiming;
        holdTime = 0;
        isCastPending = false;
        pendingIsDynamite = isDynamite;
        pendingItem = heldItem;

        ChargeCast();
    }

    void ChargeCast()
    {
        holdTime += Time.deltaTime;
        holdTime = Mathf.Clamp(holdTime, 0, maxHoldTime);

        Vector3 mouse = GetMouseWorldPosition();
        Vector3 dir = (mouse - player.position).normalized;

        cachedPlayer?.SetFishingDirection(dir);
    }

    void CastRod()
    {
        float power = holdTime / maxHoldTime;
        Vector3 mouse = GetMouseWorldPosition();
        Vector3 dir = (mouse - player.position).normalized;
        float distance = Mathf.Lerp(1f, 4f, power);
        pendingTargetPos = player.position + dir * distance;

        if (!Physics2D.OverlapCircle(pendingTargetPos, 0.2f, waterLayer))
        {
            Cleanup(); 
            return;
        }

        if (StaminaManager.Instance != null)
        {
            if (pendingIsDynamite)
            {
                StaminaManager.Instance.ConsumeDynamiteStamina();
            }
            else
            {
                StaminaManager.Instance.ConsumeFishingStamina();
            }
        }

        if (pendingIsDynamite)
        {
            state = FishingState.Result;
        }
        else
        {
            state = FishingState.Waiting;
        }

        if (castSFX != null && audioSource != null) audioSource.PlayOneShot(castSFX);

        if (cachedPlayer != null)
        {
            cachedPlayer.SetFishingDirection(dir);
            if (pendingIsDynamite)
                cachedPlayer.PlayThrowAnimation();
            else
                cachedPlayer.PlayCastAnimation();
        }

        isCastPending = true;
    }

    public void DeployBobber()
    {
        if (!isCastPending) return;
        isCastPending = false;

        if (pendingIsDynamite)
        {
            LaunchDynamiteVisual(pendingTargetPos, pendingItem);
        }
        else
        {
            Vector3 launchOrigin = GetRodTipPosition();
            launchOrigin.z = 0;
            GameObject b = Instantiate(bobberPrefab, launchOrigin, Quaternion.identity);
            currentBobber = b.GetComponent<FishingBobber>();
            currentBobber.Launch(pendingTargetPos, () => StartCoroutine(WaitForBite()));
        }
    }

    private void LaunchDynamiteVisual(Vector3 targetPos, ItemData data)
    {
        if (dynamitePrefab == null)
        {
            Debug.LogError("FishingManager: Dynamite Prefab is missing in the Inspector!");
            Cleanup();
            return;
        }

        Vector3 start = GetRodTipPosition(); 
        start.z = 0; 
        GameObject d = Instantiate(dynamitePrefab, start, Quaternion.identity);

        DynamiteProjectile proj = d.GetComponent<DynamiteProjectile>();
        if (proj != null)
        {
            proj.Launch(targetPos, () => ExplodeDynamite(targetPos, data));
        }
        else
        {
            Destroy(d);
            Cleanup();
        }
    }

    private void ExplodeDynamite(Vector3 targetPos, ItemData data)
    {
        OnCameraShakeRequested?.Invoke(shakeDuration, shakeMagnitude);

        if (explosionParticlePrefab != null)
        {
            Vector3 particlePos = new Vector3(targetPos.x, targetPos.y, 0);
            GameObject explosion = Instantiate(explosionParticlePrefab, particlePos, Quaternion.identity);
            Destroy(explosion, 3f); 
        }

        DynamiteData dynData = data as DynamiteData;
        int penalty = dynData != null ? dynData.sustainabilityPenalty : -10;
        int haul = dynData != null ? dynData.haulSize : 2;

        SustainabilityManager.Instance?.Add(penalty);

        float oceanMultiplier = ReactiveOceanManager.Instance != null ? ReactiveOceanManager.Instance.GetCurrentTier().haulMultiplier : 1f;
        
        int bonusHaul = UnityEngine.Random.Range(1, 4); 
        int finalHaul = Mathf.Max(1, Mathf.RoundToInt(haul * oceanMultiplier) + bonusHaul);

        for (int i = 0; i < finalHaul; i++)
        {
            ItemData caught = (ReactiveOceanManager.Instance != null) ? ReactiveOceanManager.Instance.GetRandomCatch() : null;
            if (caught == null && fishPool.Length > 0) caught = fishPool[UnityEngine.Random.Range(0, fishPool.Length)];

            if (caught != null && cachedInventory != null && cachedInventory.AddItem(caught))
            {
                UIManager.Instance?.ShowMessage($"Dynamite caught: {caught.itemName}");
                GameEvents.OnItemCaught?.Invoke(caught);
            }
        }

        ConsumeHeldItem();
        Cleanup();
    }

    IEnumerator WaitForBite()
    {
        float catchModifier = 1f;
        float artifactBonus = cachedInventory != null ? cachedInventory.GetTotalArtifactBonus(a => a.catchRateBonus) : 0f;

        if (cachedPlayer?.GetHeldItem() is FishingRodData rod)
            catchModifier *= rod.catchRateMultiplier;
            
        catchModifier += artifactBonus;

        InventoryItem baitStack = FindBaitInInventory();
        if (baitStack != null)
        {
            float bonus = (baitStack.item is BaitData bait) ? bait.catchRateBonus : 1.2f;
            catchModifier *= bonus;
        }

        float wait = UnityEngine.Random.Range(2f, 5f) / catchModifier;
        yield return new WaitForSeconds(wait);

        if (state != FishingState.Waiting) yield break;

        state = FishingState.Biting;

        BaitData currentBait = (baitStack != null) ? baitStack.item as BaitData : null;
        ItemData caughtItem = SelectCatch(currentBait);

        if (caughtItem != null && caughtItem.itemType == ItemType.Junk)
        {
            cachedInventory?.AddItem(caughtItem);
            UIManager.Instance?.ShowMessage($"Caught junk: {caughtItem.itemName}");
            GameEvents.OnItemCaught?.Invoke(caughtItem);
            ConsumeBait(); 
            Cleanup();
            yield break;
        }

        if (caughtItem is ArtifactData artifact)
        {
            pendingArtifact = artifact;
            
            if (runtimeArtifactStruggle != null) Destroy(runtimeArtifactStruggle);

            runtimeArtifactStruggle = ScriptableObject.CreateInstance<FishData>();
            runtimeArtifactStruggle.itemName = "Mysterious Heavy Object";
            runtimeArtifactStruggle.weightClass = FishWeight.Heavy;
            runtimeArtifactStruggle.minClicks = 45;
            runtimeArtifactStruggle.maxClicks = 75;
            hookedFish = runtimeArtifactStruggle;
        }
        else
        {
            hookedFish = caughtItem as FishData;
            pendingArtifact = null;
        }

        if (hookedFish == null) { HandleFishEscape(); yield break; }

        if (currentBobber != null)
            currentBobber.PlayBite();

        float window = UnityEngine.Random.Range(minBiteWindow, maxBiteWindow);

        if (baitStack != null && baitStack.item is BaitData baitData)
            window += baitData.biteWindowExtension;

        yield return new WaitForSeconds(window);

        if (state == FishingState.Biting)
        {
            HandleFishEscape();
        }
    }

    private ItemData SelectCatch(BaitData bait)
    {
        if (ReactiveOceanManager.Instance != null)
        {
            ItemData tierCatch = ReactiveOceanManager.Instance.GetRandomCatch();
            
            if (tierCatch != null && (tierCatch.itemType == ItemType.Artifact || tierCatch.itemType == ItemType.Junk))
            {
                return tierCatch;
            }
        }

        List<ItemData> eligibleItems = new List<ItemData>();

        foreach (var item in fishPool)
        {
            if (item is FishData fish)
            {
                if (fish.requiredBait == null || (bait != null && fish.requiredBait == bait))
                {
                    eligibleItems.Add(fish);
                }
            }
            else if (item != null)
            {
                eligibleItems.Add(item); 
            }
        }

        if (eligibleItems.Count == 0) return null;
        return eligibleItems[UnityEngine.Random.Range(0, eligibleItems.Count)];
    }

    void StartReel()
    {
        cachedPlayer?.SetPulling(true);
        cachedPlayer?.PlayPullAnimation();

        state = FishingState.Result;

        if (pullSFX != null && audioSource != null) {
            audioSource.clip = pullSFX;
            audioSource.loop = true; 
            audioSource.Play();
        }

        ConsumeBait(); 

        OnFishHooked?.Invoke(hookedFish);

        if (ReelMinigame.Instance != null)
        {
            ReelMinigame.Instance.StartMinigame(hookedFish);
        }
    }

    public void FinishReel(bool success, FishData fish)
    {
        cachedPlayer?.SetPulling(false);

        if (success)
        {
            if (pendingArtifact != null)
            {
                if (cachedInventory != null)
                    cachedInventory.AddItem(pendingArtifact);

                UIManager.Instance?.ShowMessage("Recovered Artifact: " + pendingArtifact.itemName);
                GameEvents.OnItemCaught?.Invoke(pendingArtifact);
                pendingArtifact = null;
                Cleanup();
                return;
            }

            if (cachedInventory != null)
            {
                float luck = 0f;
                float artifactLuck = cachedInventory.GetTotalArtifactBonus(a => a.qualityLuckBonus);
                
                if (cachedPlayer?.GetHeldItem() is FishingRodData rod)
                    luck = rod.qualityLuckModifier;

                FishQuality quality = FishQuality.Bronze;
                float roll = UnityEngine.Random.value + luck + artifactLuck;

                if (roll > 0.95f) quality = FishQuality.Gold;
                else if (roll > 0.70f) quality = FishQuality.Silver;

                cachedInventory.AddItem(fish, 1, quality);
            }

            if (fish != null)
            {
                Debug.Log("Firing OnItemCaught event for quest system: " + fish.itemName);
                GameEvents.OnItemCaught?.Invoke(fish);
            }

            if (UIManager.Instance != null)
                UIManager.Instance.ShowMessage("Caught: " + fish.itemName);
        }
        else
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowMessage("Fish Escaped!");
        }

        Cleanup();
    }

    void HandleFishEscape()
    {
        cachedPlayer?.SetPulling(false);

        state = FishingState.Result;

        if (UIManager.Instance != null)
            UIManager.Instance.ShowMessage("Fish Escaped!");

        if (UnityEngine.Random.value < loseBaitChance)
        {
            ConsumeBait();
        }

        OnFishEscaped?.Invoke();
        Cleanup();
    }

    void Cleanup()
    {
        if (currentBobber != null)
            Destroy(currentBobber.gameObject);

        pendingArtifact = null;

        if (runtimeArtifactStruggle != null)
        {
            Destroy(runtimeArtifactStruggle);
            runtimeArtifactStruggle = null;
        }

        if (audioSource != null && audioSource.isPlaying && audioSource.clip == pullSFX) {
            audioSource.Stop();
            audioSource.loop = false;
        }

        cachedPlayer?.StopFishingAnimation();

        StartCoroutine(ResetDelay());
    }

    IEnumerator ResetDelay()
    {
        inputLocked = true;
        yield return new WaitForSeconds(0.2f); 
        ResetFishing();
        inputLocked = false;
    }

    private void ConfirmReleaseFish(ItemData item)
    {
        if (item == null) return;

        if (item is FishData fish)
        {
            int bonus = fish.isProtectedSpecies ? Mathf.Abs(fish.sustainabilityPenalty) : 3;
            SustainabilityManager.Instance?.Add(bonus);
            UIManager.Instance?.ShowMessage($"Released {fish.itemName}. Sustainability improved!");
        }
        else if (item.itemType == ItemType.Junk)
        {
            SustainabilityManager.Instance?.Add(-5); 
            UIManager.Instance?.ShowMessage($"Littered {item.itemName}. Sustainability decreased!");
        }

        ConsumeHeldItem();
    }

    void ResetFishing()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameState.Normal);

        state = FishingState.Idle;
        holdTime = 0;
    }

    public void CancelFishing()
    {
        if (state == FishingState.Idle) return;

        if ((state == FishingState.Waiting || state == FishingState.Biting) && UnityEngine.Random.value < loseBaitChance)
        {
            ConsumeBait();
        }

        if (currentBobber != null)
            Destroy(currentBobber.gameObject);
            
        cachedPlayer?.StopFishingAnimation();
        ResetFishing();
    }

    private InventoryItem FindBaitInInventory()
    {
        if (cachedInventory == null) return null;
        return cachedInventory.itemList.Find(slot => slot.item != null && slot.item.itemType == ItemType.Bait && slot.amount > 0);
    }

    private void ConsumeBait()
    {
        InventoryItem bait = FindBaitInInventory();
        if (bait != null)
        {
            bait.amount--;
            if (bait.amount <= 0) bait.item = null;
            cachedInventory?.OnInventoryChanged?.Invoke();
        }
    }

    public void ConsumeHeldItem()
    {
        if (HotbarManager.Instance == null || cachedInventory == null) return;

        int index = HotbarManager.Instance.selectedIndex;
        if (index >= 0 && index < cachedInventory.itemList.Count)
        {
            InventoryItem slot = cachedInventory.itemList[index];
            if (slot.item != null)
            {
                slot.amount--;
                if (slot.amount <= 0)
                {
                    slot.item = null;
                }
                cachedInventory.OnInventoryChanged?.Invoke();
            }
        }
    }
}