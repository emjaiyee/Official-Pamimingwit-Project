using UnityEngine;
using UnityEngine.UI;

public class RippleBobUI : MonoBehaviour
{
    [Header("Ripple Settings (match shader)")]
    public RectTransform rippleCenter;
    public float rippleSpeed = 1f;
    public float rippleScale = 0.05f;
    public float rippleStrength = 10f;

    [Header("Shadow")]
    public RectTransform shadow;
    public float shadowDelay = 0.2f;
    public float shadowStrengthMultiplier = 0.4f;

    [Header("Extra Motion")]
    public bool useRotation = true;
    public float rotationAmount = 3f;

    public bool useScale = true;
    public float scaleAmount = 0.05f;

    private RectTransform rt;
    private Vector2 startPos;
    private Vector2 shadowStartPos;
    private Image shadowImage;

    void Start()
    {
        rt = GetComponent<RectTransform>();
        startPos = rt.anchoredPosition;

        if (shadow != null)
        {
            shadowStartPos = shadow.anchoredPosition;
            shadowImage = shadow.GetComponent<Image>();
        }
    }

    void Update()
    {
        if (rippleCenter == null) return;

        // Distance (UI space)
        float distance = Vector2.Distance(rt.anchoredPosition, rippleCenter.anchoredPosition);

        // MAIN WAVE
        float wave = Mathf.Sin(distance * rippleScale - Time.time * rippleSpeed);

        // 🌊 BUTTON MOVEMENT
        rt.anchoredPosition = startPos + Vector2.up * wave * rippleStrength;

        // Rotation
        if (useRotation)
        {
            float rot = wave * rotationAmount;
            rt.localRotation = Quaternion.Euler(0, 0, rot);
        }

        // Scale
        if (useScale)
        {
            float scale = 1 + wave * scaleAmount;
            rt.localScale = new Vector3(scale, scale, 1);
        }

        // 🌑 SHADOW (soft water style FIXED)
        if (shadow != null)
        {
            float shadowWave = Mathf.Sin(distance * rippleScale - (Time.time - shadowDelay) * rippleSpeed);

            // Slight movement only (not full bob)
            shadow.anchoredPosition = shadowStartPos + Vector2.up * shadowWave * (rippleStrength * 0.15f);

            // Soft elliptical scaling (top-down feel)
            float scale = 1f - shadowWave * 0.08f;
            shadow.localScale = new Vector3(scale * 1.2f, scale * 0.7f, 1f);

            // Fade alpha to simulate blur / water diffusion
            if (shadowImage != null)
            {
                float alpha = 0.25f + (1f - Mathf.Abs(shadowWave)) * 0.25f;

                Color c = shadowImage.color;
                c.a = alpha;
                shadowImage.color = c;
            }
        }
    }
}