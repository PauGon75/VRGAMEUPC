using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using TMPro;

[RequireComponent(typeof(XRGrabInteractable))]
public class VRRaycastWeapon : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private float range = 100f;

    [SerializeField] private LayerMask hitLayers = ~0;

    [Header("Visuals & Effects")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float tracerDuration = 0.05f;

    private XRGrabInteractable grabInteractable;
    private IXRSelectInteractor selectingInteractor;

    public TMPro.TextMeshProUGUI scoreText;
    public int points = 0;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        scoreText.text = "Points:  ";

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
        if (TargetSpawner.Instance != null)
        {
            TargetSpawner.Instance.RegisterShotFired();
        }

        Ray ray = new Ray(muzzlePoint.position, muzzlePoint.forward);
        Debug.DrawRay(ray.origin, ray.direction * range, Color.red, 1f);

        bool hasHit = Physics.Raycast(ray, out RaycastHit hit, range, hitLayers, QueryTriggerInteraction.Collide);
        Vector3 hitPoint = hasHit ? hit.point : muzzlePoint.position + (muzzlePoint.forward * range);

        if (hasHit)
        {
            VRTarget target = hit.collider.GetComponentInParent<VRTarget>();

            if (target != null)
            {
                target.OnShot();
                points++;
                UpdateScore();
            }

        }

        if (lineRenderer != null)
        {
            StartCoroutine(ShowTracer(hitPoint));
        }
    }

    public void UpdateScore()
    {
        string v = "Points: " + points.ToString();
        scoreText.text = v;
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