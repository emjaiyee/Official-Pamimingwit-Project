using UnityEngine;
using UnityEngine.InputSystem;

public class PlacementManager : MonoBehaviour
{
    public static PlacementManager Instance;

    [Header("Settings")]
    [SerializeField] private Color validColor = new Color(0, 1, 0, 0.5f);
    [SerializeField] private Color invalidColor = new Color(1, 0, 0, 0.5f);

    [Header("Grid Settings")]
    [SerializeField] private Grid grid;
    [SerializeField] private LayerMask obstacleLayer;

    private GameObject previewObject;
    private SpriteRenderer previewRenderer;
    private DeployableData currentDeployable;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        // Don't show placement if UI is open or mouse is over a button
        if (UIManager.Instance != null && (UIManager.Instance.IsUIOpen() || UIManager.Instance.IsPointerOverUI()))
        {
            CancelPlacement();
            return;
        }

        ItemData held = PlayerController.Instance?.GetHeldItem();
        if (held is DeployableData deployable)
        {
            currentDeployable = deployable;
            HandlePlacement();
        }
        else
        {
            CancelPlacement();
        }
    }

    private void HandlePlacement()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mousePos.z = 0;

        // Snap mouse position to the center of the tilemap cell
        if (grid != null)
        {
            Vector3Int cellPos = grid.WorldToCell(mousePos);
            mousePos = grid.GetCellCenterWorld(cellPos);
        }

        Vector3 playerPos = PlayerController.Instance.transform.position;
        float dist = Vector3.Distance(playerPos, mousePos);
        
        bool inRange = dist >= currentDeployable.minDistance && dist <= currentDeployable.maxDistance;
        bool onWater = false;
        if (FishingManager.Instance != null)
        {
            onWater = Physics2D.OverlapCircle(mousePos, 0.2f, FishingManager.Instance.waterLayer);
        }

        bool isOccupied = Physics2D.OverlapPoint(mousePos, obstacleLayer);
        bool isValid = inRange && (!currentDeployable.requireWater || onWater) && !isOccupied;

        // Setup Preview Object
        if (previewObject == null)
        {
            previewObject = new GameObject("PlacementPreview");
            previewRenderer = previewObject.AddComponent<SpriteRenderer>();
            previewRenderer.sortingOrder = 10;
        }

        previewRenderer.sprite = currentDeployable.icon;
        previewObject.transform.position = mousePos;
        previewRenderer.color = isValid ? validColor : invalidColor;

        // Place Item
        if (isValid && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Instantiate(currentDeployable.worldPrefab, mousePos, Quaternion.identity);
            ConsumeHeldItem();
            UIManager.Instance?.ShowMessage($"Deployed {currentDeployable.itemName}!");
        }
    }

    private void CancelPlacement()
    {
        if (previewObject != null) Destroy(previewObject);
        currentDeployable = null;
    }

    private void ConsumeHeldItem()
    {
        if (HotbarManager.Instance == null || Inventory.Instance == null) return;
        int index = HotbarManager.Instance.selectedIndex;
        
        if (Inventory.Instance.itemList[index].amount <= 1 && UIManager.Instance != null)
        {
            ItemSlotUI slotUI = UIManager.Instance.hotbarSlots[index];
            slotUI.AnimatePopOut(() => {
                Inventory.Instance.itemList[index].amount = 0;
                Inventory.Instance.itemList[index].item = null;
                Inventory.Instance.OnInventoryChanged?.Invoke();
            });
        }
        else
        {
            Inventory.Instance.itemList[index].amount--;
            if (Inventory.Instance.itemList[index].amount <= 0) Inventory.Instance.itemList[index].item = null;
            Inventory.Instance.OnInventoryChanged?.Invoke();
        }
    }
}