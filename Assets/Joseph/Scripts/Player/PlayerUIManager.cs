using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUIManager : MonoBehaviour
{
    public static PlayerUIManager Instance { get; private set; }

    [Header("Currency UI")]
    [SerializeField] private TextMeshProUGUI selyoText;

    [Header("Stamina UI")]
    [SerializeField] private GameObject staminaPanel;
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private Image staminaFillImage;
    [SerializeField] private TextMeshProUGUI staminaText;
    [SerializeField] private float lowStaminaThreshold = 0.2f;
    [SerializeField] private float pulseSpeed = 4f;
    [SerializeField] private float pulseScaleAmount = 0.15f;
    [SerializeField] private float staminaSmoothSpeed = 10f;

    [Header("Inventory & Hotbar UI")]
    [SerializeField] private ItemSlotUI[] inventorySlots;
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject hotbarPanel;
    public ItemSlotUI[] hotbarSlots;

    private Coroutine pulseCoroutine;
    private float targetStamina;
    private float targetMaxStamina;
    private float visualStamina;
    private bool initializedStamina;

    public GameObject InventoryPanel => inventoryPanel;

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
        if (staminaSlider != null) staminaSlider.interactable = false;
        if (hotbarPanel != null) hotbarPanel.SetActive(true);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);

        // Bind Player Data Listeners
        if (PlayerWallet.Instance != null)
        {
            PlayerWallet.Instance.OnCoinsChanged.AddListener(UpdateCoins);
            UpdateCoins(PlayerWallet.Instance.coins);
        }

        if (StaminaManager.Instance != null)
        {
            StaminaManager.Instance.OnStaminaChanged += UpdateStaminaUI;
        }

        if (Inventory.Instance != null)
        {
            Inventory.Instance.OnInventoryChanged += RefreshInventory;
        }

        RefreshInventory();
    }

    private void OnDestroy()
    {
        if (PlayerWallet.Instance != null)
        {
            PlayerWallet.Instance.OnCoinsChanged.RemoveListener(UpdateCoins);
        }

        if (StaminaManager.Instance != null)
        {
            StaminaManager.Instance.OnStaminaChanged -= UpdateStaminaUI;
        }

        if (Inventory.Instance != null)
        {
            Inventory.Instance.OnInventoryChanged -= RefreshInventory;
        }
    }

    private void Update()
    {
        SmoothStaminaBar();
    }

    // ---------------------------
    // WALLET / CURRENCY
    // ---------------------------
    public void UpdateCoins(int coins)
    {
        if (selyoText != null)
        {
            selyoText.text = $"Selyo: {coins}";
        }
    }

    // ---------------------------
    // STAMINA SYSTEM
    // ---------------------------
    public void UpdateStaminaUI(float currentStamina, float maxStamina)
    {
        targetStamina = currentStamina;
        targetMaxStamina = maxStamina;

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

    private void SmoothStaminaBar()
    {
        if (staminaSlider == null || targetMaxStamina <= 0) return;

        visualStamina = Mathf.Lerp(visualStamina, targetStamina, Time.deltaTime * staminaSmoothSpeed);
        if (Mathf.Abs(visualStamina - targetStamina) < 0.01f) visualStamina = targetStamina;

        staminaSlider.maxValue = targetMaxStamina;
        staminaSlider.value = visualStamina;

        if (staminaText != null)
            staminaText.text = $"{Mathf.CeilToInt(visualStamina)} / {Mathf.CeilToInt(targetMaxStamina)}";

        if (staminaFillImage != null)
        {
            float percent = visualStamina / targetMaxStamina;
            staminaFillImage.color = Color.Lerp(Color.red, Color.green, percent);
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

    // ---------------------------
    // INVENTORY / HOTBAR
    // ---------------------------
    public void RefreshInventory()
    {
        if (inventorySlots != null)
        {
            for (int i = 0; i < inventorySlots.Length; i++)
            {
                if (inventorySlots[i] != null) inventorySlots[i].Refresh();
            }
        }

        if (hotbarSlots != null)
        {
            foreach (var slot in hotbarSlots)
            {
                if (slot != null) slot.Refresh();
            }
        }
    }

    public void ToggleInventory()
    {
        if (inventoryPanel != null && UIManager.Instance != null)
        {
            UIManager.Instance.TogglePanelState(inventoryPanel);
        }
    }
}