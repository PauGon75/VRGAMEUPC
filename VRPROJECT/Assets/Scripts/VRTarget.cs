using UnityEngine;

public class VRTarget : MonoBehaviour
{
    [Header("Effects")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private FloatingScorePopup scorePopupPrefab;

    [Header("Visual Juice")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Color freshColor = new Color(0.1f, 0.9f, 0.4f);
    [SerializeField] private Color expiringColor = new Color(1f, 0.2f, 0.2f);
    [SerializeField] private float spawnPopDuration = 0.12f;

    private TargetSpawner spawner;
    private bool isStartTarget;
    private float spawnTime;
    private float lifetime;
    private Vector3 targetScale;
    private bool isHit = false;

    public void Initialize(TargetSpawner owner, bool startTarget, float targetLifetime)
    {
        spawner = owner;
        isStartTarget = startTarget;
        lifetime = targetLifetime;
        spawnTime = Time.time;
        targetScale = transform.localScale;

        transform.localScale = Vector3.zero;

        if (targetRenderer == null) targetRenderer = GetComponentInChildren<Renderer>();
        if (targetRenderer != null && !isStartTarget)
        {
            targetRenderer.material.color = freshColor;
        }
    }

    private void Update()
    {
        if (isHit) return;

        float elapsed = Time.time - spawnTime;

        if (elapsed < spawnPopDuration)
        {
            float t = elapsed / spawnPopDuration;
            float overshoot = Mathf.Sin(t * Mathf.PI * 0.5f) * 1.12f;
            transform.localScale = targetScale * Mathf.Min(overshoot, 1f);
            return;
        }

        if (!isStartTarget && lifetime > 0f)
        {
            float normalizedTime = elapsed / lifetime;

            if (normalizedTime >= 1f)
            {
                if (spawner != null) spawner.OnTargetExpired(this);
                Destroy(gameObject);
            }
            else
            {
                transform.localScale = Vector3.Lerp(targetScale, Vector3.zero, normalizedTime);
                if (targetRenderer != null)
                {
                    targetRenderer.material.color = Color.Lerp(freshColor, expiringColor, normalizedTime);
                }
            }
        }
        else
        {
            transform.localScale = targetScale;
        }
    }

    public void OnShot() => OnShot(transform.position);

    public void OnShot(Vector3 hitWorldPoint)
    {
        if (isHit) return;
        isHit = true;

        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, hitWorldPoint, Quaternion.identity);
        }

        float reactionTime = Time.time - spawnTime;

        float radius = Mathf.Max(0.01f, targetScale.x * 0.5f);
        float distFromCenter = Vector3.Distance(transform.position, hitWorldPoint);
        bool isBullseye = distFromCenter <= (radius * 0.35f);

        int pointsAwarded = 0;
        if (spawner != null)
        {
            pointsAwarded = spawner.OnTargetShot(this, reactionTime, isStartTarget, isBullseye);
        }

        if (!isStartTarget && scorePopupPrefab != null && pointsAwarded > 0)
        {
            var popup = Instantiate(scorePopupPrefab, hitWorldPoint, Quaternion.identity);
            string label = isBullseye ? $"+{pointsAwarded} BULLSEYE!" : $"+{pointsAwarded}";
            Color col = isBullseye ? Color.yellow : Color.white;
            popup.Setup(label, col);
        }

        Destroy(gameObject);
    }
}