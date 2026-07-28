using UnityEngine;
using UnityEngine.UI; // Required for the Slider component

/// <summary>
/// Updates a UI Slider to reflect the current sustainability value from the SustainabilityManager.
/// </summary>
public class SustainabilityUIUpdater : MonoBehaviour
{
    [Tooltip("Reference to the UI Slider that will display the sustainability.")]
    [SerializeField] private Slider sustainabilitySlider;

    [Tooltip("How fast the pointer moves towards the target value. Higher values are faster.")]
    [SerializeField] private float lerpSpeed = 5f;

    [Header("Juice & Visuals")]
    [Tooltip("The image that represents the fill color.")]
    [SerializeField] private Image fillImage;
    [Tooltip("The pointer/arrow that follows the current value.")]
    [SerializeField] private RectTransform pointer;
    [Tooltip("How much the pointer pulses when the value changes.")]
    [SerializeField] private float pulseAmount = 1.2f;

    [SerializeField] private Color positiveColor = Color.green;
    [SerializeField] private Color negativeColor = Color.red;

    // Cache for the fill RectTransform
    private RectTransform fillRect;

    private bool _isSubscribed = false;
    private float _targetValue;
    private Vector3 _originalPointerScale;

    void Awake()
    {
        // If the slider isn't assigned in the Inspector, try to get it from this GameObject.
        if (sustainabilitySlider == null)
        {
            sustainabilitySlider = GetComponent<Slider>();
            if (sustainabilitySlider == null)
            {
                Debug.LogError("SustainabilityUIUpdater: No Slider component found on this GameObject or assigned in Inspector. Disabling script.");
                enabled = false; // Disable the script if no slider is found.
                return;
            }
        }

        if (fillImage != null)
            fillRect = fillImage.GetComponent<RectTransform>();

        if (pointer != null)
            _originalPointerScale = pointer.localScale;
    }

    void Start()
    {
        // If OnEnable missed the Instance due to timing, Start will catch it.
        if (!_isSubscribed)
            TrySubscribe();

        // If it's still not found by Start, the manager might actually be missing from the scene.
        if (!_isSubscribed)
            Debug.LogError("SustainabilityUIUpdater: SustainabilityManager.Instance not found. Ensure it exists in your scene.");
    }

    void Update()
    {
        if (sustainabilitySlider != null)
        {
            float previousValue = sustainabilitySlider.value;
            // Smoothly interpolate the slider value towards the target value over time
            sustainabilitySlider.value = Mathf.Lerp(sustainabilitySlider.value, _targetValue, Time.deltaTime * lerpSpeed);

            UpdateVisuals(sustainabilitySlider.value);

            // Add a little pulse to the pointer if it's moving significantly
            if (pointer != null && Mathf.Abs(sustainabilitySlider.value - previousValue) > 0.01f)
            {
                pointer.localScale = Vector3.Lerp(pointer.localScale, _originalPointerScale * pulseAmount, Time.deltaTime * lerpSpeed);
            }
            else if (pointer != null)
            {
                pointer.localScale = Vector3.Lerp(pointer.localScale, _originalPointerScale, Time.deltaTime * lerpSpeed);
            }
        }
    }

    private void UpdateVisuals(float value)
    {
        if (fillImage != null && fillRect != null)
        {
            // Switch color based on positive/negative
            fillImage.color = value >= 0 ? positiveColor : negativeColor;

            // Calculate bidirectional fill (assumes 0 is neutral)
            float normalizedFill = Mathf.Abs(value) / (value >= 0 ? sustainabilitySlider.maxValue : Mathf.Abs(sustainabilitySlider.minValue));
            
            if (value >= 0)
            {
                fillRect.anchorMin = new Vector2(0.5f, 0);
                fillRect.anchorMax = new Vector2(0.5f + (normalizedFill * 0.5f), 1);
            }
            else
            {
                fillRect.anchorMin = new Vector2(0.5f - (normalizedFill * 0.5f), 0);
                fillRect.anchorMax = new Vector2(0.5f, 1);
            }

            // Reset offsets to ensure anchors strictly define the size
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
        }

        if (pointer != null)
        {
            // Calculate position for the pointer across the full slider range
            float normalizedPos = (value - sustainabilitySlider.minValue) / (sustainabilitySlider.maxValue - sustainabilitySlider.minValue);
            pointer.anchorMin = new Vector2(normalizedPos, pointer.anchorMin.y);
            pointer.anchorMax = new Vector2(normalizedPos, pointer.anchorMax.y);
            pointer.anchoredPosition = new Vector2(0, pointer.anchoredPosition.y);
        }
    }

    void OnEnable()
    {
        TrySubscribe();
    }

    void OnDisable()
    {
        if (_isSubscribed && SustainabilityManager.Instance != null)
        {
            SustainabilityManager.Instance.OnSustainabilityChanged.RemoveListener(UpdateSlider);
        }
        _isSubscribed = false;
    }

    private void TrySubscribe()
    {
        if (_isSubscribed) return;

        if (SustainabilityManager.Instance != null)
        {
            SustainabilityManager.Instance.OnSustainabilityChanged.AddListener(UpdateSlider);
            
            // Immediately initialize the slider with the current sustainability value.
            InitializeSlider(SustainabilityManager.Instance.CurrentSustainability);
            
            _isSubscribed = true;
        }
    }

    private void InitializeSlider(int initialValue)
    {
        if (SustainabilityManager.Instance != null && sustainabilitySlider != null)
        {
            sustainabilitySlider.minValue = SustainabilityManager.Instance.MinSustainability;
            sustainabilitySlider.maxValue = SustainabilityManager.Instance.MaxSustainability;
            
            sustainabilitySlider.value = initialValue; // Set the initial position of the pointer.
            _targetValue = initialValue; // Ensure the target starts at the current value to avoid sliding on start
        }
    }

    private void UpdateSlider(int newSustainabilityValue)
    {
        _targetValue = newSustainabilityValue;
    }
}