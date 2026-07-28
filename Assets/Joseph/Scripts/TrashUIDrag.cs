using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(AudioSource))]
public class TrashUIDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
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
    public Image trashImage; // The main trash visual
    public RectTransform visualContent; // The part that "lifts" up
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

        // Randomize initial state for a natural "scattered" look
        restingRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
        baseScale = Random.Range(0.85f, 1.15f);
        
        // Keep the root (and shadow) upright; only the visual content gets the random rotation
        rectTransform.localRotation = Quaternion.identity;
        if (visualContent != null) visualContent.localRotation = restingRotation;

        rectTransform.localScale = Vector3.one * baseScale;
    }

    void Update()
    {
        // Apply tilt and rotation to the visual content only
        if (visualContent != null)
            visualContent.localRotation = Quaternion.Lerp(visualContent.localRotation, restingRotation, Time.deltaTime * lerpSpeed);
        
        // "Plop" effect: use a faster lerp when dropping (returning to scale 1)
        float currentLerp = (targetScale == 1f) ? lerpSpeed * 1.5f : lerpSpeed;
        rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, Vector3.one * (baseScale * targetScale), Time.deltaTime * currentLerp);

        // Simulated height logic
        if (shadowImage != null && visualContent != null)
        {
            // Use targetScale for the progress calculation to avoid jitter during lerps
            float liftProgress = Mathf.Clamp01((targetScale - 1f) / (dragScale - 1f));

            // Calculate height with a floating bobbing effect that only happens when lifted
            float liftOffset = liftProgress * maxLiftOffset;
            float floatBob = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude * liftProgress;

            visualContent.anchoredPosition = Vector2.up * (liftOffset + floatBob);
            shadowImage.color = new Color(shadowImage.color.r, shadowImage.color.g, shadowImage.color.b, Mathf.Lerp(1f, minShadowAlpha, liftProgress));
            shadowImage.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.5f, liftProgress);
        }

        // Detect the end of the "Plop" to play the landing sound
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
        // Make it semi-transparent and ignore raycasts so we can drop it "into" the bin
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
        targetScale = dragScale;
        isFalling = false;

        if (pickUpSFX != null) audioSource.PlayOneShot(pickUpSFX);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Move the trash following the mouse delta, adjusted for canvas scale
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;

        // Apply tilt based on movement velocity
        float tiltZ = Mathf.Clamp(-eventData.delta.x * swayAmount, -25f, 25f);
        float tiltX = Mathf.Clamp(eventData.delta.y * swayAmount, -15f, 15f);
        
        // Apply the tilt on top of the resting rotation
        if (visualContent != null) 
            visualContent.localRotation = restingRotation * Quaternion.Euler(tiltX, 0, tiltZ);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Reset transparency and raycasting
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        targetScale = 1f;
        isFalling = true;
    }
}