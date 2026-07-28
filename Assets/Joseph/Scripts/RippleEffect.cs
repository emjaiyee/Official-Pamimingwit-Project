using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class RippleEffect : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.8f;
    [SerializeField] private Sprite[] animationFrames;
    
    [Header("Procedural Adjustments")]
    [SerializeField] private bool useScaling = true;
    [SerializeField] private Vector3 startScale = new Vector3(0.2f, 0.2f, 1f);
    [SerializeField] private Vector3 endScale = new Vector3(1.2f, 1.2f, 1f);
    [SerializeField] private bool useFading = true;
    
    private SpriteRenderer sr;
    private float timer;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        transform.localScale = startScale;
    }

    public void Initialize(float scaleMultiplier)
    {
        startScale *= scaleMultiplier;
        endScale *= scaleMultiplier;
        transform.localScale = startScale;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = timer / lifetime;

        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        // Handle Spritesheet Animation
        if (animationFrames != null && animationFrames.Length > 0 && sr != null)
        {
            int frameIndex = Mathf.Clamp((int)(t * animationFrames.Length), 0, animationFrames.Length - 1);
            sr.sprite = animationFrames[frameIndex];
        }

        // Optional procedural expansion
        if (useScaling)
            transform.localScale = Vector3.Lerp(startScale, endScale, t);

        // Optional procedural fade out
        if (useFading && sr != null)
        {
            Color c = sr.color;
            c.a = Mathf.Lerp(1f, 0f, t);
            sr.color = c;
        }
    }
}