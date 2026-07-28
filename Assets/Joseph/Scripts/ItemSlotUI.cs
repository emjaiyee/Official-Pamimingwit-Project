using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

public enum SlotType { Inventory, Hotbar, CraftingGrid, CraftingResult }

public class ItemSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Configuration")]
    public SlotType type;
    public int index;

    [Header("UI References")]
    public Image icon;
    public TextMeshProUGUI amountText;
    public GameObject highlight;

    [Header("Juice Settings")]
    public float selectionScaleMult = 1.2f;
    public float selectionYOffset = 10f;
    public float lerpSpeed = 15f;

    [Header("Juice - Pop")]
    public float popDuration = 0.2f;
    public float popScaleAmount = 1.25f;

    [Header("Quality Tint")]
    public Color bronzeTint = new Color(0.82f, 0.41f, 0.12f);
    public Color silverTint = new Color(0.75f, 0.75f, 0.75f);
    public Color goldTint = new Color(1f, 0.84f, 0f);

    private InventoryItem currentItem;
    private float baseScale = 1f;
    private float visualPopScale = 1f;
    private bool isInitialized = false;
    private ItemData lastItem;
    private int lastAmount;
    private Coroutine popRoutine;

    public void SetItem(InventoryItem newItem)
    {
        currentItem = newItem;
        Refresh();
    }

    private void Update()
    {
        if (icon == null || !icon.enabled) return;

        // Only apply selection juice to Hotbar slots
        bool isSelected = (type == SlotType.Hotbar && HotbarManager.Instance != null && HotbarManager.Instance.selectedIndex == index);
        
        // Calculate target scale (combining quality scaling and selection pop)
        float targetScaleValue = baseScale * (isSelected ? selectionScaleMult : 1f) * visualPopScale;
        Vector3 targetScale = new Vector3(targetScaleValue, targetScaleValue, 1f);
        
        // Calculate target vertical offset
        float targetY = isSelected ? selectionYOffset : 0f;
        Vector2 targetPos = new Vector2(0, targetY);

        // Smoothly transition
        icon.transform.localScale = Vector3.Lerp(icon.transform.localScale, targetScale, Time.deltaTime * lerpSpeed);
        icon.rectTransform.anchoredPosition = Vector2.Lerp(icon.rectTransform.anchoredPosition, targetPos, Time.deltaTime * lerpSpeed);
    }

    public void Refresh()
    {
        // Logic for Hotbar/Inventory index-based lookup
        if (type == SlotType.Inventory || type == SlotType.Hotbar)
        {
            if (Inventory.Instance != null && index < Inventory.Instance.itemList.Count)
                currentItem = Inventory.Instance.itemList[index];
            else
                currentItem = null;
        }

        // Detect if a new item appeared or quantity increased
        if (isInitialized && currentItem?.item != null && gameObject.activeInHierarchy)
        {
            if (currentItem.item != lastItem || currentItem.amount > lastAmount)
            {
                if (popRoutine != null) StopCoroutine(popRoutine);
                popRoutine = StartCoroutine(PopInRoutine());
            }
        }
        lastItem = currentItem?.item;
        lastAmount = currentItem != null ? currentItem.amount : 0;
        isInitialized = true;

        // Visual Update
        if (currentItem == null || currentItem.item == null)
        {
            if (icon != null) 
            {
                icon.enabled = false;
                icon.color = Color.white;
                // Reset to default immediately when item is cleared
                baseScale = 1f;
                icon.rectTransform.anchoredPosition = Vector2.zero;
            }
            if (amountText != null) amountText.text = "";
        }
        else
        {
            if (icon != null)
            {
                icon.sprite = currentItem.item.icon;
                icon.enabled = true;

                // Apply tint based on quality for fish
                if (currentItem.item is FishData)
                {
                    icon.color = currentItem.quality switch
                    {
                        FishQuality.Bronze => bronzeTint,
                        FishQuality.Silver => silverTint,
                        FishQuality.Gold => goldTint,
                        _ => Color.white
                    };
                }
                else
                {
                    icon.color = Color.white;
                }

                // Apply wacky scaling based on quality for fish
                if (currentItem.item is FishData fish)
                {
                    float scaleFactor = 1f;
                    switch (currentItem.quality)
                    {
                        case FishQuality.Bronze: scaleFactor = fish.bronzeScale; break;
                        case FishQuality.Silver: scaleFactor = fish.silverScale; break;
                        case FishQuality.Gold:   scaleFactor = fish.goldScale; break;
                        default: scaleFactor = 1.0f; break;
                    }
                    baseScale = scaleFactor;
                }
                else
                {
                    baseScale = 1.0f;
                }
            }
            if (amountText != null)
            {
                amountText.text = currentItem.amount > 1 ? currentItem.amount.ToString() : "";
            }
        }

        UpdateHighlight();
    }

    public void AnimatePopOut(System.Action onComplete)
    {
        if (!gameObject.activeInHierarchy)
        {
            onComplete?.Invoke();
            return;
        }

        if (popRoutine != null) StopCoroutine(popRoutine);
        popRoutine = StartCoroutine(PopOutRoutine(onComplete));
    }

    private IEnumerator PopInRoutine()
    {
        float t = 0;
        while (t < popDuration)
        {
            t += Time.deltaTime;
            float p = t / popDuration;
            // Elastic overshoot: goes from 0 to popScaleAmount then settles to 1
            if (p < 0.7f) visualPopScale = Mathf.Lerp(0, popScaleAmount, p / 0.7f);
            else visualPopScale = Mathf.Lerp(popScaleAmount, 1f, (p - 0.7f) / 0.3f);
            yield return null;
        }
        visualPopScale = 1f;
    }

    private IEnumerator PopOutRoutine(System.Action onComplete)
    {
        float t = 0;
        while (t < popDuration * 0.75f)
        {
            t += Time.deltaTime;
            visualPopScale = Mathf.Lerp(1f, 0f, t / (popDuration * 0.75f));
            yield return null;
        }
        visualPopScale = 1f;
        onComplete?.Invoke();
    }

    private void UpdateHighlight()
    {
        if (highlight == null || HotbarManager.Instance == null) return;
        if (type == SlotType.Hotbar)
        {
            highlight.SetActive(HotbarManager.Instance.selectedIndex == index);
        }
        else
        {
            highlight.SetActive(false);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (type == SlotType.Inventory || type == SlotType.Hotbar)
        {
            // If shop is open, clicking an item sells it
            if (UIManager.Instance != null && UIManager.Instance.currentShopType != UIManager.ActiveShopType.None)
            {
                if (UIManager.Instance.currentShopType == UIManager.ActiveShopType.General)
                {
                    ShopManager.Instance?.SellItem(this);
                }
                else if (UIManager.Instance.currentShopType == UIManager.ActiveShopType.Industrial)
                {
                    IndustrialShopManager.Instance?.SellItem(this);
                }
            }
            else
            {
                HotbarManager.Instance?.SelectSlot(index);
            }
        }
        else if (type == SlotType.CraftingResult)
        {
            if (UIManager.Instance != null && UIManager.Instance.resultSlot != null)
            {
                UIManager.Instance.resultSlot.CraftItem();
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentItem != null && currentItem.item != null)
        {
            TooltipUI.Instance?.ShowTooltip(currentItem.item.itemName, currentItem.item.description);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipUI.Instance?.HideTooltip();
    }

    private void OnDisable()
    {
        TooltipUI.Instance?.HideTooltip();
    }

    public InventoryItem GetItem() => currentItem;

    public float GetBaseScale() => baseScale;
}