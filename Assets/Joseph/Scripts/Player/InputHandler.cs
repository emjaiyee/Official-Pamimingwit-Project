﻿using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class InputHandler : MonoBehaviour
{
    public static InputHandler Instance { get; private set; }

    public static InputHandler EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        InputHandler found = FindObjectOfType<InputHandler>(true);
        if (found != null)
        {
            Instance = found;
            return found;
        }

        GameObject inputHandlerObject = new GameObject("InputHandler");
        Instance = inputHandlerObject.AddComponent<InputHandler>();
        return Instance;
    }

    // Static registration pool to avoid FindObjectsByType GC allocations
    private static readonly List<Interactable> registeredInteractables = new List<Interactable>();

    [Header("Interaction Settings")]
    [SerializeField] private float defaultInteractionRadius = 2.0f;

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

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
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

    public static void RegisterInteractable(Interactable interactable)
    {
        if (interactable != null && !registeredInteractables.Contains(interactable))
        {
            registeredInteractables.Add(interactable);
        }
    }

    public static void UnregisterInteractable(Interactable interactable)
    {
        if (interactable != null && registeredInteractables.Contains(interactable))
        {
            registeredInteractables.Remove(interactable);
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
    }

    public void OnClick(InputAction.CallbackContext context)
    {
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

        InteractPressed = true;

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

        Interactable closest = FindClosest();
        if (closest != null) closest.Interact();
    }

    public void OnInventory(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        InventoryPressed = true;

        if (PlayerUIManager.Instance != null)
            PlayerUIManager.Instance.ToggleInventory();
    }

    public void OnCrafting(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        CraftingPressed = true;

        if (UIManager.Instance != null)
            UIManager.Instance.ToggleCrafting();
    }

    public void OnCancel(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        CancelPressed = true;

        UIManager.Instance?.CloseAllStandardPanels();
    }

    public void OnRotate(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        RotatePressed = true;
    }

    private Interactable FindClosest()
    {
        if (PlayerController.Instance == null) return null;

        Vector3 playerPos = PlayerController.Instance.transform.position;
        float minDistanceSq = defaultInteractionRadius * defaultInteractionRadius;
        Interactable closest = null;

        for (int i = registeredInteractables.Count - 1; i >= 0; i--)
        {
            Interactable candidate = registeredInteractables[i];

            if (candidate == null)
            {
                registeredInteractables.RemoveAt(i);
                continue;
            }

            float sqrDist = (playerPos - candidate.transform.position).sqrMagnitude;

            if (sqrDist < minDistanceSq)
            {
                minDistanceSq = sqrDist;
                closest = candidate;
            }
        }

        return closest;
    }
}