﻿using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    public static InputHandler Instance { get; private set; }

    public Vector2 MoveInput { get; private set; }

    public bool ClickDown { get; private set; }
    public bool ClickHeld { get; private set; }
    public bool ClickUp { get; private set; }

    public bool RightClickDown { get; private set; }

    public bool InteractPressed { get; private set; }
    public bool InventoryPressed { get; private set; }
    public bool CraftingPressed { get; private set; }
    public bool CancelPressed { get; private set; }
    public bool RotatePressed { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void LateUpdate()
    {
        ClickDown = false;
        ClickUp = false;
        RightClickDown = false;

        InteractPressed = false;
        InventoryPressed = false;
        CraftingPressed = false;
        CancelPressed = false;
        RotatePressed = false;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        // Always track hold and release states regardless of GameState
        if (context.started)
        {
            ClickDown = true;
            ClickHeld = true;
        }

        if (context.canceled)
        {
            ClickUp = true;
            ClickHeld = false;
        }
    }

    public void OnRightClick(InputAction.CallbackContext context)
    {
        if (context.started)
            RightClickDown = true;
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        if (GameManager.Instance == null) return;

        // Handle dialogue advancement if in UI state
        if (GameManager.Instance.currentState == GameState.UI)
        {
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
            {
                DialogueManager.Instance.AdvanceDialogue();
            }
            else if (CutsceneManager.Instance != null && CutsceneManager.Instance.IsCutsceneActive)
            {
                CutsceneManager.Instance.AdvanceCutscene();
            }
            return;
        }

        if (GameManager.Instance.currentState != GameState.Normal) return;

        Interactable i = FindClosest();
        if (i != null) i.Interact();
    }

    public void OnInventory(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        if (PlayerUIManager.Instance != null)
            PlayerUIManager.Instance.ToggleInventory();
    }

    public void OnCrafting(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        if (UIManager.Instance != null)
            UIManager.Instance.ToggleCrafting();
    }

    public void OnCancel(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        UIManager.Instance?.CloseAllStandardPanels();
    }

    public void OnRotate(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        RotatePressed = true;
    }

    private Interactable FindClosest()
    {
        Interactable[] list = Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None);

        float dist = 999f;
        Interactable closest = null;

        Vector3 playerPos = PlayerController.Instance != null ? PlayerController.Instance.transform.position : transform.position;

        foreach (var i in list)
        {
            float d = Vector2.Distance(playerPos, i.transform.position);

            if (d < 2f && d < dist)
            {
                dist = d;
                closest = i;
            }
        }

        return closest;
    }
}