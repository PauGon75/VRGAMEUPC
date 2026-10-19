using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRGrabInteractable))]
public class VRRaycastWeapon : MonoBehaviour
{
    [Header("Weapon Settings")]
    [Tooltip("The point from which the raycast originates (e.g., the barrel tip).")]
    [SerializeField] private Transform muzzlePoint;

    [Tooltip("Maximum distance of the weapon's raycast.")]
    [SerializeField] private float range = 100f;

    [Tooltip("Layer mask to determine what the weapon can hit.")]
    [SerializeField] private LayerMask hitLayers;

    [Header("Visuals & Effects")]
    [Tooltip("Optional line renderer to show the laser/tracer.")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float tracerDuration = 0.05f;

    private XRGrabInteractable grabInteractable;
    private IXRSelectInteractor selectingInteractor;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        if (muzzlePoint == null)
            muzzlePoint = transform;
    }

    private void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
        grabInteractable.activated.AddListener(OnFire);
    }

    private void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrab);
        grabInteractable.selectExited.RemoveListener(OnRelease);
        grabInteractable.activated.RemoveListener(OnFire);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        selectingInteractor = args.interactorObject;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        selectingInteractor = null;
    }

    private void OnFire(ActivateEventArgs args)
    {
        Shoot();
    }

    private void Shoot()
    {
        Ray ray = new Ray(muzzlePoint.position, muzzlePoint.forward);
        bool hasHit = Physics.Raycast(ray, out RaycastHit hit, range, hitLayers);

        Vector3 hitPoint = hasHit ? hit.point : muzzlePoint.position + (muzzlePoint.forward * range);

        if (hasHit)
        {
            Debug.Log($"Hit target: {hit.collider.name}", hit.collider.gameObject);

            if (hit.collider.TryGetComponent<VRTarget>(out var target))
            {
                target.OnShot();
            }
        }

        if (lineRenderer != null)
        {
            StartCoroutine(ShowTracer(hitPoint));
        }
    }

    private System.Collections.IEnumerator ShowTracer(Vector3 endPoint)
    {
        lineRenderer.enabled = true;

        float elapsedTime = 0f;
        while (elapsedTime < tracerDuration)
        {
            lineRenderer.SetPosition(0, muzzlePoint.position);
            lineRenderer.SetPosition(1, endPoint);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        lineRenderer.enabled = false;
    }
}