using UnityEngine;
using System;

public class DynamiteProjectile : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private TrailRenderer trail;

    private Vector3 startPos;
    private Vector3 targetPos;
    private float speed = 1.6f; // Reduced for a slower, more deliberate toss
    private float arcHeight = 1.2f; // Lowered for an underhand trajectory
    private float progress = 0;
    private Action onExplode;

    public void Launch(Vector3 target, Action callback)
    {
        startPos = new Vector3(transform.position.x, transform.position.y, 0);
        targetPos = new Vector3(target.x, target.y, 0);

        if (trail != null)
        {
            trail.Clear();
            trail.emitting = true;
        }

        onExplode = callback;
        progress = 0;
    }

    void Update()
    {
        progress += Time.deltaTime * speed;
        
        float distance = Mathf.Max(Vector3.Distance(startPos, targetPos), 0.1f);
        float t = Mathf.Clamp01(progress / distance);

        // SmoothStep creates a gentle ease-in and ease-out for a more natural feel
        float smoothedT = Mathf.SmoothStep(0f, 1f, t);

        if (t >= 1.0f)
        {
            Explode();
            return;
        }

        // Move horizontally using smoothed progress
        Vector3 currentPos = Vector3.Lerp(startPos, targetPos, smoothedT);

        // Apply the arc height based on the smoothed curve
        float height = Mathf.Sin(smoothedT * Mathf.PI) * arcHeight;
        currentPos.z = 0; // Ensure it stays on the rendering plane
        currentPos.y += height;

        transform.position = currentPos;
        
        // Slower rotation to match the gentler throw speed
        transform.Rotate(0, 0, 320 * Time.deltaTime);
    }

    private void Explode()
    {
        if (trail != null)
        {
            trail.emitting = false;
        }

        onExplode?.Invoke();
        Destroy(gameObject);
    }
}