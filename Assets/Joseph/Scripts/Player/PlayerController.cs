using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance;

    public float moveSpeed = 5f;

    Rigidbody2D rb;
    Vector2 moveInput;

    bool movementLocked;
    private SpriteRenderer spriteRenderer;

    [Header("Shadow Settings")]
    [SerializeField] private Transform shadowTransform;

    private Animator animator;

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

        TeleportToSpawnPoint();
    }

    void Update()
    {
        UpdateSelectedItem();

        if (!movementLocked)
            HandleMovement();
    }

    void LateUpdate()
    {
        SyncShadow();
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

        moveInput = InputHandler.Instance.MoveInput;

        bool isMoving = moveInput != Vector2.zero;
        animator.SetBool("isWalking", isMoving);

        if (isMoving)
        {
            lastMoveDir = moveInput.normalized;

            animator.SetFloat(InputXHash, moveInput.x);
            animator.SetFloat(InputYHash, moveInput.y);
        }
    }

    private void SyncShadow()
    {
        if (shadowTransform == null || spriteRenderer == null) return;

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