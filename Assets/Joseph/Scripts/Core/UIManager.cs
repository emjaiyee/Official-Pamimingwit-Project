﻿// TODO: Fix audio not matching advance day

using UnityEngine;
using TMPro;
using System;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI; 
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public enum ActiveShopType { None, General, Industrial, TrashDisposal }
    [Header("Shop State")]
    public ActiveShopType currentShopType = ActiveShopType.None;

    [Header("Inventory UI")]
    public ItemSlotUI[] inventorySlots;
    public GameObject inventoryPanel;

    [Header("Hotbar UI")]
    public GameObject hotbarPanel;
    public ItemSlotUI[] hotbarSlots;

    [Header("Shop UI")]
    public GameObject shopPanel;
    public Transform shopContent; // The parent container for shop slots
    public GameObject industrialShopPanel;
    public Transform industrialShopContent;
    public GameObject shopSlotPrefab;
    public GameObject trashDisposalPanel;

    [Header("Stamina UI")]
    public GameObject staminaPanel; // Optional: if you want to show/hide the whole bar
    public Slider staminaSlider;
    public Image staminaFillImage;
    public TextMeshProUGUI staminaText;
    [SerializeField] private float lowStaminaThreshold = 0.2f; // 20%
    [SerializeField] private float pulseSpeed = 4f;
    [SerializeField] private float pulseScaleAmount = 0.15f;
    [SerializeField] private float staminaSmoothSpeed = 10f;
    private Coroutine pulseCoroutine;
    private float targetStamina;
    private float targetMaxStamina;
    private float visualStamina;
    private bool initializedStamina;



    [Header("Day Cycle UI")]
    public TextMeshProUGUI dayHUDText;
    public CanvasGroup dayTransitionOverlay;
    public TextMeshProUGUI dayTransitionText;
    public TextMeshProUGUI taxTransitionText;
    [SerializeField] private float typewriterSpeed = 0.05f;
    [SerializeField] private float overlayFadeDuration = 2.0f;
    [SerializeField] private float taxFadeDuration = 1.0f;
    [SerializeField] private float stayBlackDuration = 2.0f;

    [Header("Cleaning Mini-game UI")]
    public GameObject choicePanel;
    public TextMeshProUGUI choicePromptText;
    public Button throwInOceanButton;
    public TextMeshProUGUI throwInOceanText;
    public Button cleanUpButton;
    public TextMeshProUGUI cleanUpText;
    public GameObject cleaningPanel;

    [Header("Cutscene UI")]
    public GameObject cutscenePanel;
    public CanvasGroup cutsceneFadeOverlay;
    public Image cutsceneBackground;
    public Image cutsceneCharacter;
    public TextMeshProUGUI cutsceneNameText;
    public TextMeshProUGUI cutsceneContentText;

    [Header("Dialogue UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueNameText;
    public TextMeshProUGUI dialogueContentText;
    public Image dialogueIcon;

    // TODO: Revive Crafting System
    [Header("Crafting UI")]
    public GameObject craftingPanel;
    public CraftingResultSlot resultSlot;

    [Header("Quest UI")]
    public GameObject questPanel;


    [Header("Game Messages")]
    public TextMeshProUGUI messageText;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {

        staminaSlider.interactable = false;

        if (hotbarPanel != null) hotbarPanel.SetActive(true);

        if (Inventory.Instance != null)
        {
            Inventory.Instance.OnInventoryChanged += RefreshInventory;

        }

        HideCraftingPanel();
        HideShopPanel();
        choicePanel?.SetActive(false);
        if (questPanel != null) questPanel.SetActive(false);
        cleaningPanel?.SetActive(false);
        cutscenePanel?.SetActive(false);

        // Only reset alpha if a cutscene isn't already handling the transition
        if (cutsceneFadeOverlay != null && (CutsceneManager.Instance == null || !CutsceneManager.Instance.IsCutsceneActive)) 
            cutsceneFadeOverlay.alpha = 0f;
            
        dialoguePanel?.SetActive(false);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);

        // Subscribe to StaminaManager events
        if (StaminaManager.Instance != null)
        {
            StaminaManager.Instance.OnStaminaChanged += UpdateStaminaUI;
        }

        // 3. Initial UI Sync and Overlay Reset
        if (dayTransitionOverlay != null)
        {
            // If we are about to load a save, keep the screen black to hide snapping/warping
            dayTransitionOverlay.alpha = SaveController.shouldLoadGame ? 1f : 0f;
        }
        RefreshInventory();
    }

    public void StartDayTransition(Action onFadeComplete)
    {
        StartCoroutine(DayTransitionRoutine(onFadeComplete));
    }

    private IEnumerator DayTransitionRoutine(Action onFadeComplete)
    {
        if (dayTransitionOverlay == null) yield break;

        GameManager.Instance?.SetState(GameState.UI);
        PlayerController.Instance?.LockMovement();

        // Clear previous text immediately so it doesn't show during the fade-out
        if (dayTransitionText != null)
        {
            dayTransitionText.text = "";
            dayTransitionText.alpha = 1f; 
        }
        if (taxTransitionText != null) 
        {
            taxTransitionText.text = "";
            taxTransitionText.alpha = 0f;
        }

        // Fade Out
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / overlayFadeDuration;
            dayTransitionOverlay.alpha = t;
            yield return null;
        }
        dayTransitionOverlay.alpha = 1f;

        // Update day and show text
        GameManager.Instance?.AdvanceDay();

        
        GameEvents.TriggerSound(SoundType.Rooster);

        GameEvents.TriggerSound(SoundType.MorningAmbience);

        // Animate Day Text
        if (dayTransitionText != null)
        {
            string dayStr = $"DAY: {GameManager.Instance.currentDay}";
            yield return StartCoroutine(TypewriterEffect(dayTransitionText, dayStr));
        }

        // Animate Tax Text
        if (taxTransitionText != null && GameManager.Instance != null)
        {
            string taxMessage = GameManager.Instance.ProcessTax();
            taxTransitionText.text = taxMessage;

            if (taxMessage.Contains("deducted"))
                GameEvents.TriggerSound(SoundType.TaxPaid);
            else if (taxMessage.Contains("doubles"))
                GameEvents.TriggerSound(SoundType.TaxFailed);

            float textFadeT = 0f;
            while (textFadeT < 1f)
            {
                textFadeT += Time.deltaTime / taxFadeDuration;
                taxTransitionText.alpha = textFadeT;
                yield return null;
            }
            taxTransitionText.alpha = 1f;
        }
        
        onFadeComplete?.Invoke();
        yield return new WaitForSeconds(stayBlackDuration); // Longer wait to let the player read

        // Fade out both texts while screen is still black
        float textFadeOutT = 1f;
        while (textFadeOutT > 0)
        {
            textFadeOutT -= Time.deltaTime / taxFadeDuration;
            if (dayTransitionText != null) dayTransitionText.alpha = textFadeOutT;
            if (taxTransitionText != null) taxTransitionText.alpha = textFadeOutT;
            yield return null;
        }

        if (dayTransitionText != null) dayTransitionText.alpha = 0f;
        if (taxTransitionText != null) taxTransitionText.alpha = 0f;


        while (t > 0)
        {
            t -= Time.deltaTime / overlayFadeDuration;
            dayTransitionOverlay.alpha = Mathf.Max(t, 0);


            yield return null;
        }

        GameEvents.TriggerSound(SoundType.StopTransitionAudio);

        dayTransitionOverlay.alpha = 0f;

        GameManager.Instance?.SetState(GameState.Normal);
        PlayerController.Instance?.UnlockMovement();
    }

    private IEnumerator TypewriterEffect(TextMeshProUGUI textComponent, string content)
    {
        textComponent.text = "";
        

        foreach (char c in content.ToCharArray())
        {
            textComponent.text += c;
            GameEvents.TriggerSound(SoundType.Typewriter);        
            yield return new WaitForSeconds(typewriterSpeed);
         }
    }

    void Update()
    {
        if (staminaSlider != null && targetMaxStamina > 0)
        {
            // Smoothly interpolate the visual value toward the target logical stamina
            visualStamina = Mathf.Lerp(visualStamina, targetStamina, Time.deltaTime * staminaSmoothSpeed);
            
            // Snap to target if very close to avoid micro-updates and floating point drift
            if (Mathf.Abs(visualStamina - targetStamina) < 0.01f) visualStamina = targetStamina;

            staminaSlider.maxValue = targetMaxStamina;
            staminaSlider.value = visualStamina;

            if (staminaText != null) 
                staminaText.text = $"{Mathf.CeilToInt(visualStamina)} / {Mathf.CeilToInt(targetMaxStamina)}";

            // Smooth color transition based on the current visual state of the bar
            if (staminaFillImage != null)
            {
                float percent = visualStamina / targetMaxStamina;
                staminaFillImage.color = Color.Lerp(Color.red, Color.green, percent);
            }
        }
    }

    // ---------------------------
    // CENTRALIZED PANEL MANAGEMENT
    // ---------------------------

    /// <summary>
    /// Unified method to open/close panels while handling GameState, movement locking, and sound effects.
    /// </summary>
    public void TogglePanelState(GameObject panel, bool? forceState = null, bool playSFX = true)
    {
        if (panel == null) return;

        bool targetState = forceState ?? !panel.activeSelf;
        if (panel.activeSelf == targetState) return;

        panel.SetActive(targetState);

        if (targetState)
        {
            if (playSFX) PlayPanelOpenSFX();
            GameManager.Instance?.SetState(GameState.UI);
            PlayerController.Instance?.LockMovement();
        }
        else
        {
            if (playSFX) PlayPanelCloseSFX();
            CheckAndRestoreState();
        }
    }

    // ---------------------------
    // INVENTORY
    // ---------------------------
    public void RefreshInventory()
    {
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i] != null)
            {
                inventorySlots[i].Refresh();
            }
        }

        foreach (var slot in hotbarSlots)
        {
            if (slot != null) slot.Refresh();
        }
    }

    public void ToggleInventory()
    {
        TogglePanelState(inventoryPanel);
    }

    // ---------------------------
    // SHOP
    // ---------------------------
    private void RefreshShopStock(ItemData[] stock, Transform targetContent)
    {
        if (targetContent == null || shopSlotPrefab == null) return;

        // Clear existing slots
        foreach (Transform child in targetContent)
        {
            Destroy(child.gameObject);
        }

        foreach (ItemData item in stock)
        {
            GameObject obj = Instantiate(shopSlotPrefab, targetContent);
            ShopSlotUI slot = obj.GetComponent<ShopSlotUI>();
            if (slot != null) slot.Setup(item);
        }
    }

    public void OpenShop()
    {
        if (shopPanel == null) return;

        currentShopType = ActiveShopType.General;
        if (ShopManager.Instance != null) RefreshShopStock(ShopManager.Instance.shopStock, shopContent);

        TogglePanelState(shopPanel, true);
        if (inventoryPanel != null) TogglePanelState(inventoryPanel, true, false); // Prevent redundant SFX
    }

    public void OpenIndustrialShop()
    {
        if (industrialShopPanel == null) return;
        
        currentShopType = ActiveShopType.Industrial;
        if (IndustrialShopManager.Instance != null) RefreshShopStock(IndustrialShopManager.Instance.shopStock, industrialShopContent);

        TogglePanelState(industrialShopPanel, true);
        if (inventoryPanel != null) TogglePanelState(inventoryPanel, true, false);
    }

    public void OpenTrashDisposal()
    {
        currentShopType = ActiveShopType.TrashDisposal;
        TogglePanelState(trashDisposalPanel, true);
        if (inventoryPanel != null) TogglePanelState(inventoryPanel, true, false);
    }

    public void HideShopPanel()
    {
        TogglePanelState(shopPanel, false);
        TogglePanelState(industrialShopPanel, false, false);
        TogglePanelState(trashDisposalPanel, false, false);
        TogglePanelState(inventoryPanel, false, false);

        currentShopType = ActiveShopType.None;
    }

    public void ToggleShop()
    {
        bool target = !shopPanel.activeSelf;
        TogglePanelState(shopPanel, target);
        if (inventoryPanel != null) TogglePanelState(inventoryPanel, target, false);

        if (!target) currentShopType = ActiveShopType.None;
    }

    // ---------------------------
    // CRAFTING
    // ---------------------------
    public void ShowCraftingPanel()
    {
        TogglePanelState(craftingPanel, true);
    }

    public void HideCraftingPanel()
    {
        TogglePanelState(craftingPanel, false);
    }

    public void ToggleCrafting()
    {
        TogglePanelState(craftingPanel);
    }

    // ---------------------------
    // QUESTS
    // ---------------------------
    public void ToggleQuest()
    {
        TogglePanelState(questPanel);
    }

    /// <summary>
    /// Closes all active gameplay panels (Inventory, Crafting, Shops, etc.)
    /// specifically ignoring narrative elements like Dialogue and Cutscenes.
    /// </summary>
    public void CloseAllStandardPanels()
    {
        // Per requirement: do not close if dialogue or cutscenes are active
        if ((dialoguePanel != null && dialoguePanel.activeSelf) || 
            (cutscenePanel != null && cutscenePanel.activeSelf)) return;

        bool closedSomething = false;

        if (inventoryPanel != null && inventoryPanel.activeSelf) { TogglePanelState(inventoryPanel, false); closedSomething = true; }
        if (craftingPanel != null && craftingPanel.activeSelf) { TogglePanelState(craftingPanel, false); closedSomething = true; }
        if (questPanel != null && questPanel.activeSelf) { TogglePanelState(questPanel, false); closedSomething = true; }
        if (choicePanel != null && choicePanel.activeSelf) { HideChoicePanel(); closedSomething = true; }
        if (cleaningPanel != null && cleaningPanel.activeSelf) { TogglePanelState(cleaningPanel, false); closedSomething = true; }
        
        if ((shopPanel != null && shopPanel.activeSelf) || 
            (industrialShopPanel != null && industrialShopPanel.activeSelf) || 
            (trashDisposalPanel != null && trashDisposalPanel.activeSelf))
        {
            HideShopPanel();
            closedSomething = true;
        }

        if (closedSomething) TooltipUI.Instance?.HideTooltip();
    }

    // ---------------------------
    // MESSAGE SYSTEM
    // ---------------------------
    public void ShowMessage(string msg)
    {
        if (messageText == null) return;
        messageText.text = msg;
    }

    // ---------------------------
    // HELPER
    // ---------------------------
    
    /// <summary>
    /// Triggers a generic choice pop-up.
    /// </summary>
    public void ShowChoice(string prompt, string opt1Text, Action opt1Action, string opt2Text, Action opt2Action)
    {
        if (choicePanel == null || throwInOceanButton == null || cleanUpButton == null) return;

        if (choicePromptText != null) choicePromptText.text = prompt;

        // Set labels
        if (throwInOceanText != null) throwInOceanText.text = opt1Text;
        if (cleanUpText != null) cleanUpText.text = opt2Text;

        // Clear and bind actions
        throwInOceanButton.onClick.RemoveAllListeners();
        cleanUpButton.onClick.RemoveAllListeners();

        throwInOceanButton.onClick.AddListener(() => {
            choicePanel.SetActive(false);
            opt1Action?.Invoke();
            CheckAndRestoreState();
        });

        cleanUpButton.onClick.AddListener(() => {
            choicePanel.SetActive(false);
            opt2Action?.Invoke();
            CheckAndRestoreState();
        });

        TogglePanelState(choicePanel, true);
    }

    private void CheckAndRestoreState()
    {
        // Only restore state if no other UI panel is currently active
        if (!IsUIOpen())
        {
            GameManager.Instance?.SetState(GameState.Normal);
            PlayerController.Instance?.UnlockMovement();
        }
    }

    public void HideChoicePanel()
    {
        TogglePanelState(choicePanel, false);
    }

    // --- NEW: Stamina UI Update ---
    public void UpdateStaminaUI(float currentStamina, float maxStamina)
    {
        targetStamina = currentStamina;
        targetMaxStamina = maxStamina;

        // On the very first update, snap the visual value so it doesn't "fill up" from 0 at game start
        if (!initializedStamina)
        {
            visualStamina = currentStamina;
            initializedStamina = true;
        }

        bool isLow = maxStamina > 0 && (currentStamina / maxStamina) <= lowStaminaThreshold;

        if (isLow)
        {
            if (pulseCoroutine == null)
                pulseCoroutine = StartCoroutine(PulseStaminaPanel());
        }
        else
        {
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
                if (staminaPanel != null) staminaPanel.transform.localScale = Vector3.one;
            }
        }
    }

    private IEnumerator PulseStaminaPanel()
    {
        if (staminaPanel == null) yield break;

        while (true)
        {
            float scale = 1f + Mathf.PingPong(Time.time * pulseSpeed, pulseScaleAmount);
            staminaPanel.transform.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
    }
    public void PlayPanelOpenSFX()
    {
        GameEvents.TriggerSound(SoundType.PanelOpen);
    }

    public void PlayPanelCloseSFX()
    {
        GameEvents.TriggerSound(SoundType.PanelClose);
    }

    public bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    public bool IsUIOpen()
    {
        return (inventoryPanel != null && inventoryPanel.activeSelf) ||
               (craftingPanel != null && craftingPanel.activeSelf) ||
               (shopPanel != null && shopPanel.activeSelf) ||
               (industrialShopPanel != null && industrialShopPanel.activeSelf) ||
               (trashDisposalPanel != null && trashDisposalPanel.activeSelf) ||
               (questPanel != null && questPanel.activeSelf) ||
               (choicePanel != null && choicePanel.activeSelf) ||
               (cleaningPanel != null && cleaningPanel.activeSelf) ||
               (cutscenePanel != null && cutscenePanel.activeSelf) ||
               (dialoguePanel != null && dialoguePanel.activeSelf);
    }
}