using UnityEngine;
using System.Collections;
using TMPro;

[RequireComponent(typeof(Collider2D))]
public class Interactable : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private GameObject visualCuePanel; // The root Panel object
    [SerializeField] private TextMeshProUGUI interactionText;
    [SerializeField] private Vector3 cueOffset = new Vector3(0f, -320f, 0f); // Offset to the left and slightly above
    [SerializeField] private float blinkSpeed = 4f;

    [Header("Floating Effect")]
    [SerializeField] private float floatAmplitude = 0.5f;
    [SerializeField] private float floatSpeed = 3f;

    [Header("Fade Effect")]
    [SerializeField] private float fadeDuration = 0.2f;

    private bool isPlayerInRange;
    private IInteractable interactableComponent;
    private CanvasGroup canvasGroup;
    private Coroutine fadeCoroutine;
    private bool isFadingOut;

    void Awake() {
        // Find the script on this object that uses the IInteractable interface
        interactableComponent = GetComponent<IInteractable>();
        
        if (visualCuePanel != null)
        {
            canvasGroup = visualCuePanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = visualCuePanel.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            visualCuePanel.SetActive(false);
        }

        if (interactableComponent == null)
        {
            Debug.LogWarning($"[Interactable] {gameObject.name} is missing an IInteractable component (like NPCManager or NPC)!");
        }
    }

    void Update()
    {
        if (visualCuePanel == null) return;

        // The cue should only be visible if the player is in range 
        // AND the game is in Normal state (not in a menu, dialogue, or fishing).
        bool isBusy = GameManager.Instance != null && GameManager.Instance.currentState != GameState.Normal;
        bool shouldShow = isPlayerInRange && !isBusy;

        // Handle Fade transitions
        if (shouldShow)
        {
            if (!visualCuePanel.activeSelf || isFadingOut)
            {
                isFadingOut = false;
                visualCuePanel.SetActive(true);
                StartFade(1f);
            }
        }
        else if (visualCuePanel.activeSelf && !isFadingOut)
        {
            isFadingOut = true;
            StartFade(0f, () => 
            {
                visualCuePanel.SetActive(false);
                isFadingOut = false;
            });
        }

        if (visualCuePanel.activeSelf)
        {
            // Enforce consistent placement with a floating bobbing effect
            float floatY = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            visualCuePanel.transform.localPosition = cueOffset + new Vector3(0, floatY, 0);

            // Automatically update the text prompt
            if (interactionText != null && interactableComponent != null)
                interactionText.text = interactableComponent.GetInteractPrompt();
        }

        // Smooth blinking effect using a sine wave
        if (shouldShow && interactionText != null)
        {
            float alpha = (Mathf.Sin(Time.time * blinkSpeed) + 1f) / 2f;
            Color c = interactionText.color;
            c.a = alpha;
            interactionText.color = c;
        }
    }

    private void StartFade(float targetAlpha, System.Action onComplete = null)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha, onComplete));
    }

    private IEnumerator FadeRoutine(float targetAlpha, System.Action onComplete = null)
    {
        float startAlpha = canvasGroup.alpha;
        float time = 0;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = targetAlpha;
        fadeCoroutine = null;
        onComplete?.Invoke();
    }

    public void Interact()
    {
        interactableComponent?.Interact();
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Player")) {
            Debug.Log($"Player entered range of {gameObject.name}");
            isPlayerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other) {
        if (other.CompareTag("Player")) {
            isPlayerInRange = false;
        }
    }
}