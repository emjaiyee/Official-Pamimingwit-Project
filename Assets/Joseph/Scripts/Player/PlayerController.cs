using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance;

    public float moveSpeed = 5f;

    Rigidbody2D rb;
    Vector2 moveInput;

    bool movementLocked;
    private SpriteRenderer spriteRenderer;
    private SortingGroup sortingGroup;
    private readonly HashSet<Collider2D> walkInFrontContacts = new HashSet<Collider2D>();
    private int originalSortingLayerID;
    private int originalSortingOrder;

    [Header("Shadow Settings")]
    [SerializeField] private Transform shadowTransform;

    [Header("Water Walking Ripples")]
    [SerializeField] private GameObject waterRipplePrefab;
    [SerializeField] private LayerMask waterLayer;
    [SerializeField, Min(0.05f)] private float waterRippleInterval = 0.45f;
    [SerializeField] private float waterRippleScale = 0.5f;
    [SerializeField] private Vector2 waterRippleOffset = new Vector2(0f, -0.15f);
    [SerializeField, Min(0.01f)] private float waterCheckRadius = 0.1f;
    [SerializeField, Min(0.01f)] private float waterSinkDepth = 2.5f;
    [SerializeField, Range(0f, 1f)] private float waterSinkMinimumVisibleFraction = 0.2f;
    [SerializeField, Range(0.1f, 1f)] private float waterSinkShadowMinimumScale = 0.45f;
    [SerializeField, Range(0f, 1f)] private float waterSinkShadowMinimumAlpha = 0.4f;

    private Animator animator;
    private float waterRippleTimer;
    private bool wasWalkingOnWater;
    private SpriteMask waterSinkMask;
    private float waterVisibleFraction = 1f;
    private float waterSinkProgress;
    private float appliedShadowSinkOffset;
    private float lastShadowPositionY;
    private bool hasLastShadowPosition;
    private SpriteRenderer shadowSpriteRenderer;
    private Vector3 originalShadowScale;
    private Color originalShadowColor;

    // 🆕 CURRENT ITEM
    ItemData currentItem;

    // 🆕 ADDED
    Vector2 lastMoveDir = Vector2.down;

    private static readonly int CastHash = Animator.StringToHash("Cast");
    private static readonly int PullHash = Animator.StringToHash("Pull");
    private static readonly int ThrowHash = Animator.StringToHash("Throw");
    private static readonly int IsFishingHash = Animator.StringToHash("isFishing");
    private static readonly int IsDynamiteHash = Animator.StringToHash("isDynamite");
    private static readonly int InputXHash = Animator.StringToHash("InputX");
    private static readonly int InputYHash = Animator.StringToHash("InputY");

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        sortingGroup = GetComponent<SortingGroup>();
        if (sortingGroup != null)
        {
            originalSortingLayerID = sortingGroup.sortingLayerID;
            originalSortingOrder = sortingGroup.sortingOrder;
        }
        else if (spriteRenderer != null)
        {
            originalSortingLayerID = spriteRenderer.sortingLayerID;
            originalSortingOrder = spriteRenderer.sortingOrder;
        }
        if (shadowTransform != null)
        {
            shadowSpriteRenderer = shadowTransform.GetComponent<SpriteRenderer>();
            originalShadowScale = shadowTransform.localScale;
            if (shadowSpriteRenderer != null)
                originalShadowColor = shadowSpriteRenderer.color;
        }
        InitializeWaterSinkMask();

        TeleportToSpawnPoint();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Collider2D otherCollider = collision.otherCollider;
        if (IsWalkInFrontCollider(otherCollider))
        {
            walkInFrontContacts.Add(otherCollider);
            UpdateWalkInFrontSorting();
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (walkInFrontContacts.Remove(collision.otherCollider))
            UpdateWalkInFrontSorting();
    }

    private bool IsWalkInFrontCollider(Collider2D collider)
    {
        if (collider == null)
            return false;

        SortingGroup sortingGroup = collider.GetComponentInParent<SortingGroup>();
        if (sortingGroup != null)
            return sortingGroup.sortingLayerName == "WalkInfront";

        Renderer targetRenderer = collider.GetComponent<Renderer>();
        if (targetRenderer == null)
            targetRenderer = collider.GetComponentInParent<Renderer>();
        if (targetRenderer == null)
            targetRenderer = collider.GetComponentInChildren<Renderer>();

        return targetRenderer != null && targetRenderer.sortingLayerName == "WalkInfront";
    }

    private void UpdateWalkInFrontSorting()
    {
        int sortingLayerID = walkInFrontContacts.Count > 0
            ? SortingLayer.NameToID("Ground Design")
            : originalSortingLayerID;
        int sortingOrder = walkInFrontContacts.Count > 0
            ? (shadowTransform != null && shadowTransform.TryGetComponent<SortingGroup>(out var shadowSortingGroup)
                ? shadowSortingGroup.sortingOrder + 1
                : originalSortingOrder)
            : originalSortingOrder;

        if (sortingGroup != null)
        {
            sortingGroup.sortingLayerID = sortingLayerID;
            sortingGroup.sortingOrder = sortingOrder;
        }
        else if (spriteRenderer != null)
        {
            spriteRenderer.sortingLayerID = sortingLayerID;
            spriteRenderer.sortingOrder = sortingOrder;
        }
    }

    void Update()
    {
        UpdateSelectedItem();

        if (!movementLocked)
            HandleMovement();

        UpdateWaterRipples();
    }

    void LateUpdate()
    {
        SyncShadow();
        SyncWaterSinkMask();
    }

    void FixedUpdate()
    {
        if (movementLocked)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = moveInput * moveSpeed;
    }

    void HandleMovement()
    {
        if (GameManager.Instance != null &&
            GameManager.Instance.currentState == GameState.UI)
        {
            moveInput = Vector2.zero;
            return;
        }

        if (GameManager.Instance != null &&
        GameManager.Instance.currentState == GameState.Fishing)
        {
            moveInput = Vector2.zero;
            animator.SetBool("isWalking", false);
            return;
        }

        moveInput = InputHandler.Instance != null ? InputHandler.Instance.MoveInput : Vector2.zero;

        bool isMoving = moveInput != Vector2.zero;
        animator.SetBool("isWalking", isMoving);

        if (isMoving)
        {
            lastMoveDir = moveInput.normalized;

            animator.SetFloat(InputXHash, moveInput.x);
            animator.SetFloat(InputYHash, moveInput.y);
        }
    }

    private void UpdateWaterRipples()
    {
        Vector2 ripplePosition = (Vector2)transform.position + waterRippleOffset;
        Collider2D waterCollider = Physics2D.OverlapCircle(ripplePosition, waterCheckRadius, waterLayer);
        UpdateWaterSinking(waterCollider, ripplePosition.y);

        bool isWalkingOnWater =
            !movementLocked &&
            moveInput.sqrMagnitude > 0.01f &&
            waterRipplePrefab != null &&
            waterCollider != null;

        if (!isWalkingOnWater)
        {
            waterRippleTimer = 0f;
            wasWalkingOnWater = false;
            return;
        }

        if (wasWalkingOnWater)
        {
            waterRippleTimer += Time.deltaTime;
            if (waterRippleTimer < Mathf.Max(waterRippleInterval, 0.05f))
                return;
        }

        wasWalkingOnWater = true;
        waterRippleTimer = 0f;

        GameObject ripple = Instantiate(
            waterRipplePrefab,
            new Vector3(ripplePosition.x, ripplePosition.y, transform.position.z),
            Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));

        if (ripple.TryGetComponent<RippleEffect>(out var rippleEffect))
            rippleEffect.Initialize(waterRippleScale);
    }

    private void UpdateWaterSinking(Collider2D waterCollider, float footY)
    {
        float depth = 0f;
        if (waterCollider != null)
        {
            float waterDepth = Mathf.Max(waterSinkDepth, 0.01f);
            depth = Mathf.Clamp01((waterCollider.bounds.max.y - footY) / waterDepth);
        }

        waterSinkProgress = depth;
        waterVisibleFraction = Mathf.Lerp(1f, waterSinkMinimumVisibleFraction, depth);
    }

    private void InitializeWaterSinkMask()
    {
        if (spriteRenderer == null)
            return;

        GameObject maskObject = new GameObject("Water Sink Mask");
        maskObject.transform.SetParent(transform, false);
        waterSinkMask = maskObject.AddComponent<SpriteMask>();
        waterSinkMask.isCustomRangeActive = true;

        SortingGroup sortingGroup = GetComponent<SortingGroup>();
        int sortingLayerID = sortingGroup != null ? sortingGroup.sortingLayerID : spriteRenderer.sortingLayerID;
        int sortingOrder = sortingGroup != null ? sortingGroup.sortingOrder : spriteRenderer.sortingOrder;
        waterSinkMask.backSortingLayerID = sortingLayerID;
        waterSinkMask.frontSortingLayerID = sortingLayerID;
        waterSinkMask.backSortingOrder = sortingOrder - 1;
        waterSinkMask.frontSortingOrder = sortingOrder + 1;
        waterSinkMask.enabled = false;
    }

    private void SyncWaterSinkMask()
    {
        if (spriteRenderer == null || waterSinkMask == null)
            return;

        Sprite sprite = spriteRenderer.sprite;
        bool isSinking = waterVisibleFraction < 0.999f && sprite != null;
        if (!isSinking)
        {
            spriteRenderer.maskInteraction = SpriteMaskInteraction.None;
            waterSinkMask.enabled = false;
            return;
        }

        Bounds spriteBounds = sprite.bounds;
        float visibleFraction = Mathf.Clamp01(waterVisibleFraction);
        bool flipY = spriteRenderer.flipY;
        float verticalDirection = flipY ? -1f : 1f;
        float spriteTop = flipY ? -spriteBounds.min.y : spriteBounds.max.y;

        waterSinkMask.sprite = sprite;
        waterSinkMask.alphaCutoff = 0.01f;
        waterSinkMask.transform.localScale = new Vector3(
            spriteRenderer.flipX ? -1f : 1f,
            verticalDirection * visibleFraction,
            1f);
        waterSinkMask.transform.localPosition = new Vector3(
            0f,
            spriteTop * (1f - visibleFraction),
            0f);
        waterSinkMask.enabled = true;
        spriteRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
    }

    private void SyncShadow()
    {
        if (shadowTransform == null || spriteRenderer == null) return;

        float shadowSinkOffset = spriteRenderer.sprite != null
            ? spriteRenderer.sprite.bounds.size.y *
                Mathf.Abs(spriteRenderer.transform.localScale.y) *
                (1f - waterVisibleFraction)
            : 0f;
        Vector3 shadowPosition = shadowTransform.localPosition;
        if (hasLastShadowPosition && Mathf.Approximately(shadowPosition.y, lastShadowPositionY))
            shadowPosition.y -= appliedShadowSinkOffset;

        shadowPosition.y += shadowSinkOffset;
        shadowTransform.localPosition = shadowPosition;
        appliedShadowSinkOffset = shadowSinkOffset;
        lastShadowPositionY = shadowPosition.y;
        hasLastShadowPosition = true;

        float shadowScale = Mathf.Lerp(1f, waterSinkShadowMinimumScale, waterSinkProgress);
        shadowTransform.localScale = originalShadowScale * shadowScale;

        if (shadowSpriteRenderer != null)
        {
            Color shadowColor = originalShadowColor;
            shadowColor.a *= Mathf.Lerp(1f, waterSinkShadowMinimumAlpha, waterSinkProgress);
            shadowSpriteRenderer.color = shadowColor;
        }

        bool isFlipped = spriteRenderer.flipX;

        // If the player is facing left, mirror the shadow's animated X position.
        // This ensures your "Right" animation remains exactly as you keyed it when facing right.
        if (isFlipped)
        {
            Vector3 localPos = shadowTransform.localPosition;
            shadowTransform.localPosition = new Vector3(-localPos.x, localPos.y, localPos.z);
        }
    }

    // 🆕 CONNECT HOTBAR → PLAYER
    void UpdateSelectedItem()
    {
        if (HotbarManager.Instance == null) return;

        currentItem = HotbarManager.Instance.GetSelectedItem();

        if (GameManager.Instance != null && GameManager.Instance.currentState == GameState.Fishing)
        {
            bool hasRod = EquipmentManager.Instance != null && EquipmentManager.Instance.hasFishingRodEquipped;
            bool hasDynamite = EquipmentManager.Instance != null && EquipmentManager.Instance.hasDynamiteEquipped;

            if (!hasRod && !hasDynamite)
            {
                FishingManager.Instance?.CancelFishing();
            }
        }
    }

    public ItemData GetHeldItem()
    {
        return currentItem;
    }

    public void LockMovement()
    {
        movementLocked = true;
    }

    public void UnlockMovement()
    {
        movementLocked = false;
    }

    public bool IsMoving()
    {
        return moveInput != Vector2.zero;
    }

    // 🆕 ANIMATION CONTROL

    public void StartAiming(bool isDynamite)
    {
        animator.SetBool(IsFishingHash, true);
        animator.SetBool(IsDynamiteHash, isDynamite);

        // Clear any old triggers to prevent an immediate transition to the throw/cast action
        animator.ResetTrigger(CastHash);
        animator.ResetTrigger(ThrowHash);

        animator.Update(0);
    }

    public void SetFishingDirection(Vector2 dir)
    {
        // Mirroring logic: We target the 'Right' animation (InputX = 1) 
        // and flip the sprite visually if aiming Left.
        float targetX = 0;
        float targetY = 0;

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            // Use targetX = 1 for the 'Right' animation in the Blend Tree
            targetX = 1f; 
            targetY = 0;
            // Visually flip the sprite if the actual target direction is Left
            if (spriteRenderer != null) spriteRenderer.flipX = dir.x < 0;
        }
        else
        {
            if (dir.y > 0)
            {
                // Fallback to horizontal (Right) for Upward throws if no dedicated clip exists
                targetX = 1f; 
                targetY = 0;
                if (spriteRenderer != null) spriteRenderer.flipX = lastMoveDir.x < 0;
            }
            else // Aiming Down
            {
                targetX = 0f;
                targetY = -1f;
                if (spriteRenderer != null) { spriteRenderer.flipX = false; }
            }
        }

        animator.SetFloat(InputXHash, targetX);
        animator.SetFloat(InputYHash, targetY);
        
        // Force the animator to process these values immediately to avoid 1-frame lag
        animator.Update(0);
    }

    public void PlayCastAnimation()
    {
        // Ensure isFishing is true BEFORE firing the trigger
        if (!animator.GetBool(IsFishingHash))
        {
            animator.SetBool(IsFishingHash, true);
        }
        animator.SetBool(IsDynamiteHash, false);
        
        animator.ResetTrigger(ThrowHash);
        animator.ResetTrigger(PullHash);
        animator.SetTrigger(CastHash);
        
        // Force the animator to enter the Cast state immediately so rodTip position is accurate
        animator.Update(0);
    }

    public void PlayThrowAnimation()
    {
        if (!animator.GetBool(IsFishingHash))
        {
            animator.SetBool(IsFishingHash, true);
        }
        animator.SetBool(IsDynamiteHash, true);

        animator.ResetTrigger(CastHash);
        animator.ResetTrigger(PullHash);

        // Fire the trigger to transition into the Dynamite Throw Blend Tree
        animator.SetTrigger(ThrowHash);
        animator.Update(0);
    }

    // Triggered by Animation Event in the Cast clips
    public void AE_DeployBobber()
    {
        FishingManager.Instance?.DeployBobber();
    }

    public void PlayPullAnimation()
    {
        // We keep isFishing true so the animator stays in the fishing state logic
        animator.SetTrigger(PullHash); 
    }

    public void SetPulling(bool value)
    {
        animator.SetBool("isPulling", value);
        if (value) animator.SetBool(IsFishingHash, true);
        animator.Update(0);
    }

    public void StopFishingAnimation()
    {
        animator.SetBool(IsFishingHash, false);
        animator.SetBool(IsDynamiteHash, false);
        animator.SetBool("isPulling", false);
        
        // Reset flipX when fishing stops so it doesn't interfere with walking animations
        // that might have their own left/right sprites.
        if (spriteRenderer != null) spriteRenderer.flipX = false;

        // Return InputX/Y to the last movement direction so Idle looks correct
        animator.SetFloat(InputXHash, lastMoveDir.x);
        animator.SetFloat(InputYHash, lastMoveDir.y);

        animator.ResetTrigger(CastHash);
        animator.ResetTrigger(PullHash);
        animator.ResetTrigger(ThrowHash);

        // Force the animator to process the state change immediately to avoid stuck animations
        animator.Update(0);
    }

    // ✅ ADDED (fallback direction if needed)
    public Vector2 GetLastDirection()
    {
        return lastMoveDir;
    }

    // ---------------- SCENE ----------------

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        walkInFrontContacts.Clear();
        UpdateWalkInFrontSorting();
        TeleportToSpawnPoint();
    }

    void TeleportToSpawnPoint()
    {
        if (!string.IsNullOrEmpty(SceneTransferData.TargetSpawnID))
        {
            SceneSpawnPoint[] points = Object.FindObjectsByType<SceneSpawnPoint>(FindObjectsSortMode.None);

            foreach (SceneSpawnPoint sp in points)
            {
                if (sp.spawnID == SceneTransferData.TargetSpawnID)
                {
                    transform.position = sp.transform.position;
                    SceneTransferData.TargetSpawnID = null;
                    break;
                }
            }
        }
    }
}