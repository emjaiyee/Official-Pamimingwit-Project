using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class RippleBobUI : MonoBehaviour
{
    [Header("Ripple Settings (match shader)")]
    [SerializeField] private RectTransform rippleCenter;
    [SerializeField] private float rippleSpeed = 1f;
    [SerializeField] private float rippleScale = 0.05f;
    [SerializeField] private float rippleStrength = 10f;

    [Header("Shadow")]
    [SerializeField] private RectTransform shadow;
    [SerializeField] private float shadowDelay = 0.2f;
    [SerializeField] private float shadowStrengthMultiplier = 0.4f;

    [Header("Extra Motion")]
    [SerializeField] private bool useRotation = true;
    [SerializeField] private float rotationAmount = 3f;

    [SerializeField] private bool useScale = true;
    [SerializeField] private float scaleAmount = 0.05f;

    private RectTransform rt;
    private Vector2 startPos;
    private Vector2 shadowStartPos;
    private Image shadowImage;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        startPos = rt.anchoredPosition;

        if (shadow != null)
        {
            shadowStartPos = shadow.anchoredPosition;
            shadowImage = shadow.GetComponent<Image>();
        }
    }

    private void OnEnable()
    {
        // Re-anchor start position when object reactivates to prevent canvas shifts
        startPos = rt.anchoredPosition;
        if (shadow != null)
        {
            shadowStartPos = shadow.anchoredPosition;
        }
    }

    private void Update()
    {
        if (rippleCenter == null) return;

        // Sample distance using immutable startPos to prevent positional drift
        float distance = Vector2.Distance(startPos, rippleCenter.anchoredPosition);
        float time = Time.time;

        // Main Wave Waveform Calculation
        float wave = Mathf.Sin(distance * rippleScale - time * rippleSpeed);

        // Position Updates
        rt.anchoredPosition = startPos + Vector2.up * (wave * rippleStrength);

        // Rotation Matrix Updates
        if (useRotation)
        {
            float rot = wave * rotationAmount;
            rt.localRotation = Quaternion.Euler(0f, 0f, rot);
        }

        // Scale Factor Updates
        if (useScale)
        {
            float scale = 1f + wave * scaleAmount;
            rt.localScale = new Vector3(scale, scale, 1f);
        }

        // Shadow Processing Loop
        if (shadow != null)
        {
            float shadowWave = Mathf.Sin(distance * rippleScale - (time - shadowDelay) * rippleSpeed);

            shadow.anchoredPosition = shadowStartPos + Vector2.up * (shadowWave * (rippleStrength * 0.15f * shadowStrengthMultiplier));

            float shadowScale = 1f - shadowWave * 0.08f;
            shadow.localScale = new Vector3(shadowScale * 1.2f, shadowScale * 0.7f, 1f);

            if (shadowImage != null)
            {
                float alpha = 0.25f + (1f - Mathf.Abs(shadowWave)) * 0.25f;
                Color color = shadowImage.color;
                color.a = alpha;
                shadowImage.color = color;
            }
        }
    }
}