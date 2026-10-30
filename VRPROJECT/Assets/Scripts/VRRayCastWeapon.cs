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

    [Header("Visuals & Tracer")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float tracerDuration = 0.05f;
    [SerializeField] private ParticleSystem muzzleFlash;

    [Header("Procedural Recoil Juice (Assign Child Mesh!)")]
    [SerializeField] private Transform gunVisualMesh;
    [SerializeField] private Vector3 recoilKickback = new Vector3(0f, 0.015f, -0.04f);
    [SerializeField] private float recoilPitchAngle = -14f;
    [SerializeField] private float recoilRecoverySpeed = 16f;

    [Header("Audio Juice")]
    [SerializeField] private AudioSource gunAudioSource;
    [SerializeField] private AudioClip shootClip;

    private XRGrabInteractable grabInteractable;
    private IXRSelectInteractor selectingInteractor;

    public TextMeshProUGUI scoreText;
    public int points = 0;

    private Vector3 initialMeshLocalPos;
    private Quaternion initialMeshLocalRot;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (scoreText != null) scoreText.text = "Points: 0";
        if (muzzlePoint == null) muzzlePoint = transform;

        if (gunVisualMesh != null)
        {
            initialMeshLocalPos = gunVisualMesh.localPosition;
            initialMeshLocalRot = gunVisualMesh.localRotation;
        }
    }

    private void Update()
    {
        if (gunVisualMesh != null)
        {
            gunVisualMesh.localPosition = Vector3.Lerp(gunVisualMesh.localPosition, initialMeshLocalPos, Time.deltaTime * recoilRecoverySpeed);
            gunVisualMesh.localRotation = Quaternion.Slerp(gunVisualMesh.localRotation, initialMeshLocalRot, Time.deltaTime * recoilRecoverySpeed);
        }
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

    private void OnGrab(SelectEnterEventArgs args) => selectingInteractor = args.interactorObject;
    private void OnRelease(SelectExitEventArgs args) => selectingInteractor = null;
    private void OnFire(ActivateEventArgs args) => Shoot();

    private void Shoot()
    {
        ApplyProceduralRecoil();
        if (muzzleFlash != null) muzzleFlash.Play();
        if (gunAudioSource != null && shootClip != null)
        {
            gunAudioSource.pitch = Random.Range(0.95f, 1.05f);
            gunAudioSource.PlayOneShot(shootClip);
        }

        SendHaptics(0.35f, 0.05f);

        if (TargetSpawner.Instance != null)
        {
            TargetSpawner.Instance.RegisterShotFired();
        }

        Ray ray = new Ray(muzzlePoint.position, muzzlePoint.forward);
        bool hasHit = Physics.Raycast(ray, out RaycastHit hit, range, hitLayers, QueryTriggerInteraction.Collide);
        Vector3 hitPoint = hasHit ? hit.point : muzzlePoint.position + (muzzlePoint.forward * range);

        if (hasHit)
        {
            VRTarget target = hit.collider.GetComponentInParent<VRTarget>();
            if (target != null)
            {
                target.OnShot(hit.point);
                points++;
                UpdateScore();
                SendHaptics(0.75f, 0.09f);
            }
        }

        if (lineRenderer != null)
        {
            StartCoroutine(ShowTracer(hitPoint));
        }
    }

    private void ApplyProceduralRecoil()
    {
        if (gunVisualMesh == null) return;
        gunVisualMesh.localPosition += recoilKickback;
        gunVisualMesh.localRotation *= Quaternion.Euler(recoilPitchAngle, Random.Range(-2f, 2f), 0f);
    }

    private void SendHaptics(float amplitude, float duration)
    {
        if (selectingInteractor is XRBaseInputInteractor inputInteractor)
        {
            inputInteractor.SendHapticImpulse(amplitude, duration);
        }
    }

    public void UpdateScore()
    {
        if (scoreText != null) scoreText.text = "Points: " + points;
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