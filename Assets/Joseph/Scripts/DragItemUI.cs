using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class DragItemUI : MonoBehaviour
{
    public static DragItemUI Instance;
    private RectTransform rectTransform; // Reference to this object's RectTransform
    private Canvas canvas; // Reference to the parent Canvas
    public Image icon;
    private AudioSource audioSource;

    [Header("Sway Settings")]
    public float swayAmount = 0.5f;
    public float lerpSpeed = 10f;
    public float dragScale = 1.2f;

    [Header("Float Animation")]
    public float floatAmplitude = 2f; // How much the item bobs up and down
    public float floatSpeed = 3f;     // How fast the item bobs

    [Header("Audio")]
    public AudioClip pickUpSFX;
    public AudioClip dropSFX;

    private Vector2 lastMousePos;
    private float currentBaseScale = 1f;
    void Awake()
    {
        Instance = this;
        rectTransform = GetComponent<RectTransform>(); // Get this object's RectTransform
        canvas = GetComponentInParent<Canvas>(); // Get the parent Canvas
        audioSource = GetComponent<AudioSource>(); // Get the AudioSource
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>(); // Ensure AudioSource exists
        if (icon != null) icon.enabled = false;
        if (icon != null) icon.rectTransform.anchoredPosition = Vector2.zero; // Ensure icon starts centered relative to its parent
        // Crucial: Make the icon not block raycasts so OnDrop can reach slots underneath
        if (icon != null) icon.raycastTarget = false;
    }

    void Update()
    {
        if (icon != null && icon.enabled)
        {
                if (Mouse.current != null && canvas != null)
                {
                    Vector2 currentMousePos = Mouse.current.position.ReadValue();

                    // Determine the correct camera: Null for Overlay, worldCamera for others
                    Camera uiCamera = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;

                    // Absolutely map the mouse position to the UI local space
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        (RectTransform)rectTransform.parent,
                        currentMousePos,
                        uiCamera,
                        out Vector2 localPoint))
                    {
                        rectTransform.anchoredPosition = localPoint;
                    }

                    // Calculate mouse velocity for the tilt effect
                    float deltaX = currentMousePos.x - lastMousePos.x;
                    float deltaY = currentMousePos.y - lastMousePos.y;

                    float targetRotZ = Mathf.Clamp(-deltaX * swayAmount, -25f, 25f);
                    float targetRotX = Mathf.Clamp(deltaY * swayAmount, -15f, 15f);
                    
                    icon.transform.localRotation = Quaternion.Lerp(icon.transform.localRotation, Quaternion.Euler(targetRotX, 0, targetRotZ), Time.deltaTime * lerpSpeed);
                    
                    // Smoothly scale up while dragging
                    icon.transform.localScale = Vector3.Lerp(icon.transform.localScale, Vector3.one * (currentBaseScale * dragScale), Time.deltaTime * lerpSpeed);
                    
                    // Apply subtle float animation
                    float bob = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
                    icon.rectTransform.anchoredPosition = new Vector2(0, bob); // This is relative to DragItemUI's position

                    lastMousePos = currentMousePos;
                }
        }
    }

    public void StartDrag(ItemData item, Color tint, float baseScale)
    {
        if (item == null || icon == null) return;

        if (Mouse.current != null)
            lastMousePos = Mouse.current.position.ReadValue();

        icon.sprite = item.icon;
        icon.enabled = true;
        icon.color = tint;
        currentBaseScale = baseScale;
        icon.rectTransform.anchoredPosition = Vector2.zero; // Reset bobbing position

        if (pickUpSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(pickUpSFX);
        }
    }

    public void StopDrag()
    {
        if (icon != null)
        {
            icon.enabled = false;
            icon.transform.localRotation = Quaternion.identity; // Reset rotation for next drag
            icon.transform.localScale = Vector3.one;
            icon.color = Color.white;
            icon.rectTransform.anchoredPosition = Vector2.zero; // Reset bobbing position
        }

        if (dropSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(dropSFX);
        }
    }
}