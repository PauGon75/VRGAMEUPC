using UnityEngine;

public class VRTarget : MonoBehaviour
{
    [Tooltip("If true, destroys the GameObject. If false, just disables it (useful if you want to respawn it later).")]
    [SerializeField] private bool destroyOnHit = false;

    [Tooltip("Optional particle effect to spawn when the target is hit.")]
    [SerializeField] private GameObject hitEffectPrefab;

    public void OnShot()
    {
        // Spawn a hit effect if one is assigned
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, transform.rotation);
        }

        // Make the target disappear
        if (destroyOnHit)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}