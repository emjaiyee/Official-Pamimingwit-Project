using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.InputSystem;

public class TooltipUI : MonoBehaviour
{
    public static TooltipUI Instance;

    [Header("UI References")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Settings")]
    [SerializeField] private Vector2 offset = new Vector2(15f, -15f);
    [SerializeField] private float padding = 5f;
    [SerializeField] private float showDelay = 0.5f;
    [SerializeField] private float fadeSpeed = 10f;

    private Coroutine _delayCoroutine;
    private float _targetAlpha = 0f;

    void Awake()
    {
        Instance = this;
        
        // Ensure the GameObject stays active so the script can run, 
        // but hide the visuals immediately.
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    void Update()
    {
        if (tooltipPanel == null || canvasGroup == null) return;

        // Follow the mouse position whenever the panel is active
        // We check alpha to know if it's "effectively" active
        if (canvasGroup.alpha > 0f) UpdatePosition();

        // Smoothly interpolate the alpha towards the target
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, _targetAlpha, Time.deltaTime * fadeSpeed);
    }

    private void UpdatePosition()
    {
        if (Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 targetPos = mousePos + offset;

        float width = rectTransform.rect.width * canvasGroup.transform.lossyScale.x;
        float height = rectTransform.rect.height * canvasGroup.transform.lossyScale.y;

        // Flip to the left if the tooltip would go off the right edge of the screen
        if (targetPos.x + width > Screen.width - padding)
        {
            targetPos.x = mousePos.x - width - offset.x;
        }

        // Flip upwards if the tooltip would go off the bottom edge of the screen
        if (targetPos.y - height < padding)
        {
            targetPos.y = mousePos.y + height + Mathf.Abs(offset.y);
        }

        if (targetPos.y > Screen.height - padding)
        {
            targetPos.y = Screen.height - padding;
        }


        transform.position = targetPos;
    }

    public void ShowTooltip(string itemName, string description)
    {
        if (tooltipPanel == null) return;
        if (canvasGroup == null) return;

        // Cancel any existing delay if the user moves between items quickly
        if (_delayCoroutine != null) StopCoroutine(_delayCoroutine);
        _delayCoroutine = StartCoroutine(ShowAfterDelay(itemName, description));
    }

    private IEnumerator ShowAfterDelay(string itemName, string description)
    {
        yield return new WaitForSeconds(showDelay);

        itemNameText.text = itemName;
        itemDescriptionText.text = description;
        
        // Update position immediately before showing to prevent 1-frame flickering from the old position
        UpdatePosition();
        
        _targetAlpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // Force the layout system to recalculate height based on the new text immediately
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }

    public void HideTooltip()
    {
        if (_delayCoroutine != null) StopCoroutine(_delayCoroutine);
        
        // Set the target alpha to 0 to trigger the fade-out in Update
        _targetAlpha = 0f;
        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
    }
}