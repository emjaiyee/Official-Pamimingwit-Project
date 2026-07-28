using UnityEngine;
using System;
using System.Collections;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[Serializable]
public class DialogueLine
{
    public string speakerName;
    [TextArea(3, 10)]
    public string text;
    public Sprite icon;
}

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    private DialogueLine[] currentLines;
    private int currentIndex;
    private Action onFinishCallback;

    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private float textFadeDuration = 0.15f; // For the text content
    private CanvasGroup canvasGroup;
    private Coroutine fadeCoroutine;
    private CanvasGroup dialogueNameCanvasGroup;
    private CanvasGroup dialogueContentCanvasGroup;
    private CanvasGroup dialogueIconCanvasGroup;

    public bool IsDialogueActive { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (!IsDialogueActive) return;

        // Advance dialogue with Spacebar or Left Mouse Button
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame || Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            AdvanceDialogue();
        }
    }

    public void ShowDialogue(CutsceneData data, Action onFinish = null)
    {
        if (data == null || data.steps == null) return;

        DialogueLine[] converted = new DialogueLine[data.steps.Length];
        for (int i = 0; i < data.steps.Length; i++)
        {
            converted[i] = new DialogueLine
            {
                speakerName = data.steps[i].speakerName,
                text = data.steps[i].dialogue,
                icon = data.steps[i].characterSprite
            };
        }
        ShowDialogue(converted, onFinish);
    }

    public void ShowDialogue(DialogueLine[] lines, Action onFinish = null)
    {
        currentLines = lines;
        currentIndex = 0;
        onFinishCallback = onFinish;
        IsDialogueActive = true;

        if (UIManager.Instance == null || UIManager.Instance.dialoguePanel == null)
        {
            Debug.LogError("DialogueManager: UIManager Instance or Dialogue Panel is missing!");
            return;
        }

        GameManager.Instance?.SetState(GameState.UI);
        PlayerController.Instance?.LockMovement();

        UIManager.Instance.dialoguePanel.SetActive(true);

        // Get or add CanvasGroup for the main panel
        if (canvasGroup == null) canvasGroup = UIManager.Instance.dialoguePanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = UIManager.Instance.dialoguePanel.AddComponent<CanvasGroup>();

        // Get or add CanvasGroup for text elements
        if (UIManager.Instance.dialogueNameText != null)
        {
            dialogueNameCanvasGroup = UIManager.Instance.dialogueNameText.GetComponent<CanvasGroup>();
            if (dialogueNameCanvasGroup == null) dialogueNameCanvasGroup = UIManager.Instance.dialogueNameText.gameObject.AddComponent<CanvasGroup>();
            dialogueNameCanvasGroup.alpha = 0f; // Start hidden
        }
        if (UIManager.Instance.dialogueContentText != null)
        {
            dialogueContentCanvasGroup = UIManager.Instance.dialogueContentText.GetComponent<CanvasGroup>();
            if (dialogueContentCanvasGroup == null) dialogueContentCanvasGroup = UIManager.Instance.dialogueContentText.gameObject.AddComponent<CanvasGroup>();
            dialogueContentCanvasGroup.alpha = 0f; // Start hidden
        }
        if (UIManager.Instance.dialogueIcon != null)
        {
            dialogueIconCanvasGroup = UIManager.Instance.dialogueIcon.GetComponent<CanvasGroup>();
            if (dialogueIconCanvasGroup == null) dialogueIconCanvasGroup = UIManager.Instance.dialogueIcon.gameObject.AddComponent<CanvasGroup>();
            dialogueIconCanvasGroup.alpha = 0f; // Start hidden
        }

        // Initialize the first line's data immediately so it's correct during the panel fade-in
        UpdateUI(0);

        canvasGroup.alpha = 0f;
        StartFade(1f, () => {
            UIManager.Instance?.PlayPanelOpenSFX(); // Play sound after panel fully faded in
            StartCoroutine(DisplayLineRoutine());
        }); // Fade in panel, then display first line
    }

    private void UpdateUI(int index)
    {
        if (index < currentLines.Length)
        {
            DialogueLine current = currentLines[index];
            UIManager ui = UIManager.Instance;
            
            if (ui.dialogueNameText != null)
                ui.dialogueNameText.text = current.speakerName;

            if (ui.dialogueContentText != null)
                ui.dialogueContentText.text = current.text;

            if (ui.dialogueIcon != null)
            {
                ui.dialogueIcon.sprite = current.icon;
                ui.dialogueIcon.gameObject.SetActive(current.icon != null);
            }
        }
    }

    private IEnumerator DisplayLineRoutine()
    {
        // Fade out current text if not the very first line
        if (currentIndex > 0 && (dialogueNameCanvasGroup != null || dialogueContentCanvasGroup != null || dialogueIconCanvasGroup != null))
        {
            yield return StartCoroutine(FadeText(0f));
        }

        if (currentIndex < currentLines.Length)
        {
            UpdateUI(currentIndex);

            // Fade in new text
            if (dialogueNameCanvasGroup != null || dialogueContentCanvasGroup != null || dialogueIconCanvasGroup != null)
            {
                yield return StartCoroutine(FadeText(1f));
            }
        }
    }

    public void AdvanceDialogue()
    {
        currentIndex++;
        if (currentIndex < currentLines.Length)
        {
            StartCoroutine(DisplayLineRoutine()); // Start routine to fade out old, show new
        }
        else
        {
            EndDialogue();
        }
    }

    private void EndDialogue()
    {
        IsDialogueActive = false;
        StartFade(0f, () =>
        {
            UIManager.Instance?.PlayPanelCloseSFX(); // Play sound after panel fully faded out
            if (UIManager.Instance.dialoguePanel) UIManager.Instance.dialoguePanel.SetActive(false);

            GameManager.Instance?.SetState(GameState.Normal);
            PlayerController.Instance?.UnlockMovement();

            // Release the narrative lock
            if (NarrativeStateManager.Instance != null) NarrativeStateManager.Instance.IsNarrativeActive = false;

            onFinishCallback?.Invoke();
        });
    }

    private void StartFade(float targetAlpha, Action onComplete = null)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeDialogue(targetAlpha, onComplete));
    }

    private IEnumerator FadeDialogue(float targetAlpha, Action onComplete = null)
    {
        if (canvasGroup == null) yield break;

        float startAlpha = canvasGroup.alpha;
        float time = 0;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = targetAlpha;
        onComplete?.Invoke();
    }

    private IEnumerator FadeText(float targetAlpha)
    {
        if (dialogueNameCanvasGroup == null && dialogueContentCanvasGroup == null && dialogueIconCanvasGroup == null) yield break;

        float startNameAlpha = dialogueNameCanvasGroup != null ? dialogueNameCanvasGroup.alpha : 0;
        float startContentAlpha = dialogueContentCanvasGroup != null ? dialogueContentCanvasGroup.alpha : 0;
        float startIconAlpha = dialogueIconCanvasGroup != null ? dialogueIconCanvasGroup.alpha : 0;
        float time = 0;

        while (time < textFadeDuration)
        {
            time += Time.deltaTime;
            float progress = time / textFadeDuration;
            if (dialogueNameCanvasGroup != null) dialogueNameCanvasGroup.alpha = Mathf.Lerp(startNameAlpha, targetAlpha, progress);
            if (dialogueContentCanvasGroup != null) dialogueContentCanvasGroup.alpha = Mathf.Lerp(startContentAlpha, targetAlpha, progress);
            if (dialogueIconCanvasGroup != null) dialogueIconCanvasGroup.alpha = Mathf.Lerp(startIconAlpha, targetAlpha, progress);
            yield return null;
        }
        if (dialogueNameCanvasGroup != null) dialogueNameCanvasGroup.alpha = targetAlpha;
        if (dialogueContentCanvasGroup != null) dialogueContentCanvasGroup.alpha = targetAlpha;
        if (dialogueIconCanvasGroup != null) dialogueIconCanvasGroup.alpha = targetAlpha;
    }
}