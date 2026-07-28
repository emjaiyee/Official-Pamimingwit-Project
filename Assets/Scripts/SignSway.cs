using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class DragSign : MonoBehaviour
{
    private Rigidbody2D rb;

    private bool dragging;

    private Vector3 mouseWorldPos;

    [Header("Drag Strength")]
    public float followSpeed = 15f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
    }

    void Update()
    {
        // Mouse position using NEW Input System
        mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mouseWorldPos.z = 0f;

        // Mouse pressed
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos);

            if (hit != null && hit.gameObject == gameObject)
            {
                dragging = true;
            }
        }

        // Mouse released
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            dragging = false;
        }
    }

    void FixedUpdate()
    {
        if (dragging)
        {
            Vector2 targetPos = Vector2.Lerp(
                rb.position,
                mouseWorldPos,
                followSpeed * Time.fixedDeltaTime
            );

            rb.MovePosition(targetPos);
        }
    }
}