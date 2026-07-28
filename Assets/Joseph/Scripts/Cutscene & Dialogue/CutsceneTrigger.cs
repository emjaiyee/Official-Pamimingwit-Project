using UnityEngine;
using System.Collections;

public class CutsceneTrigger : MonoBehaviour
{
    public enum TriggerType { OnStart, OnCollision }
    public TriggerType type;
    public CutsceneData cutscene;
    public bool triggerOnce = true;
    [Tooltip("Unique ID for this trigger. Required for saving/loading its triggered state.")]
    [SerializeField]
    public string triggerID;
    private bool hasTriggered;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(triggerID))
        {
            triggerID = $"{gameObject.name}_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif

    [ContextMenu("Generate Unique ID")]
    private void GenerateID() => triggerID = $"{gameObject.name}_{System.Guid.NewGuid().ToString().Substring(0, 8)}";

    private IEnumerator Start()
    {
        if (SaveController.shouldLoadGame)
        {
            while (SaveController.shouldLoadGame) yield return null;
            yield return new WaitForSeconds(0.1f);
        }

        if (string.IsNullOrEmpty(triggerID))
        {
            Debug.LogError($"CutsceneTrigger on {gameObject.name} is missing a unique Trigger ID. It will not save/load correctly.");
        }

        if (triggerOnce && NarrativeStateManager.Instance != null && NarrativeStateManager.Instance.IsTriggered(triggerID))
        {
            hasTriggered = true;
        }

        if (type == TriggerType.OnStart)
        {
            if (hasTriggered && triggerOnce) yield break;

            // Wait until the screen is clear AND no other narrative is active
            while ((UIManager.Instance != null && UIManager.Instance.IsUIOpen()) || (NarrativeStateManager.Instance != null && NarrativeStateManager.Instance.IsNarrativeActive)) yield return null;

            // Stagger the check slightly to prevent frame-perfect race conditions
            yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));

            if (!hasTriggered) Trigger();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (type == TriggerType.OnCollision && other.CompareTag("Player"))
        {
            StartCoroutine(WaitAndTrigger());
        }
    }

    private IEnumerator WaitAndTrigger()
    {
        // Wait for a clear UI and narrative state
        while ((UIManager.Instance != null && UIManager.Instance.IsUIOpen()) || (NarrativeStateManager.Instance != null && NarrativeStateManager.Instance.IsNarrativeActive) || SaveController.shouldLoadGame) yield return null;
        Trigger();
    }

    public void Trigger()
    {
        if (hasTriggered && triggerOnce) return;

        if (CutsceneManager.Instance == null)
        {
            Debug.LogError($"CutsceneTrigger on {gameObject.name}: CutsceneManager.Instance is null! Is there a CutsceneManager in the scene?");
            return;
        }

        // Engage the global narrative lock
        if (NarrativeStateManager.Instance != null)
            NarrativeStateManager.Instance.IsNarrativeActive = true;

        hasTriggered = true;
        NarrativeStateManager.Instance?.SetTriggered(triggerID, true); // Persist state
        CutsceneManager.Instance?.StartCutscene(cutscene);
    }
}
