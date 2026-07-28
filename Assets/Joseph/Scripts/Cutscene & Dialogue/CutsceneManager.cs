using UnityEngine;
using System;
using System.Collections;
using UnityEngine.InputSystem;

public class CutsceneManager : MonoBehaviour
{
    public static CutsceneManager Instance;

    private CutsceneStep[] currentSteps;
    private int currentIndex;
    private Action onFinishCallback;

    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float panelFadeDuration = 0.25f; // For the cutscenePanel itself to fade
    [SerializeField] private float textFadeDuration = 0.15f; // For the text content
    private bool isFading;
    private CanvasGroup cutscenePanelCanvasGroup; // CanvasGroup for the cutscenePanel
    private Coroutine panelFadeCoroutine; // Coroutine for the cutscenePanel fade
    private CanvasGroup cutsceneNameCanvasGroup;
    private CanvasGroup cutsceneContentCanvasGroup;
    private CanvasGroup cutsceneCharacterCanvasGroup;

    public bool IsCutsceneActive { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (!IsCutsceneActive || isFading) return;

        bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        bool mousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        if (spacePressed || mousePressed)
        {
            AdvanceCutscene();
        }
    }

    public void StartCutscene(CutsceneData data, Action onFinish = null)
    {
        if (data == null)
        {
            Debug.LogError("CutsceneManager: Attempted to start a cutscene with null CutsceneData!");
            return;
        }

        if (data.steps.Length == 0)
        {
            Debug.LogWarning($"CutsceneManager: Cutscene '{data.name}' has no steps!");
            return;
        }

        StartCutscene(data.steps, onFinish);
    }

    public void StartCutscene(DialogueLine[] lines, Action onFinish = null)
    {
        CutsceneStep[] converted = new CutsceneStep[lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            converted[i].speakerName = lines[i].speakerName;
            converted[i].dialogue = lines[i].text;
            converted[i].characterSprite = lines[i].icon;
        }
        StartCutscene(converted, onFinish);
    }

    public void StartCutscene(CutsceneStep[] steps, Action onFinish = null)
    {
        if (IsCutsceneActive) return;
        
        // Ensure the cutscenePanel has a CanvasGroup
        if (UIManager.Instance != null && UIManager.Instance.cutscenePanel != null)
        {
            cutscenePanelCanvasGroup = UIManager.Instance.cutscenePanel.GetComponent<CanvasGroup>();
            if (cutscenePanelCanvasGroup == null)
            {
                cutscenePanelCanvasGroup = UIManager.Instance.cutscenePanel.AddComponent<CanvasGroup>();
            }
        }
        else { Debug.LogError("CutsceneManager: UIManager Instance or Cutscene Panel is missing!"); return; }

        // Get or add CanvasGroup for text elements
        if (UIManager.Instance.cutsceneNameText != null)
        {
            cutsceneNameCanvasGroup = UIManager.Instance.cutsceneNameText.GetComponent<CanvasGroup>();
            if (cutsceneNameCanvasGroup == null) cutsceneNameCanvasGroup = UIManager.Instance.cutsceneNameText.gameObject.AddComponent<CanvasGroup>();
            cutsceneNameCanvasGroup.alpha = 0f; // Start hidden
        }
        if (UIManager.Instance.cutsceneContentText != null)
        {
            cutsceneContentCanvasGroup = UIManager.Instance.cutsceneContentText.GetComponent<CanvasGroup>();
            if (cutsceneContentCanvasGroup == null) cutsceneContentCanvasGroup = UIManager.Instance.cutsceneContentText.gameObject.AddComponent<CanvasGroup>();
            cutsceneContentCanvasGroup.alpha = 0f; // Start hidden
        }
        if (UIManager.Instance.cutsceneCharacter != null)
        {
            cutsceneCharacterCanvasGroup = UIManager.Instance.cutsceneCharacter.GetComponent<CanvasGroup>();
            if (cutsceneCharacterCanvasGroup == null) cutsceneCharacterCanvasGroup = UIManager.Instance.cutsceneCharacter.gameObject.AddComponent<CanvasGroup>();
            cutsceneCharacterCanvasGroup.alpha = 0f; // Start hidden
        }

        isFading = true;
        StartCoroutine(StartCutsceneRoutine(steps, onFinish));
    }

    private IEnumerator StartCutsceneRoutine(CutsceneStep[] steps, Action onFinish)
    {
        // If starting at the beginning of the level, snap to black immediately 
        // to avoid seeing the game world for a split second.
        if (Time.timeSinceLevelLoad < 0.2f && UIManager.Instance?.cutsceneFadeOverlay != null)
        {
            UIManager.Instance.cutsceneFadeOverlay.alpha = 1f;
        }

        // Fade to black (or maintain black if already snapped) before setup
        yield return StartCoroutine(Fade(1f));

        currentSteps = steps;
        currentIndex = 0;
        onFinishCallback = onFinish;
        IsCutsceneActive = true;

        // Initialize the first step's data immediately so it's correct during the panel fade-in
        UpdateUI(0);

        if (UIManager.Instance == null || UIManager.Instance.cutscenePanel == null) {
            Debug.LogError("CutsceneManager: UIManager Instance or Cutscene Panel is missing!");
            isFading = false;
            yield break;
        }

        // Set alpha to 0 before activating, then fade in
        cutscenePanelCanvasGroup.alpha = 0f;
        UIManager.Instance.cutscenePanel.SetActive(true);
        GameManager.Instance?.SetState(GameState.UI);
        PlayerController.Instance?.LockMovement();

        yield return StartCoroutine(FadeCutscenePanel(1f, () => UIManager.Instance?.PlayPanelOpenSFX())); // Fade in the cutscene panel and play sound

        yield return StartCoroutine(DisplayStepRoutine()); // Display first step with fade

        // Fade from black (full screen)
        yield return StartCoroutine(Fade(0f));
        isFading = false;
    }

    private void UpdateUI(int index)
    {
        if (index < currentSteps.Length)
        {
            CutsceneStep step = currentSteps[index];
            UIManager ui = UIManager.Instance;

            if (ui.cutsceneNameText) ui.cutsceneNameText.text = step.speakerName;
            if (ui.cutsceneContentText) ui.cutsceneContentText.text = step.dialogue;
            
            if (ui.cutsceneBackground) ui.cutsceneBackground.sprite = step.background;
            
            if (ui.cutsceneCharacter)
            {
                ui.cutsceneCharacter.sprite = step.characterSprite;
                ui.cutsceneCharacter.gameObject.SetActive(step.characterSprite != null);
            }
        }
    }

    private IEnumerator DisplayStepRoutine()
    {
        // Fade out current text if not the very first step
        if (currentIndex > 0 && (cutsceneNameCanvasGroup != null || cutsceneContentCanvasGroup != null || cutsceneCharacterCanvasGroup != null))
        {
            yield return StartCoroutine(FadeText(0f));
        }

        if (currentIndex >= currentSteps.Length)
        {
            // If cutscene is over, initiate the end routine
            StartCoroutine(EndCutsceneRoutine());
            yield break;
        }

        UpdateUI(currentIndex);

        // Fade in new text
        if (cutsceneNameCanvasGroup != null || cutsceneContentCanvasGroup != null || cutsceneCharacterCanvasGroup != null)
        {
            yield return StartCoroutine(FadeText(1f));
        }
    }

    public void AdvanceCutscene()
    {
        if (isFading) return;

        currentIndex++;
        if (currentIndex < currentSteps.Length)
        {
            // Transition if background changes
            if (currentSteps[currentIndex].background != currentSteps[currentIndex - 1].background)
            {
                StartCoroutine(TransitionStepRoutine());
            }
            else
            { // Display next step with fade
                StartCoroutine(DisplayStepRoutine());
            }
        }
        else
        {
            StartCoroutine(EndCutsceneRoutine());
        }
    }

    private IEnumerator TransitionStepRoutine()
    {
        isFading = true;
        yield return StartCoroutine(Fade(1f));
        yield return StartCoroutine(DisplayStepRoutine()); // Display new step after fade to black
        yield return StartCoroutine(Fade(0f));
        isFading = false;
    }

    private IEnumerator EndCutsceneRoutine()
    {
        isFading = true; // Set isFading to true at the start of the routine
        yield return StartCoroutine(FadeCutscenePanel(0f, () => UIManager.Instance?.PlayPanelCloseSFX())); // Fade out the cutscene panel itself and play sound
        yield return StartCoroutine(Fade(1f)); // Fade to black (full screen overlay)

        EndCutscene();
        yield return StartCoroutine(Fade(0f)); // Fade from black (full screen)
        isFading = false;
    }

    private void EndCutscene()
    {
        IsCutsceneActive = false;
        UIManager.Instance.cutscenePanel.SetActive(false);
        
        GameManager.Instance?.SetState(GameState.Normal);
        PlayerController.Instance?.UnlockMovement();

        // Release the narrative lock
        if (NarrativeStateManager.Instance != null) NarrativeStateManager.Instance.IsNarrativeActive = false;

        onFinishCallback?.Invoke();
    }

    private IEnumerator Fade(float targetAlpha)
    {
        if (UIManager.Instance == null || UIManager.Instance.cutsceneFadeOverlay == null) yield break;

        CanvasGroup cg = UIManager.Instance.cutsceneFadeOverlay;
        float startAlpha = cg.alpha;
        float time = 0;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }
        cg.alpha = targetAlpha;
    }

    // New fade method for the cutscenePanel itself
    private IEnumerator FadeCutscenePanel(float targetAlpha, Action onComplete = null)
    {
        if (cutscenePanelCanvasGroup == null) yield break;

        float startAlpha = cutscenePanelCanvasGroup.alpha;
        float time = 0;

        while (time < panelFadeDuration)
        {
            time += Time.deltaTime;
            cutscenePanelCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / panelFadeDuration);
            yield return null;
        }
        cutscenePanelCanvasGroup.alpha = targetAlpha;
        onComplete?.Invoke();
    }

    // New fade method for the text content itself
    private IEnumerator FadeText(float targetAlpha)
    {
        if (cutsceneNameCanvasGroup == null && cutsceneContentCanvasGroup == null && cutsceneCharacterCanvasGroup == null) yield break;

        float startNameAlpha = cutsceneNameCanvasGroup != null ? cutsceneNameCanvasGroup.alpha : 0;
        float startContentAlpha = cutsceneContentCanvasGroup != null ? cutsceneContentCanvasGroup.alpha : 0;
        float startCharacterAlpha = cutsceneCharacterCanvasGroup != null ? cutsceneCharacterCanvasGroup.alpha : 0;
        float time = 0;

        while (time < textFadeDuration)
        {
            time += Time.deltaTime;
            float progress = time / textFadeDuration;
            if (cutsceneNameCanvasGroup != null) cutsceneNameCanvasGroup.alpha = Mathf.Lerp(startNameAlpha, targetAlpha, progress);
            if (cutsceneContentCanvasGroup != null) cutsceneContentCanvasGroup.alpha = Mathf.Lerp(startContentAlpha, targetAlpha, progress);
            if (cutsceneCharacterCanvasGroup != null) cutsceneCharacterCanvasGroup.alpha = Mathf.Lerp(startCharacterAlpha, targetAlpha, progress);
            yield return null;
        }
        if (cutsceneNameCanvasGroup != null) cutsceneNameCanvasGroup.alpha = targetAlpha;
        if (cutsceneContentCanvasGroup != null) cutsceneContentCanvasGroup.alpha = targetAlpha;
        if (cutsceneCharacterCanvasGroup != null) cutsceneCharacterCanvasGroup.alpha = targetAlpha;
    }
}