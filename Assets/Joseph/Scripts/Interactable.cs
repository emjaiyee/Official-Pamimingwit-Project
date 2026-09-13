using UnityEngine;
using System.Collections;
using TMPro;

[RequireComponent(typeof(Collider2D))]
public class Interactable : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private GameObject visualCuePanel; 
    [SerializeField] private TextMeshProUGUI interactionText;
    [SerializeField] private Vector3 cueOffset = new Vector3(0f, -320f, 0f); 
    [SerializeField] private float blinkSpeed = 4f;

    [Header("Floating Effect")]
    [SerializeField] private float floatAmplitude = 0.5f;
    [SerializeField] private float floatSpeed = 3f;

    [Header("Fade Effect")]
    [SerializeField] private float fadeDuration = 0.2f;

    private bool isPlayerInRange;
    private IInteractable interactableComponent;
    private CanvasGroup canvasGroup;
    private RectTransform panelRectTransform;
    private Coroutine fadeCoroutine;
    private bool isFadingOut;
    private Color originalTextColor = Color.white;
    private string cachedPrompt = string.Empty;

    private void Awake() 
    {
        interactableComponent = GetComponent<IInteractable>();
        
        if (visualCuePanel != null)
        {
            panelRectTransform = visualCuePanel.GetComponent<RectTransform>();
            canvasGroup = visualCuePanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = visualCuePanel.AddComponent<CanvasGroup>();
            
            canvasGroup.alpha = 0f;
            visualCuePanel.SetActive(false);
        }

        if (interactionText != null)
        {
            originalTextColor = interactionText.color;
        }

        if (interactableComponent == null)
        {
            Debug.LogWarning($"[Interactable] {gameObject.name} is missing an IInteractable component!");
        }
    }

    private void OnEnable()
    {
        InputHandler.RegisterInteractable(this);
    }

    private void OnDisable()
    {
        InputHandler.UnregisterInteractable(this);
        
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (visualCuePanel != null)
        {
            visualCuePanel.SetActive(false);
            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }

        isFadingOut = false;
        isPlayerInRange = false;
        cachedPrompt = string.Empty;
    }

    private void Update()
    {
        if (visualCuePanel == null) return;

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
            // Floating bobbing effect
            float floatY = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            Vector3 targetPosition = cueOffset + new Vector3(0f, floatY, 0f);

            if (panelRectTransform != null)
            {
                panelRectTransform.anchoredPosition = targetPosition;
            }
            else
            {
                visualCuePanel.transform.localPosition = targetPosition;
            }

            // Update prompt text (String allocation cached to prevent GC overhead)
            if (interactionText != null && interactableComponent != null)
            {
                string currentPrompt = interactableComponent.GetInteractPrompt();
                if (cachedPrompt != currentPrompt)
                {
                    cachedPrompt = currentPrompt;
                    interactionText.text = currentPrompt;
                }
                
                // Pulse text alpha smoothly relative to original color base
                float blinkAlpha = (Mathf.Sin(Time.time * blinkSpeed) + 1f) * 0.5f;
                Color targetColor = originalTextColor;
                targetColor.a = originalTextColor.a * blinkAlpha;
                interactionText.color = targetColor;
            }
        }
    }

    private void StartFade(float targetAlpha, System.Action onComplete = null)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha, onComplete));
    }

    private IEnumerator FadeRoutine(float targetAlpha, System.Action onComplete = null)
    {
        if (canvasGroup == null) yield break;

        float startAlpha = canvasGroup.alpha;
        float time = 0f;

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

    private void OnTriggerEnter2D(Collider2D other) 
    {
        if (other.CompareTag("Player")) 
        {
            isPlayerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other) 
    {
        if (other.CompareTag("Player")) 
        {
            isPlayerInRange = false;
        }
    }
}