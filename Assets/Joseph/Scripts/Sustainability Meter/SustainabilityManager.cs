using UnityEngine;
using UnityEngine.Events; // Required for UnityEvent

/// <summary>
/// Manages the game's overall sustainability score.
/// Provides methods to modify the score and an event for UI updates.
/// </summary>
public class SustainabilityManager : MonoBehaviour
{
    public static SustainabilityManager Instance { get; private set; }

    [Header("Sustainability Settings")]
    [Tooltip("The current sustainability value. Starts at neutral (0).")]
    [SerializeField] private int _currentSustainability = 0;
    [Tooltip("The minimum possible sustainability value (e.g., -100 for very unsustainable).")]
    [SerializeField] private int _minSustainability = -100;
    [Tooltip("The maximum possible sustainability value (e.g., 100 for very sustainable).")]
    [SerializeField] private int _maxSustainability = 100;

    // Event to notify listeners (e.g., UI) when the sustainability value changes.
    // It passes the new sustainability value as an integer.
    public UnityEvent<int> OnSustainabilityChanged = new UnityEvent<int>();

    // Public properties to allow other scripts to read the current state and range.
    public int CurrentSustainability => _currentSustainability;
    public int MinSustainability => _minSustainability;
    public int MaxSustainability => _maxSustainability;

    void Awake()
    {
        // Implement the Singleton pattern to ensure only one instance exists.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Optional: Uncomment the line below if you want this manager to persist across scene loads.
            // DontDestroyOnLoad(gameObject); 
        }
    }

    void Start()
    {
        // Ensure the initial value is within the defined bounds and notify listeners.
        _currentSustainability = Mathf.Clamp(_currentSustainability, _minSustainability, _maxSustainability);
        OnSustainabilityChanged?.Invoke(_currentSustainability);
    }

    /// <summary>
    /// Adds or subtracts from the current sustainability value.
    /// The value is automatically clamped between MinSustainability and MaxSustainability.
    /// </summary>
    /// <param name="amount">The amount to add (positive for sustainable actions, negative for unsustainable actions).</param>
    public void Add(int amount)
    {
        int previousSustainability = _currentSustainability;
        _currentSustainability = Mathf.Clamp(_currentSustainability + amount, _minSustainability, _maxSustainability);

        // Only invoke the event if the sustainability value actually changed.
        if (_currentSustainability != previousSustainability)
        {
            Debug.Log($"Sustainability changed from {previousSustainability} to {_currentSustainability}");
            OnSustainabilityChanged?.Invoke(_currentSustainability);
        }
    }
}