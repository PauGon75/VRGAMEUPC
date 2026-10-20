using UnityEngine;

public class VRTarget : MonoBehaviour
{
    [SerializeField] private GameObject hitEffectPrefab;

    private TargetSpawner spawner;
    private bool isStartTarget;
    private float spawnTime;
    private float lifetime;
    private Vector3 initialScale;
    private bool isHit = false;

    public void Initialize(TargetSpawner owner, bool startTarget, float targetLifetime)
    {
        spawner = owner;
        isStartTarget = startTarget;
        lifetime = targetLifetime;
        spawnTime = Time.time;
        initialScale = transform.localScale;
    }

    private void Update()
    {
        if (!isStartTarget && lifetime > 0f && !isHit)
        {
            float elapsed = Time.time - spawnTime;
            float normalizedTime = elapsed / lifetime;

            if (normalizedTime >= 1f)
            {
                if (spawner != null) spawner.OnTargetExpired(this);
                Destroy(gameObject);
            }
            else
            {
                transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, normalizedTime);
            }
        }
    }

    public void OnShot()
    {
        if (isHit) return;
        isHit = true;

        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, transform.rotation);
        }

        float reactionTime = Time.time - spawnTime;

        if (spawner != null)
        {
            spawner.OnTargetShot(this, reactionTime, isStartTarget);
        }

        Destroy(gameObject);
    }
}