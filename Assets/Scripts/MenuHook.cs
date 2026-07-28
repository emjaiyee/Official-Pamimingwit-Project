using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class DanglingSign : MonoBehaviour
{
    [Header("Sign Reference")]
    public Transform sign;

    [Header("Drop Settings")]
    public float dropHeight = 2f;
    public float dropSpeed = 4f;

    [Header("Swing Settings")]
    public float swingAngle = 5f;
    public float swingSpeed = 1f;
    public float swingSmooth = 3f;

    private LineRenderer lr;

    private Vector3 startPos;
    private bool dropped;

    private float currentAngle;

    void Start()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = 2;

        // Save original position
        startPos = transform.position;

        // Start ABOVE original position
        transform.position += Vector3.up * dropHeight;
    }

    void Update()
    {
        DropAnimation();

        UpdateRope();
    }

    void DropAnimation()
    {
        // Drop down first
        if (!dropped)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                startPos,
                Time.deltaTime * dropSpeed
            );

            // Close enough to original position
            if (Vector3.Distance(transform.position, startPos) < 0.01f)
            {
                dropped = true;
            }
        }
        else
        {
            // Natural sway after drop
            float targetAngle =
                Mathf.Sin(Time.time * swingSpeed) * swingAngle;

            currentAngle = Mathf.Lerp(
                currentAngle,
                targetAngle,
                Time.deltaTime * swingSmooth
            );

            transform.rotation =
                Quaternion.Euler(0, 0, currentAngle);
        }
    }

    void UpdateRope()
    {
        // Rope start
        lr.SetPosition(0, Vector3.zero);

        // Rope end at top of sign
        Vector3 signTop =
            sign.localPosition + Vector3.up * 0.5f;

        lr.SetPosition(1, signTop);
    }
}