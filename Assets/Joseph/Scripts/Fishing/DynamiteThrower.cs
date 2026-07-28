using UnityEngine;
using UnityEngine.InputSystem;

public class DynamiteThrower : MonoBehaviour
{
    public GameObject dynamitePrefab;
    public float throwDistance = 4f;

    bool holding;

    void Update()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame)
            holding = true;

        if (holding && Mouse.current.rightButton.wasReleasedThisFrame)
        {
            Throw();
            holding = false;
        }
    }

    void Throw()
    {
        Vector3 mouse = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mouse.z = 0;

        Vector3 dir = (mouse - transform.position).normalized;

        Vector3 spawn = transform.position + dir * throwDistance;
        Vector3 start = transform.position + new Vector3(0, 0.4f, 0);

        GameObject d = Instantiate(dynamitePrefab, start, Quaternion.identity);
        DynamiteProjectile proj = d.GetComponent<DynamiteProjectile>();
        if (proj != null)
        {
            spawn.z = 0;
            proj.Launch(spawn, null);
        }
    }
}