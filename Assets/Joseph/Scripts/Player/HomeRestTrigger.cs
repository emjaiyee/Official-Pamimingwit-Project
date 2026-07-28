using UnityEngine;

public class HomeRestTrigger : MonoBehaviour, IInteractable
{
    public string GetInteractPrompt()
    {
        return "Rest for the day [E]";
    }

    public void Interact()
    {
        if (UIManager.Instance == null) return;

        // Start the transition
        UIManager.Instance.StartDayTransition(() => {
            // This runs when the screen is fully black
            if (StaminaManager.Instance != null)
            {
                StaminaManager.Instance.RefillStamina();
            }
        });
    }
}