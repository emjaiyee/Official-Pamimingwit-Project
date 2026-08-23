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
    public GameObject dynamitePrefab; // Reference to a visual prefab with DynamiteProjectile script
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

    float holdTime;
    bool inputLocked;

    private Vector3 pendingTargetPos;
    private ItemData pendingItem;
    private bool pendingIsDynamite;
    private bool isCastPending; // Guard to prevent double-firing animation events

    FishingBobber currentBobber;

    private ArtifactData pendingArtifact;
    FishData hookedFish;

    private FishData runtimeArtifactStruggle;


    public static event Action<FishData> OnFishHooked;
    public static event Action OnFishEscaped;

    public static event Action<float, float> OnCameraShakeRequested;

    void Awake()
    {
        Instance = this;

        if (fishingLine != null)
        {
            fishingLine.useWorldSpace = true;
        }
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

        // // DEBUG: Press 'T' while in Debug Mode to simulate a catch and quality roll
        // if (debugMode && Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        // {
        //     if (fishPool != null && fishPool.Length > 0)
        //     {
        //         if (fishPool[0] is FishData fish)
        //             FinishReel(true, fish);
        //         else
        //         {
        //             Inventory.Instance?.AddItem(fishPool[0]);
        //             UIManager.Instance?.ShowMessage($"Debug Catch: {fishPool[0].itemName}");
        //             Cleanup();
        //         }
        //     }
        // }
    }

    void LateUpdate()
    {
        UpdateFishingLine();
    }

    void HandleInput()
    {
        if (inputLocked) return;

        if (GameManager.Instance != null &&
            GameManager.Instance.currentState == GameState.UI)
            return;

        bool hasRod = EquipmentManager.Instance != null && EquipmentManager.Instance.hasFishingRodEquipped;
        bool hasDynamite = EquipmentManager.Instance != null && EquipmentManager.Instance.hasDynamiteEquipped;
        ItemData heldItem = PlayerController.Instance?.GetHeldItem();
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

                // Prevent fishing with a rod if no bait is found in the inventory (Enforced even in debug)
                if (hasRod && FindBaitInInventory() == null)
                {
                    UIManager.Instance?.ShowMessage("You need bait to use the rod!");
                    return;
                }

                // --- NEW: Stamina Check ---
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
                StartReel(); // 🆕 changed
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

        // Mirroring Logic: Since we use spriteRenderer.flipX in PlayerController, 
        // child transforms don't physically flip.
        var pController = PlayerController.Instance;
        if (pController != null && pController.GetComponent<SpriteRenderer>().flipX)
        {
            // We use the player's position as the pivot for mirroring.
            // If the rod looks 'off' on the left, ensure the Player Sprite's pivot 
            // is centered horizontally in the Sprite Editor.
            Vector3 localPos = player.InverseTransformPoint(worldPos);
            
            // Mirror the X position
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

            // Force Z to 0 to ensure it's on the same plane as the 2D sprites
            startPos.z = 0;
            endPos.z = 0;

            fishingLine.enabled = true;
            fishingLine.positionCount = lineResolution;

            // Tauten the line (low sag) when flying, biting, or reeling.
            bool isLineTense = currentBobber.IsFlying || state == FishingState.Biting || state == FishingState.Result;
            float currentSag = isLineTense ? 0.02f : lineSagAmount;

            for (int i = 0; i < lineResolution; i++)
            {
                float t = i / (float)(lineResolution - 1);
                Vector3 pos = Vector3.Lerp(startPos, endPos, t);

                // Apply a simple downward arc using a sine wave
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
        // Prevent starting the aiming state if the player is not targeting water
        Vector3 mouse = GetMouseWorldPosition();
        Vector3 dir = (mouse - player.position).normalized;
        Vector3 initialTarget = player.position + dir * 1f; // Check at minimum possible cast distance

        if (!Physics2D.OverlapCircle(initialTarget, 0.2f, waterLayer))
            return;

        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameState.Fishing);

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.StartAiming(isDynamite);
        }

        state = FishingState.Aiming;
        holdTime = 0;
        isCastPending = false;
        pendingIsDynamite = isDynamite;
        pendingItem = heldItem;

        // Immediately update direction so the aiming animation shows up instantly
        ChargeCast();
    }

    void ChargeCast()
    {
        holdTime += Time.deltaTime;
        holdTime = Mathf.Clamp(holdTime, 0, maxHoldTime);

        // Update direction while aiming so the player doesn't feel frozen
        Vector3 mouse = GetMouseWorldPosition();
        Vector3 dir = (mouse - player.position).normalized;

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetFishingDirection(dir);
        }
    }

    void CastRod()
    {
        float power = holdTime / maxHoldTime;
        Vector3 mouse = GetMouseWorldPosition();
        Vector3 dir = (mouse - player.position).normalized;
        float distance = Mathf.Lerp(1f, 4f, power);
        pendingTargetPos = player.position + dir * distance;

        // Ensure the target is on the Water Layer for both regular and dynamite fishing.
        if (!Physics2D.OverlapCircle(pendingTargetPos, 0.2f, waterLayer))
        {
            Cleanup(); // Correctly resets animator triggers and restores game state
            return;
        }

        // --- NEW: Consume Stamina ---
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

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetFishingDirection(dir);
            if (pendingIsDynamite)
                PlayerController.Instance.PlayThrowAnimation();
            else
                PlayerController.Instance.PlayCastAnimation();
        }

        isCastPending = true;
    }

    // This is called by an Animation Event via PlayerController
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

        // Start the throw from the rod tip, same as the fishing rod
        Vector3 start = GetRodTipPosition(); 
        start.z = 0; // Ensure it's on the rendering plane
        GameObject d = Instantiate(dynamitePrefab, start, Quaternion.identity);

        DynamiteProjectile proj = d.GetComponent<DynamiteProjectile>();
        if (proj != null)
        {
            proj.Launch(targetPos, () => ExplodeDynamite(targetPos, data));
        }
        else
        {
            //Debug.LogError("FishingManager: The Dynamite Prefab is missing the DynamiteProjectile component!");
            Destroy(d);
            Cleanup();
        }
    }

    private void ExplodeDynamite(Vector3 targetPos, ItemData data)
    {
        
        OnCameraShakeRequested?.Invoke(shakeDuration, shakeMagnitude);

        // 1. Visual Effects
        if (explosionParticlePrefab != null)
        {
            Vector3 particlePos = new Vector3(targetPos.x, targetPos.y, 0);
            GameObject explosion = Instantiate(explosionParticlePrefab, particlePos, Quaternion.identity);
            // Ensure the explosion is destroyed even if it doesn't have its own cleanup script
            Destroy(explosion, 3f); 
        }

        // Try to get stats from DynamiteData, or use defaults if it's a generic item named 'Dynamite'
        DynamiteData dynData = data as DynamiteData;
        int penalty = dynData != null ? dynData.sustainabilityPenalty : -10;
        int haul = dynData != null ? dynData.haulSize : 2;

        // 2. Apply Sustainability Penalty
        SustainabilityManager.Instance?.Add(penalty);

        // 3. Catch multiple fish immediately (the "Haul")
        float oceanMultiplier = ReactiveOceanManager.Instance != null ? ReactiveOceanManager.Instance.GetCurrentTier().haulMultiplier : 1f;
        
        int bonusHaul = UnityEngine.Random.Range(1, 4); 
        int finalHaul = Mathf.Max(1, Mathf.RoundToInt(haul * oceanMultiplier) + bonusHaul);

        for (int i = 0; i < finalHaul; i++)
        {
            ItemData caught = (ReactiveOceanManager.Instance != null) ? ReactiveOceanManager.Instance.GetRandomCatch() : null;
            if (caught == null && fishPool.Length > 0) caught = fishPool[UnityEngine.Random.Range(0, fishPool.Length)];

            if (caught != null && Inventory.Instance != null && Inventory.Instance.AddItem(caught))
            {
                UIManager.Instance?.ShowMessage($"Dynamite caught: {caught.itemName}");
                GameEvents.OnItemCaught?.Invoke(caught);
            }
        }

        // 3. Consume the Dynamite (Single use)
        ConsumeHeldItem();

        // 4. Reset
        //Debug.Log("BOOM! Dynamite fishing completed.");
        Cleanup();
    }

    IEnumerator WaitForBite()
    {
        float catchModifier = 1f;
        float artifactBonus = Inventory.Instance != null ? Inventory.Instance.GetTotalArtifactBonus(a => a.catchRateBonus) : 0f;

        // 1. Check if held item is a specialized rod
        if (PlayerController.Instance?.GetHeldItem() is FishingRodData rod)
            catchModifier *= rod.catchRateMultiplier;
            
        catchModifier += artifactBonus;

        // 2. Check for bait in inventory
        InventoryItem baitStack = FindBaitInInventory();
        if (baitStack != null)
        {
            float bonus = (baitStack.item is BaitData bait) ? bait.catchRateBonus : 1.2f;
            catchModifier *= bonus;
            //Debug.Log($"Using Bait: {baitStack.item.itemName}. Catch Modifier: {catchModifier}");
        }

        float wait = UnityEngine.Random.Range(2f, 5f) / catchModifier;
        yield return new WaitForSeconds(wait);

        if (state != FishingState.Waiting) yield break;

        state = FishingState.Biting;

        // Pick fish based on current bait
        BaitData currentBait = (baitStack != null) ? baitStack.item as BaitData : null;
        ItemData caughtItem = SelectCatch(currentBait);

        // Junk is caught instantly
        if (caughtItem != null && caughtItem.itemType == ItemType.Junk)
        {
            Inventory.Instance?.AddItem(caughtItem);
            UIManager.Instance?.ShowMessage($"Caught junk: {caughtItem.itemName}");
            GameEvents.OnItemCaught?.Invoke(caughtItem);
            ConsumeBait(); // Catching junk consumes the bait
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

        // Apply bait reaction window extension
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
        // Priority 1: Check the Ocean Tier for special environmental items (Artifacts/Junk)
        if (ReactiveOceanManager.Instance != null)
        {
            ItemData tierCatch = ReactiveOceanManager.Instance.GetRandomCatch();
            
            // If we roll an artifact or junk from the tier system, return it immediately.
            // These types are environmental and ignore bait preferences.
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
                // A fish is eligible if it has no bait requirement, 
                // or if the player is using the required bait.
                if (fish.requiredBait == null || (bait != null && fish.requiredBait == bait))
                {
                    eligibleItems.Add(fish);
                }
            }
            else if (item != null)
            {
                eligibleItems.Add(item); // Junk items are always eligible
            }
        }

        if (eligibleItems.Count == 0) return null;
        return eligibleItems[UnityEngine.Random.Range(0, eligibleItems.Count)];
    }

    // 🆕 START REEL
    void StartReel()
    {
        PlayerController.Instance?.SetPulling(true);

        PlayerController.Instance?.PlayPullAnimation();

        state = FishingState.Result;

        ConsumeBait(); // Bait is consumed the moment you hook the fish

        OnFishHooked?.Invoke(hookedFish);

        if (ReelMinigame.Instance != null)
        {
            ReelMinigame.Instance.StartMinigame(hookedFish);
        }
    }

    // 🆕 RESULT FROM MINIGAME
    public void FinishReel(bool success, FishData fish)
    {
        PlayerController.Instance?.SetPulling(false);

        if (success)
        {
            if (pendingArtifact != null)
            {
                if (Inventory.Instance != null)
                    Inventory.Instance.AddItem(pendingArtifact);

                UIManager.Instance?.ShowMessage("Recovered Artifact: " + pendingArtifact.itemName);
                GameEvents.OnItemCaught?.Invoke(pendingArtifact);
                pendingArtifact = null;
                Cleanup();
                return;
            }

            if (Inventory.Instance != null)
            {
                float luck = 0f;
                float artifactLuck = Inventory.Instance.GetTotalArtifactBonus(a => a.qualityLuckBonus);
                
                if (PlayerController.Instance?.GetHeldItem() is FishingRodData rod)
                    luck = rod.qualityLuckModifier;

                FishQuality quality = FishQuality.Bronze;
                float roll = UnityEngine.Random.value + luck + artifactLuck;

                if (roll > 0.95f) quality = FishQuality.Gold;
                else if (roll > 0.70f) quality = FishQuality.Silver;

                Inventory.Instance.AddItem(fish, 1, quality);
            }

            // 🔥🔥🔥 THIS IS THE FIX 🔥🔥🔥
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
        PlayerController.Instance?.SetPulling(false);

        state = FishingState.Result;

        if (UIManager.Instance != null)
            UIManager.Instance.ShowMessage("Fish Escaped!");

        // Chance to lose bait even if the fish escaped the hook
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

        // Ensure Animator returns to Idle immediately
        PlayerController.Instance?.StopFishingAnimation();

        StartCoroutine(ResetDelay());
    }

    IEnumerator ResetDelay()
    {
        inputLocked = true;

        yield return new WaitForSeconds(0.2f); // Reduced delay for a snappier feel

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
            SustainabilityManager.Instance?.Add(-5); // Penalty for littering junk back into sea
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

        // If reeled in manually while waiting or during a bite, there's a chance to lose the bait
        if ((state == FishingState.Waiting || state == FishingState.Biting) && UnityEngine.Random.value < loseBaitChance)
        {
            ConsumeBait();
        }

        if (currentBobber != null)
            Destroy(currentBobber.gameObject);
            
        PlayerController.Instance?.StopFishingAnimation();
        ResetFishing();
    }

    private InventoryItem FindBaitInInventory()
    {
        if (Inventory.Instance == null) return null;
        return Inventory.Instance.itemList.Find(slot => slot.item != null && slot.item.itemType == ItemType.Bait && slot.amount > 0);
    }

    private void ConsumeBait()
    {
        InventoryItem bait = FindBaitInInventory();
        if (bait != null)
        {
            bait.amount--;
            if (bait.amount <= 0) bait.item = null;
            Inventory.Instance.OnInventoryChanged?.Invoke();
        }
    }

    public void ConsumeHeldItem()
    {
        if (HotbarManager.Instance == null || Inventory.Instance == null) return;

        int index = HotbarManager.Instance.selectedIndex;
        if (index >= 0 && index < Inventory.Instance.itemList.Count)
        {
            InventoryItem slot = Inventory.Instance.itemList[index];
            if (slot.item != null)
            {
                // Trigger reverse pop if this is the last item in the stack
                if (slot.amount <= 1 && UIManager.Instance != null && UIManager.Instance.hotbarSlots.Length > index)
                {
                    ItemSlotUI slotUI = UIManager.Instance.hotbarSlots[index];
                    slotUI.AnimatePopOut(() => 
                    {
                        slot.amount = 0;
                        slot.item = null;
                        Inventory.Instance.OnInventoryChanged?.Invoke();
                    });
                }
                else
                {
                    slot.amount--;
                    if (slot.amount <= 0) slot.item = null;
                    Inventory.Instance.OnInventoryChanged?.Invoke();
                }
            }
        }
    }

}