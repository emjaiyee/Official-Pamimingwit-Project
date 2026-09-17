using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ShopSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image icon;
    public TextMeshProUGUI itemName;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI stockText;
    
    private ItemData item;

    private void Awake()
    {
        if (stockText == null)
        {
            foreach (TextMeshProUGUI text in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (text != null && text.gameObject.name == "QuantityText")
                {
                    stockText = text;
                    break;
                }
            }
        }
    }

    public void Setup(ItemData newItem, int price, int stock)
    {
        item = newItem;
        if (item == null) return;

        if (icon != null) icon.sprite = item.icon;

        if (itemName != null)
        {
            itemName.text = stockText != null ? item.itemName : $"{item.itemName} ({stock})";
        }

        if (stockText != null)
        {
            stockText.text = stock.ToString();
            stockText.gameObject.SetActive(true);
        }

        if (priceText != null) priceText.text = price.ToString();
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