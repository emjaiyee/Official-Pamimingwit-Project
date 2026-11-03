using UnityEngine;

public class WalkingNPC : MonoBehaviour
{
    [SerializeField] private Transform[] waypoints;
    [SerializeField, Min(0f)] private float moveSpeed = 1f;
    [SerializeField, Min(0f)] private float waitAtWaypoints = 0.5f;
    [SerializeField, Min(0.01f)] private float arrivalDistance = 0.05f;
    [SerializeField] private bool loopRoute;
    [SerializeField] private string walkingParameter = "isWalking";
    [SerializeField] private string horizontalParameter = "InputX";
    [SerializeField] private string verticalParameter = "InputY";
    [SerializeField, Tooltip("Optional idle sprite used with the custom directional walking frames.")]
    private Sprite idleSprite;
    [SerializeField, Tooltip("Custom walking frames facing down, in playback order.")]
    private Sprite[] walkingDownSprites;
    [SerializeField, Tooltip("Custom walking frames facing left, in playback order.")]
    private Sprite[] walkingLeftSprites;
    [SerializeField, Tooltip("Custom walking frames facing up, in playback order.")]
    private Sprite[] walkingUpSprites;
    [SerializeField, Tooltip("Custom walking frames facing right, in playback order.")]
    private Sprite[] walkingRightSprites;
    [SerializeField, Min(0.1f), Tooltip("Playback speed for the custom walking frames.")]
    private float customWalkingFramesPerSecond = 8f;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private int targetIndex;
    private int routeDirection = 1;
    private int walkingParameterHash;
    private int horizontalParameterHash;
    private int verticalParameterHash;
    private bool hasWalkingParameter;
    private bool hasHorizontalParameter;
    private bool hasVerticalParameter;
    private bool isWalking;
    private Vector2 lastFacingDirection = Vector2.down;
    private float walkingAnimationTimer;
    private float waitTimer;
    private float fixedZ;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        fixedZ = transform.position.z;
        walkingParameterHash = string.IsNullOrEmpty(walkingParameter)
            ? 0
            : Animator.StringToHash(walkingParameter);
        horizontalParameterHash = string.IsNullOrEmpty(horizontalParameter)
            ? 0
            : Animator.StringToHash(horizontalParameter);
        verticalParameterHash = string.IsNullOrEmpty(verticalParameter)
            ? 0
            : Animator.StringToHash(verticalParameter);

        if (animator != null)
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.nameHash == walkingParameterHash &&
                    parameter.type == AnimatorControllerParameterType.Bool)
                    hasWalkingParameter = true;

                if (parameter.nameHash == horizontalParameterHash &&
                    parameter.type == AnimatorControllerParameterType.Float)
                    hasHorizontalParameter = true;

                if (parameter.nameHash == verticalParameterHash &&
                    parameter.type == AnimatorControllerParameterType.Float)
                    hasVerticalParameter = true;
            }
        }
    }

    private void LateUpdate()
    {
        if (spriteRenderer == null || !HasCustomWalkingAnimation())
            return;

        Sprite[] frames = GetWalkingSprites(lastFacingDirection);
        if (isWalking)
        {
            if (frames == null || frames.Length == 0)
                return;

            int frameIndex = Mathf.FloorToInt(walkingAnimationTimer * customWalkingFramesPerSecond)
                % frames.Length;
            for (int i = 0; i < frames.Length; i++)
            {
                Sprite frame = frames[(frameIndex + i) % frames.Length];
                if (frame == null)
                    continue;

                spriteRenderer.sprite = frame;
                break;
            }

            walkingAnimationTimer += Time.deltaTime;
            return;
        }

        Sprite idleFrame = idleSprite != null ? idleSprite : GetFirstSprite(frames);
        if (idleFrame != null)
            spriteRenderer.sprite = idleFrame;
    }

    private Sprite[] GetWalkingSprites(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            return direction.x < 0f ? walkingLeftSprites : walkingRightSprites;

        return direction.y < 0f ? walkingDownSprites : walkingUpSprites;
    }

    private static Sprite GetFirstSprite(Sprite[] sprites)
    {
        if (sprites == null)
            return null;

        foreach (Sprite sprite in sprites)
        {
            if (sprite != null)
                return sprite;
        }

        return null;
    }

    private bool HasCustomWalkingAnimation()
    {
        return idleSprite != null ||
            GetFirstSprite(walkingDownSprites) != null ||
            GetFirstSprite(walkingLeftSprites) != null ||
            GetFirstSprite(walkingUpSprites) != null ||
            GetFirstSprite(walkingRightSprites) != null;
    }

    private void Start()
    {
        if (waypoints == null || waypoints.Length < 2)
        {
            Debug.LogWarning($"{nameof(WalkingNPC)} on {name} needs at least two waypoints.", this);
            enabled = false;
            return;
        }

        foreach (Transform waypoint in waypoints)
        {
            if (waypoint == null)
            {
                Debug.LogWarning($"{nameof(WalkingNPC)} on {name} has an unassigned waypoint.", this);
                enabled = false;
                return;
            }
        }
    }

    private void Update()
    {
        if (moveSpeed <= 0f)
        {
            SetWalking(false);
            return;
        }

        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            SetWalking(false);
            return;
        }

        Transform target = waypoints[targetIndex];
        if (target == null)
        {
            Debug.LogWarning($"{nameof(WalkingNPC)} on {name} has an unassigned waypoint.", this);
            enabled = false;
            return;
        }

        Vector2 offset = (Vector2)target.position - (Vector2)transform.position;
        if (offset.sqrMagnitude <= arrivalDistance * arrivalDistance)
        {
            AdvanceWaypoint();
            waitTimer = waitAtWaypoints;
            SetWalking(false);
            return;
        }

        Vector2 direction = offset.normalized;
        SetWalking(true, direction);

        Vector3 nextPosition = Vector2.MoveTowards(
            transform.position,
            target.position,
            moveSpeed * Time.deltaTime);
        transform.position = new Vector3(nextPosition.x, nextPosition.y, fixedZ);

        if (spriteRenderer != null &&
            !HasCustomWalkingAnimation() &&
            !hasHorizontalParameter &&
            Mathf.Abs(direction.x) > 0.01f)
            spriteRenderer.flipX = direction.x < 0f;
    }

    private void AdvanceWaypoint()
    {
        if (loopRoute)
        {
            targetIndex = (targetIndex + 1) % waypoints.Length;
            return;
        }

        targetIndex += routeDirection;
        if (targetIndex >= waypoints.Length || targetIndex < 0)
        {
            routeDirection *= -1;
            targetIndex += routeDirection * 2;
        }
    }

    private void SetWalking(bool isWalking, Vector2 direction = default)
    {
        if (this.isWalking != isWalking)
        {
            this.isWalking = isWalking;
            walkingAnimationTimer = 0f;
        }

        if (isWalking)
        {
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
                lastFacingDirection = direction.x < 0f ? Vector2.left : Vector2.right;
            else
                lastFacingDirection = direction.y < 0f ? Vector2.down : Vector2.up;
        }

        if (animator == null)
            return;

        if (hasWalkingParameter)
            animator.SetBool(walkingParameterHash, isWalking);

        if (isWalking && hasHorizontalParameter)
            animator.SetFloat(horizontalParameterHash, direction.x);

        if (isWalking && hasVerticalParameter)
            animator.SetFloat(verticalParameterHash, direction.y);
    }

    private void OnDrawGizmosSelected()
    {
        if (waypoints == null || waypoints.Length < 2)
            return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null)
                continue;

            Gizmos.DrawSphere(waypoints[i].position, 0.08f);
            if (i < waypoints.Length - 1 && waypoints[i + 1] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
        }

        if (loopRoute && waypoints[0] != null && waypoints[waypoints.Length - 1] != null)
            Gizmos.DrawLine(waypoints[waypoints.Length - 1].position, waypoints[0].position);
    }
}
