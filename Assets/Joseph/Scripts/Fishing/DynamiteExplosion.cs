using UnityEngine;

public class DynamiteExplosion : MonoBehaviour
{
    public float radius = 3f;

    void Start()
    {
        Explode();
    }

    void Explode()
    {
        // Ensure explosion is on the correct Z-plane to be visible
        transform.position = new Vector3(transform.position.x, transform.position.y, 0);

        // Get random number of fish, modified by ocean health
        float multiplier = ReactiveOceanManager.Instance != null ? ReactiveOceanManager.Instance.GetCurrentTier().haulMultiplier : 1f;
        int baseCount = Random.Range(2, 6);
        int fishCount = Mathf.Max(1, Mathf.RoundToInt(baseCount * multiplier));

        for (int i = 0; i < fishCount; i++)
        {
            ItemData fish = GetRandomCatch();

            if (fish != null)
                Inventory.Instance.AddItem(fish);
        }

        // Penalize sustainability
        if (SustainabilityManager.Instance != null)
            SustainabilityManager.Instance.Add(-10);

        // Destroy after a delay to allow particles to play and catch logic to finish
        Destroy(gameObject, 2.0f);
    }

    ItemData GetRandomCatch()
    {
        if (ReactiveOceanManager.Instance != null) return ReactiveOceanManager.Instance.GetRandomCatch();
        return null;
    }
}