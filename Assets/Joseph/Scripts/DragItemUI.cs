using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class DragItemUI : MonoBehaviour
{
    public static DragItemUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private Image icon;
    private RectTransform rectTransform;
    private Canvas canvas;
    private AudioSource audioSource;

    [Header("Sway Settings")]
    [SerializeField] private float swayAmount = 0.5f;
    [SerializeField] private float lerpSpeed = 12f;
    [SerializeField] private float dragScale = 1.2f;

    [Header("Float Animation")]
    [SerializeField] private float floatAmplitude = 2f;
    [SerializeField] private float floatSpeed = 3f;

    [Header("Audio")]
    [SerializeField] private AudioClip pickUpSFX;
    [SerializeField] private AudioClip dropSFX;

    private Vector2 lastMousePos;
    private Vector2 currentVelocity;
    private float currentBaseScale = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null) 
            audioSource = gameObject.AddComponent<AudioSource>();

        if (icon != null)
        {
            icon.enabled = false;
            icon.raycastTarget = false;
            icon.rectTransform.anchoredPosition = Vector2.zero;
        }
    }

    private void Update()
    {
        if (icon == null || !icon.enabled || canvas == null || Mouse.current == null) 
            return;

        Vector2 currentMousePos = Mouse.current.position.ReadValue();

        // 1. Follow Cursor Space
        Camera uiCamera = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)rectTransform.parent,
            currentMousePos,
            uiCamera,
            out Vector2 localPoint))
        {
            rectTransform.anchoredPosition = localPoint;
        }

        // 2. Frame-rate Independent Sway Calculation
        Vector2 rawDelta = currentMousePos - lastMousePos;
        currentVelocity = Vector2.Lerp(currentVelocity, rawDelta, 1f - Mathf.Exp(-lerpSpeed * Time.deltaTime));

        float targetRotZ = Mathf.Clamp(-currentVelocity.x * swayAmount, -25f, 25f);
        float targetRotX = Mathf.Clamp(currentVelocity.y * swayAmount, -15f, 15f);

        float blendFactor = 1f - Mathf.Exp(-lerpSpeed * Time.deltaTime);

        icon.transform.localRotation = Quaternion.Lerp(
            icon.transform.localRotation, 
            Quaternion.Euler(targetRotX, 0f, targetRotZ), 
            blendFactor
        );

        // 3. Smooth Scaling
        icon.transform.localScale = Vector3.Lerp(
            icon.transform.localScale, 
            Vector3.one * (currentBaseScale * dragScale), 
            blendFactor
        );

        // 4. Bobbing Oscillation
        float bob = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        icon.rectTransform.anchoredPosition = new Vector2(0f, bob);

        lastMousePos = currentMousePos;
    }

    public void StartDrag(ItemData item, Color tint, float baseScale)
    {
        if (item == null || icon == null) return;

        if (Mouse.current != null)
        {
            lastMousePos = Mouse.current.position.ReadValue();
            currentVelocity = Vector2.zero;
        }

        icon.sprite = item.icon;
        icon.enabled = true;
        icon.color = tint;
        currentBaseScale = baseScale;
        icon.rectTransform.anchoredPosition = Vector2.zero;

        PlaySFX(pickUpSFX);
    }

    public void StopDrag()
    {
        if (icon != null)
        {
            icon.enabled = false;
            icon.transform.localRotation = Quaternion.identity;
            icon.transform.localScale = Vector3.one;
            icon.color = Color.white;
            icon.rectTransform.anchoredPosition = Vector2.zero;
        }

        PlaySFX(dropSFX);
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && audioSource != null && audioSource.isActiveAndEnabled)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}