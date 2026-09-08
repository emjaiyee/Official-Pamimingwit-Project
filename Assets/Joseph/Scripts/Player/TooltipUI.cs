using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.InputSystem;

public class TooltipUI : MonoBehaviour
{
    public static TooltipUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Canvas parentCanvas;

    [Header("Settings")]
    [SerializeField] private Vector2 offset = new Vector2(15f, -15f);
    [SerializeField] private float padding = 5f;
    [SerializeField] private float showDelay = 0.3f;
    [SerializeField] private float fadeSpeed = 12f;

    private Coroutine _delayCoroutine;
    private float _targetAlpha = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    private void Update()
    {
        if (tooltipPanel == null || canvasGroup == null) return;

        if (canvasGroup.alpha > 0f) 
            UpdatePosition();

        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, _targetAlpha, Time.deltaTime * fadeSpeed);
    }

    private void UpdatePosition()
    {
        Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;

        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                mousePos,
                parentCanvas.worldCamera,
                out Vector2 localPoint
            );
            rectTransform.anchoredPosition = ClampToCanvas(localPoint + offset);
        }
        else
        {
            Vector2 targetPos = mousePos + offset;
            float width = rectTransform.rect.width * canvasGroup.transform.lossyScale.x;
            float height = rectTransform.rect.height * canvasGroup.transform.lossyScale.y;

            if (targetPos.x + width > Screen.width - padding)
                targetPos.x = mousePos.x - width - offset.x;

            if (targetPos.y - height < padding)
                targetPos.y = mousePos.y + height + Mathf.Abs(offset.y);

            if (targetPos.y > Screen.height - padding)
                targetPos.y = Screen.height - padding;

            transform.position = targetPos;
        }
    }

    private Vector2 ClampToCanvas(Vector2 pos)
    {
        RectTransform canvasRect = parentCanvas.transform as RectTransform;
        if (canvasRect == null) return pos;

        Vector2 minPosition = canvasRect.rect.min + new Vector2(padding, padding);
        Vector2 maxPosition = canvasRect.rect.max - new Vector2(padding, padding) - rectTransform.rect.size;

        pos.x = Mathf.Clamp(pos.x, minPosition.x, maxPosition.x);
        pos.y = Mathf.Clamp(pos.y, minPosition.y, maxPosition.y);

        return pos;
    }

    public void ShowTooltip(string itemName, string description)
    {
        if (tooltipPanel == null || canvasGroup == null) return;

        if (_delayCoroutine != null) StopCoroutine(_delayCoroutine);
        _delayCoroutine = StartCoroutine(ShowAfterDelay(itemName, description));
    }

    private IEnumerator ShowAfterDelay(string itemName, string description)
    {
        yield return new WaitForSeconds(showDelay);

        if (itemNameText != null) itemNameText.text = itemName;
        if (itemDescriptionText != null) itemDescriptionText.text = description;

        // Force rebuild BEFORE updating position so Rect dimensions are accurate
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        
        UpdatePosition();

        _targetAlpha = 1f;
        canvasGroup.blocksRaycasts = false; // Prevent tooltip from blocking raycasts under the cursor
    }

    public void HideTooltip()
    {
        if (_delayCoroutine != null) StopCoroutine(_delayCoroutine);

        _targetAlpha = 0f;
        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
    }
}