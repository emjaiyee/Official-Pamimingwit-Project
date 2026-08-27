﻿using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public enum ActiveShopType { None, General, Industrial, TrashDisposal }

    [Header("Shop State")]
    public ActiveShopType currentShopType = ActiveShopType.None;

    [Header("Shop UI")]
    public GameObject shopPanel;
    public Transform shopContent;
    public GameObject industrialShopPanel;
    public Transform industrialShopContent;
    public GameObject shopSlotPrefab;
    public GameObject trashDisposalPanel;

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

    [Header("Crafting UI")]
    public GameObject craftingPanel;
    public CraftingResultSlot resultSlot;

    [Header("Quest UI")]
    [SerializeField] private GameObject questPanel;

    [Header("Game Messages")]
    [SerializeField] private TextMeshProUGUI messageText;

    private Coroutine activeDayTransitionCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        HideCraftingPanel();
        HideShopPanel();
        if (choicePanel != null) choicePanel.SetActive(false);
        if (questPanel != null) questPanel.SetActive(false);
        if (cleaningPanel != null) cleaningPanel.SetActive(false);
        if (cutscenePanel != null) cutscenePanel.SetActive(false);
        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        if (cutsceneFadeOverlay != null && (CutsceneManager.Instance == null || !CutsceneManager.Instance.IsCutsceneActive))
        {
            cutsceneFadeOverlay.alpha = 0f;
        }

        if (dayTransitionOverlay != null)
        {
            dayTransitionOverlay.alpha = SaveController.shouldLoadGame ? 1f : 0f;
        }
    }

    // ---------------------------
    // DAY TRANSITION SYSTEM
    // ---------------------------
    public void StartDayTransition(Action onFadeComplete)
    {
        if (activeDayTransitionCoroutine != null)
        {
            StopCoroutine(activeDayTransitionCoroutine);
        }
        activeDayTransitionCoroutine = StartCoroutine(DayTransitionRoutine(onFadeComplete));
    }

    private IEnumerator DayTransitionRoutine(Action onFadeComplete)
    {
        if (dayTransitionOverlay == null) yield break;

        GameManager.Instance?.SetState(GameState.UI);
        PlayerController.Instance?.LockMovement();

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

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / overlayFadeDuration;
            dayTransitionOverlay.alpha = Mathf.Min(t, 1f);
            yield return null;
        }
        dayTransitionOverlay.alpha = 1f;

        GameManager.Instance?.AdvanceDay();
        GameEvents.TriggerSound(SoundType.Rooster);

        if (dayTransitionText != null)
        {
            string dayStr = $"DAY: {GameManager.Instance.currentDay}";
            yield return StartCoroutine(TypewriterEffect(dayTransitionText, dayStr));
        }

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

        GameEvents.TriggerSound(SoundType.MorningAmbience);
        yield return new WaitForSeconds(stayBlackDuration);

        float textFadeOutT = 1f;
        while (textFadeOutT > 0f)
        {
            textFadeOutT -= Time.deltaTime / taxFadeDuration;
            if (dayTransitionText != null) dayTransitionText.alpha = textFadeOutT;
            if (taxTransitionText != null) taxTransitionText.alpha = textFadeOutT;
            yield return null;
        }

        if (dayTransitionText != null) dayTransitionText.alpha = 0f;
        if (taxTransitionText != null) taxTransitionText.alpha = 0f;

        float fadeOutT = 1f;
        while (fadeOutT > 0f)
        {
            fadeOutT -= Time.deltaTime / overlayFadeDuration;
            dayTransitionOverlay.alpha = Mathf.Max(fadeOutT, 0f);
            yield return null;
        }

        GameEvents.TriggerSound(SoundType.StopTransitionAudio);
        dayTransitionOverlay.alpha = 0f;

        GameManager.Instance?.SetState(GameState.Normal);
        PlayerController.Instance?.UnlockMovement();
        activeDayTransitionCoroutine = null;
    }

    private IEnumerator TypewriterEffect(TextMeshProUGUI textComponent, string content)
    {
        textComponent.text = "";
        foreach (char c in content.ToCharArray())
        {
            textComponent.text += c;
            yield return new WaitForSeconds(typewriterSpeed);
        }
    }

    // ---------------------------
    // CENTRALIZED PANEL MANAGEMENT
    // ---------------------------
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
    // SHOPS
    // ---------------------------
    private void RefreshShopStock(ItemData[] stock, Transform targetContent)
    {
        if (targetContent == null || shopSlotPrefab == null) return;

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

        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.InventoryPanel != null)
        {
            TogglePanelState(PlayerUIManager.Instance.InventoryPanel, true, false);
        }
    }

    public void OpenIndustrialShop()
    {
        if (industrialShopPanel == null) return;
        currentShopType = ActiveShopType.Industrial;
        if (IndustrialShopManager.Instance != null) RefreshShopStock(IndustrialShopManager.Instance.shopStock, industrialShopContent);
        TogglePanelState(industrialShopPanel, true);

        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.InventoryPanel != null)
        {
            TogglePanelState(PlayerUIManager.Instance.InventoryPanel, true, false);
        }
    }

    public void OpenTrashDisposal()
    {
        currentShopType = ActiveShopType.TrashDisposal;
        TogglePanelState(trashDisposalPanel, true);

        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.InventoryPanel != null)
        {
            TogglePanelState(PlayerUIManager.Instance.InventoryPanel, true, false);
        }
    }

    public void HideShopPanel()
    {
        TogglePanelState(shopPanel, false);
        TogglePanelState(industrialShopPanel, false, false);
        TogglePanelState(trashDisposalPanel, false, false);

        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.InventoryPanel != null)
        {
            TogglePanelState(PlayerUIManager.Instance.InventoryPanel, false, false);
        }

        currentShopType = ActiveShopType.None;
    }

    public void ToggleShop()
    {
        bool target = !shopPanel.activeSelf;
        TogglePanelState(shopPanel, target);

        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.InventoryPanel != null)
        {
            TogglePanelState(PlayerUIManager.Instance.InventoryPanel, target, false);
        }

        if (!target) currentShopType = ActiveShopType.None;
    }

    // ---------------------------
    // CRAFTING & QUESTS
    // ---------------------------
    public void ShowCraftingPanel() => TogglePanelState(craftingPanel, true);
    public void HideCraftingPanel() => TogglePanelState(craftingPanel, false);
    public void ToggleCrafting() => TogglePanelState(craftingPanel);
    public void ToggleQuest() => TogglePanelState(questPanel);

    public void CloseAllStandardPanels()
    {
        if ((dialoguePanel != null && dialoguePanel.activeSelf) ||
            (cutscenePanel != null && cutscenePanel.activeSelf)) return;

        bool closedSomething = false;

        if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.InventoryPanel != null && PlayerUIManager.Instance.InventoryPanel.activeSelf)
        {
            TogglePanelState(PlayerUIManager.Instance.InventoryPanel, false);
            closedSomething = true;
        }

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
    // MESSAGES & CHOICE POPUPS
    // ---------------------------
    public void ShowMessage(string msg)
    {
        if (messageText == null) return;
        messageText.text = msg;
    }

    public void ShowChoice(string prompt, string opt1Text, Action opt1Action, string opt2Text, Action opt2Action)
    {
        if (choicePanel == null || throwInOceanButton == null || cleanUpButton == null) return;

        if (choicePromptText != null) choicePromptText.text = prompt;
        if (throwInOceanText != null) throwInOceanText.text = opt1Text;
        if (cleanUpText != null) cleanUpText.text = opt2Text;

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

    public void HideChoicePanel() => TogglePanelState(choicePanel, false);

    private void CheckAndRestoreState()
    {
        if (!IsUIOpen())
        {
            GameManager.Instance?.SetState(GameState.Normal);
            PlayerController.Instance?.UnlockMovement();
        }
    }

    public void PlayPanelOpenSFX() => GameEvents.TriggerSound(SoundType.PanelOpen);
    public void PlayPanelCloseSFX() => GameEvents.TriggerSound(SoundType.PanelClose);

    public bool IsPointerOverUI() => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

    public bool IsUIOpen()
    {
        GameObject invPanel = PlayerUIManager.Instance != null ? PlayerUIManager.Instance.InventoryPanel : null;

        return (invPanel != null && invPanel.activeSelf) ||
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