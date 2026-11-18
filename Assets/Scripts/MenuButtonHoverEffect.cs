using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button), typeof(RectTransform))]
public class MenuButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField, Min(1f)] private float hoverScale = 1.06f;
    [SerializeField] private float hoverRotation = 2.5f;
    [SerializeField, Min(0f)] private float hoverShiftLeft = 12f;
    [SerializeField, Min(0.01f)] private float transitionSpeed = 14f;
    [SerializeField] private Graphic shadowGraphic;
    [SerializeField, Range(0f, 1f)] private float hoverShadowAlpha = 0.65f;

    private RectTransform rectTransform;
    private Vector2 restingPosition;
    private Vector3 restingScale;
    private Quaternion restingRotation;
    private float restingShadowAlpha;
    private bool isPointerOver;
    private bool isSelected;

    private bool IsHighlighted => isPointerOver || isSelected;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        CacheRestingTransform();
        CacheRestingShadowAlpha();
    }

    private void OnEnable()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        CacheRestingTransform();
        CacheRestingShadowAlpha();
    }

    private void OnDisable()
    {
        isPointerOver = false;
        isSelected = false;

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = restingPosition;
            rectTransform.localScale = restingScale;
            rectTransform.localRotation = restingRotation;
        }

        SetShadowAlpha(restingShadowAlpha);
    }

    private void Update()
    {
        float blend = 1f - Mathf.Exp(-transitionSpeed * Time.unscaledDeltaTime);
        Vector2 targetPosition = restingPosition +
            (IsHighlighted ? Vector2.left * hoverShiftLeft : Vector2.zero);
        float targetScale = IsHighlighted ? hoverScale : 1f;
        float targetRotation = IsHighlighted ? hoverRotation : 0f;

        rectTransform.anchoredPosition = Vector2.Lerp(
            rectTransform.anchoredPosition,
            targetPosition,
            blend);
        rectTransform.localScale = Vector3.Lerp(
            rectTransform.localScale,
            restingScale * targetScale,
            blend);
        rectTransform.localRotation = Quaternion.Slerp(
            rectTransform.localRotation,
            restingRotation * Quaternion.Euler(0f, 0f, targetRotation),
            blend);

        float targetShadowAlpha = restingShadowAlpha * (IsHighlighted ? hoverShadowAlpha : 1f);
        float currentShadowAlpha = shadowGraphic != null ? shadowGraphic.color.a : 0f;
        SetShadowAlpha(Mathf.Lerp(currentShadowAlpha, targetShadowAlpha, blend));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
    }

    private void CacheRestingTransform()
    {
        restingPosition = rectTransform.anchoredPosition;
        restingScale = rectTransform.localScale;
        restingRotation = rectTransform.localRotation;
    }

    private void CacheRestingShadowAlpha()
    {
        if (shadowGraphic != null)
        {
            restingShadowAlpha = shadowGraphic.color.a;
        }
    }

    private void SetShadowAlpha(float alpha)
    {
        if (shadowGraphic == null)
        {
            return;
        }

        Color color = shadowGraphic.color;
        color.a = alpha;
        shadowGraphic.color = color;
    }
}
