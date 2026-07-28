using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ShopSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image icon;
    public TextMeshProUGUI itemName;
    public TextMeshProUGUI priceText;
    
    private ItemData item;

    public void Setup(ItemData newItem)
    {
        item = newItem;
        if (item == null) return;

        if (icon != null) icon.sprite = item.icon;
        if (itemName != null) itemName.text = item.itemName;
        if (priceText != null) priceText.text = item.price.ToString();
    }

    public void OnBuyClicked()
    {
        if (item == null || UIManager.Instance == null) return;

        if (UIManager.Instance.currentShopType == UIManager.ActiveShopType.General && ShopManager.Instance != null)
        {
            ShopManager.Instance.BuyItem(item);
        }
        else if (UIManager.Instance.currentShopType == UIManager.ActiveShopType.Industrial && IndustrialShopManager.Instance != null)
        {
            IndustrialShopManager.Instance.BuyItem(item);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (item != null)
        {
            TooltipUI.Instance?.ShowTooltip(item.itemName, item.description);
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
}