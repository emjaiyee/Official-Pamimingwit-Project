using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(AudioSource))]
public class TrashUIDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public bool isRecyclable; // Category Flag

    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private AudioSource audioSource;

    [Header("Sway Settings")]
    public float swayAmount = 3f;
    public float lerpSpeed = 12f;
    public float dragScale = 1.15f;

    [Header("Shadow & Lift Settings")]
    public Image shadowImage;
    public Image trashImage; 
    public RectTransform visualContent; 
    public float maxLiftOffset = 25f;
    public float minShadowAlpha = 0f;

    [Header("Float Animation")]
    public float floatAmplitude = 5f;
    public float floatSpeed = 4f;

    [Header("Audio")]
    public AudioClip pickUpSFX;
    public AudioClip landSFX;

    private float targetScale = 1f;
    private Quaternion restingRotation;
    private float baseScale;
    private bool isFalling = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        audioSource = GetComponent<AudioSource>();
        canvas = GetComponentInParent<Canvas>();

        restingRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
        baseScale = Random.Range(0.85f, 1.15f);
        
        rectTransform.localRotation = Quaternion.identity;
        if (visualContent != null) visualContent.localRotation = restingRotation;

        rectTransform.localScale = Vector3.one * baseScale;
    }

    void Update()
    {
        if (visualContent != null)
            visualContent.localRotation = Quaternion.Lerp(visualContent.localRotation, restingRotation, Time.deltaTime * lerpSpeed);
        
        float currentLerp = (targetScale == 1f) ? lerpSpeed * 1.5f : lerpSpeed;
        rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, Vector3.one * (baseScale * targetScale), Time.deltaTime * currentLerp);

        if (shadowImage != null && visualContent != null)
        {
            float liftProgress = Mathf.Clamp01((targetScale - 1f) / (dragScale - 1f));
            float liftOffset = liftProgress * maxLiftOffset;
            float floatBob = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude * liftProgress;

            visualContent.anchoredPosition = Vector2.up * (liftOffset + floatBob);
            shadowImage.color = new Color(shadowImage.color.r, shadowImage.color.g, shadowImage.color.b, Mathf.Lerp(1f, minShadowAlpha, liftProgress));
            shadowImage.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.5f, liftProgress);
        }

        if (isFalling && targetScale == 1f)
        {
            if (rectTransform.localScale.x <= (baseScale * 1.02f))
            {
                if (landSFX != null) audioSource.PlayOneShot(landSFX);
                isFalling = false;
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
        targetScale = dragScale;
        isFalling = false;

        if (pickUpSFX != null) audioSource.PlayOneShot(pickUpSFX);
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;

        float tiltZ = Mathf.Clamp(-eventData.delta.x * swayAmount, -25f, 25f);
        float tiltX = Mathf.Clamp(eventData.delta.y * swayAmount, -15f, 15f);
        
        if (visualContent != null) 
            visualContent.localRotation = restingRotation * Quaternion.Euler(tiltX, 0, tiltZ);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        targetScale = 1f;
        isFalling = true;
    }
}