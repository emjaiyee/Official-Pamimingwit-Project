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
    public static DialogueManager Instance { get; private set; }

    private DialogueLine[] currentLines;
    private int currentIndex;
    private Action onFinishCallback;

    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private float textFadeDuration = 0.15f;
    
    private CanvasGroup canvasGroup;
    private Coroutine fadeCoroutine;
    private Coroutine lineRoutine;
    private Coroutine textFadeRoutine;

    private CanvasGroup dialogueNameCanvasGroup;
    private CanvasGroup dialogueContentCanvasGroup;
    private CanvasGroup dialogueIconCanvasGroup;

    public bool IsDialogueActive { get; private set; }
    private bool isDisplayingLine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (!IsDialogueActive) return;

        // Block advance while active text transitions are animating
        if (isDisplayingLine) return;

        bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        bool clickPressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        if (spacePressed || clickPressed)
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
        if (lines == null || lines.Length == 0)
        {
            onFinish?.Invoke();
            return;
        }

        currentLines = lines;
        currentIndex = 0;
        onFinishCallback = onFinish;
        IsDialogueActive = true;

        if (UIManager.Instance == null || UIManager.Instance.dialoguePanel == null)
        {
            Debug.LogError("[DialogueManager] UIManager Instance or Dialogue Panel missing!");
            IsDialogueActive = false;
            return;
        }

        GameManager.Instance?.SetState(GameState.UI);
        PlayerController.Instance?.LockMovement();

        UIManager.Instance.dialoguePanel.SetActive(true);

        InitializeCanvasGroups();
        UpdateUI(0);

        canvasGroup.alpha = 0f;
        StartFade(1f, () => {
            UIManager.Instance?.PlayPanelOpenSFX();
            StartLineDisplay();
        });
    }

    private void InitializeCanvasGroups()
    {
        if (canvasGroup == null) canvasGroup = GetOrAddComponent<CanvasGroup>(UIManager.Instance.dialoguePanel);

        if (UIManager.Instance.dialogueNameText != null)
        {
            dialogueNameCanvasGroup = GetOrAddComponent<CanvasGroup>(UIManager.Instance.dialogueNameText.gameObject);
            dialogueNameCanvasGroup.alpha = 0f;
        }
        if (UIManager.Instance.dialogueContentText != null)
        {
            dialogueContentCanvasGroup = GetOrAddComponent<CanvasGroup>(UIManager.Instance.dialogueContentText.gameObject);
            dialogueContentCanvasGroup.alpha = 0f;
        }
        if (UIManager.Instance.dialogueIcon != null)
        {
            dialogueIconCanvasGroup = GetOrAddComponent<CanvasGroup>(UIManager.Instance.dialogueIcon.gameObject);
            dialogueIconCanvasGroup.alpha = 0f;
        }
    }

    private T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        T comp = target.GetComponent<T>();
        return comp != null ? comp : target.AddComponent<T>();
    }

    private void UpdateUI(int index)
    {
        if (index >= currentLines.Length) return;

        DialogueLine current = currentLines[index];
        UIManager ui = UIManager.Instance;
        
        if (ui.dialogueNameText != null) ui.dialogueNameText.text = current.speakerName;
        if (ui.dialogueContentText != null) ui.dialogueContentText.text = current.text;

        if (ui.dialogueIcon != null)
        {
            ui.dialogueIcon.sprite = current.icon;
            ui.dialogueIcon.gameObject.SetActive(current.icon != null);
        }
    }

    private void StartLineDisplay()
    {
        if (lineRoutine != null) StopCoroutine(lineRoutine);
        lineRoutine = StartCoroutine(DisplayLineRoutine());
    }

    private IEnumerator DisplayLineRoutine()
    {
        isDisplayingLine = true;

        if (currentIndex > 0)
        {
            yield return StartCoroutine(FadeText(0f));
        }

        if (currentIndex < currentLines.Length)
        {
            UpdateUI(currentIndex);
            yield return StartCoroutine(FadeText(1f));
        }

        isDisplayingLine = false;
    }

    public void AdvanceDialogue()
    {
        currentIndex++;
        if (currentIndex < currentLines.Length)
        {
            StartLineDisplay();
        }
        else
        {
            EndDialogue();
        }
    }

    private void EndDialogue()
    {
        IsDialogueActive = false;
        isDisplayingLine = false;

        StartFade(0f, () =>
        {
            UIManager.Instance?.PlayPanelCloseSFX();
            if (UIManager.Instance?.dialoguePanel != null) 
                UIManager.Instance.dialoguePanel.SetActive(false);

            if (NarrativeStateManager.Instance != null) 
                NarrativeStateManager.Instance.IsNarrativeActive = false;

            // Execute callback FIRST so secondary UI (like Shops) can acquire state control correctly
            Action callback = onFinishCallback;
            onFinishCallback = null;
            callback?.Invoke();

            // Only restore to normal state if no other UI panel was opened by the callback
            if (UIManager.Instance != null && !UIManager.Instance.IsUIOpen())
            {
                GameManager.Instance?.SetState(GameState.Normal);
                PlayerController.Instance?.UnlockMovement();
            }
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