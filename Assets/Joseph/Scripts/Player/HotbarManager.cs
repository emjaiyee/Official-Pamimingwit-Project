using UnityEngine;
using UnityEngine.InputSystem;

public class HotbarManager : MonoBehaviour
{
    public static HotbarManager Instance;

    public int selectedIndex = 0;
    public int hotbarSize = 6;

    [Header("Audio")]
    public AudioClip switchSFX;
    private AudioSource audioSource;

    void Awake()
    {
        Instance = this;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        if (GameManager.Instance == null) return;

        // Block keyboard/scroll selection whenever any UI panel is open (Shop, Inventory, etc.)
        if (UIManager.Instance != null && UIManager.Instance.IsUIOpen()) return;

        HandleSelectionInput();
        HandleUseInput();
    }

    private void HandleUseInput()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            Inventory.Instance?.TryUseSelectedHotbarConsumable();
        }
    }

    private void HandleSelectionInput()
    {
        float scroll = InputHandler.Instance != null ? InputHandler.Instance.GetHotbarScrollDelta() : 0f;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            int newIndex = selectedIndex - (int)Mathf.Sign(scroll);
            if (newIndex < 0) newIndex = hotbarSize - 1;
            if (newIndex >= hotbarSize) newIndex = 0;

            SelectSlot(newIndex);
        }

        // Number Key Selection (1-6), plus Input System action fallback
        if (Keyboard.current != null)
        {
            for (int i = 0; i < hotbarSize; i++)
            {
                if (Keyboard.current[Key.Digit1 + i].wasPressedThisFrame)
                {
                    SelectSlot(i);
                }
            }
        }

        if (InputHandler.Instance != null)
        {
            for (int i = 0; i < hotbarSize; i++)
            {
                string actionName = $"Player/{i + 1}";
                if (InputHandler.Instance.WasActionPressed(actionName))
                {
                    SelectSlot(i);
                }
            }
        }
    }

    public void SelectSlot(int index)
    {
        if (index >= 0 && index < hotbarSize && index != selectedIndex)
        {
            selectedIndex = index;
            Inventory.Instance?.OnInventoryChanged?.Invoke();

            if (switchSFX != null && audioSource != null)
                audioSource.PlayOneShot(switchSFX);
        }
    }

    public ItemData GetSelectedItem()
    {
        if (Inventory.Instance == null || selectedIndex >= Inventory.Instance.itemList.Count)
            return null;

        return Inventory.Instance.itemList[selectedIndex].item;
    }
}
