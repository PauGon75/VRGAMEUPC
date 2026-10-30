using UnityEngine;
using System.Collections.Generic;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TargetSpawner : MonoBehaviour
{
    public static TargetSpawner Instance { get; private set; }

    public enum SpawnPattern
    {
        SingleFlick,
        HorizontalPair,
        VerticalPair,
        TriangleCluster,
        DiagonalSweep,
        RandomMultiBurst
    }

    [Header("Spawner Settings")]
    [SerializeField] private GameObject targetPrefab;
    [SerializeField] private Vector3 area = new Vector3(5f, 3f, 0.5f);
    [SerializeField] private List<GameObject> targets = new List<GameObject>();
    [SerializeField] private int targetsToSpawn = 3;
    [SerializeField] private LayerMask spawnBlockingLayer;

    [Header("Mode Modifiers")]
    [SerializeField] private float patternSpacing = 1.1f;
    [SerializeField] private Vector2 targetScaleRange = new Vector2(0.8f, 1.2f);
    [SerializeField] private float targetLifetime = 0f;
    [SerializeField] private bool spawnInPatternWaves = false;
    private ModeMenu.AimGameMode currentMode = ModeMenu.AimGameMode.Gridshot;

    [Header("PC Debug Controls")]
    [SerializeField] private bool enableKeyboardTest = true;
    [SerializeField] private bool alternateADKeys = false;
    [SerializeField] private bool allowMouseClickToShoot = true;

    [Header("UI & Round")]
    [SerializeField] private float roundDuration = 60f;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private ModeMenu modeMenu;

    [Header("Audio Juice (Combo Pitch)")]
    [SerializeField] private AudioSource spawnerAudio;
    [SerializeField] private AudioClip targetPopClip;
    [SerializeField] private AudioClip streakBreakClip;
    [SerializeField] private AudioClip roundEndClip;

    private float spawnRadius = 0.5f;
    private bool isRoundActive = false;
    public bool IsRoundActive => isRoundActive;

    private float timeRemaining;
    private int score;
    private int shotsFired;
    private int targetsHit;
    private int currentStreak;
    private int hitsInARow;
    private float totalReactionTime;

    private bool expectKeyA = true;
    private SpawnPattern lastPattern = SpawnPattern.SingleFlick;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (targetPrefab != null)
        {
            spawnRadius = targetPrefab.transform.localScale.x / 2f;
        }
        SpawnStartTarget();
    }

    private void Update()
    {
        if (enableKeyboardTest) HandlePCDebugInput();
        if (!isRoundActive) return;

        timeRemaining -= Time.deltaTime;
        UpdateUI();

        if (timeRemaining <= 0f)
        {
            EndRound();
        }
    }

    public void ConfigureMode(ModeMenu.ModePreset preset)
    {
        currentMode = preset.mode;
        area = preset.areaSize;
        targetScaleRange = preset.scaleRange;
        targetsToSpawn = preset.simultaneousTargets;
        targetLifetime = preset.targetLifetime;
        spawnInPatternWaves = preset.usePatterns;

        if (isRoundActive)
        {
            isRoundActive = false;
        }
        SpawnStartTarget();
        UpdateUI($"Mode Selected: {preset.title}\nPress Start or Shoot Start Target!");
    }

    public void SetRoundDuration(float seconds)
    {
        roundDuration = seconds;
    }

    public void StopRoundEarly()
    {
        if (!isRoundActive) return;
        isRoundActive = false;
        ClearAllTargets();
        SpawnStartTarget();
        UpdateUI("Round Cancelled.\nSelect a Mode to Play.");
        if (modeMenu != null) modeMenu.UpdateMenuUI();
    }

    private void HandlePCDebugInput()
    {
        bool aPressed = Keyboard.current != null && Keyboard.current.aKey.wasPressedThisFrame;
        bool dPressed = Keyboard.current != null && Keyboard.current.dKey.wasPressedThisFrame;
        bool mouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        if (alternateADKeys)
        {
            if (aPressed && expectKeyA) { expectKeyA = false; PopNextTargetFromKeyboard(); }
            else if (dPressed && !expectKeyA) { expectKeyA = true; PopNextTargetFromKeyboard(); }
            else if (aPressed || dPressed) RegisterShotFired();
        }
        else if (aPressed)
        {
            PopNextTargetFromKeyboard();
        }

        if (allowMouseClickToShoot && mouseClicked && Camera.main != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(mousePos);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                RegisterShotFired();
                VRTarget target = hit.collider.GetComponentInParent<VRTarget>();
                if (target != null) target.OnShot(hit.point);
            }
            else
            {
                RegisterShotFired();
            }
        }
    }

    private void PopNextTargetFromKeyboard()
    {
        targets.RemoveAll(t => t == null);
        if (targets.Count > 0)
        {
            RegisterShotFired();
            VRTarget vrTarget = targets[0].GetComponentInChildren<VRTarget>();
            if (vrTarget != null) vrTarget.OnShot();
        }
    }

    private void SpawnStartTarget()
    {
        ClearAllTargets();
        expectKeyA = true;

        GameObject startObj = Instantiate(targetPrefab, transform.position, Quaternion.identity);
        if (startObj.TryGetComponent<VRTarget>(out var vrTarget))
        {
            vrTarget.Initialize(this, true, 0f);
        }
        targets.Add(startObj);
    }

    public void StartRound()
    {
        ClearAllTargets();
        score = 0;
        shotsFired = 0;
        targetsHit = 0;
        currentStreak = 0;
        hitsInARow = 0;
        totalReactionTime = 0f;
        timeRemaining = roundDuration;
        isRoundActive = true;
        expectKeyA = true;

        if (spawnInPatternWaves)
        {
            SpawnNextPattern();
        }
        else
        {
            for (int i = 0; i < targetsToSpawn; i++)
            {
                SpawnTargetAtFreePosition();
            }
        }
        if (modeMenu != null) modeMenu.UpdateMenuUI();
    }

    private void EndRound()
    {
        isRoundActive = false;
        timeRemaining = 0f;
        ClearAllTargets();

        if (spawnerAudio != null && roundEndClip != null)
        {
            spawnerAudio.pitch = 1f;
            spawnerAudio.PlayOneShot(roundEndClip);
        }

        string key = $"HighScore_{currentMode}";
        int prevBest = PlayerPrefs.GetInt(key, 0);
        bool newRecord = score > prevBest;
        if (newRecord)
        {
            PlayerPrefs.SetInt(key, score);
            PlayerPrefs.Save();
        }

        float accuracy = shotsFired > 0 ? ((float)targetsHit / shotsFired) * 100f : 0f;
        float avgReactionMs = targetsHit > 0 ? (totalReactionTime / targetsHit) * 1000f : 0f;

        string recordTag = newRecord ? " [NEW RECORD!]" : "";
        string summary = $"ROUND OVER!{recordTag}\nScore: {score} | Hits: {targetsHit} | Acc: {accuracy:F1}% | Avg: {avgReactionMs:F0}ms";
        UpdateUI(summary);

        if (modeMenu != null) modeMenu.UpdateMenuUI();
        Invoke(nameof(SpawnStartTarget), 2.5f);
    }

    public void RegisterShotFired()
    {
        if (!isRoundActive) return;
        shotsFired++;

        if (shotsFired - targetsHit > hitsInARow)
        {
            currentStreak = 0;
        }
        UpdateUI();
    }

    public int OnTargetShot(VRTarget target, float reactionTime, bool isStartTarget, bool isBullseye)
    {
        targets.Remove(target.gameObject);
        targets.RemoveAll(t => t == null);

        if (isStartTarget)
        {
            StartRound();
            return 0;
        }

        if (!isRoundActive) return 0;

        targetsHit++;
        currentStreak++;
        hitsInARow = targetsHit;
        totalReactionTime += reactionTime;

        if (spawnerAudio != null && targetPopClip != null)
        {
            spawnerAudio.pitch = Mathf.Clamp(0.9f + (currentStreak * 0.04f), 0.9f, 1.8f);
            spawnerAudio.PlayOneShot(targetPopClip);
        }

        int speedBonus = Mathf.Clamp(Mathf.RoundToInt((1f - reactionTime) * 50f), 0, 50);
        int bullseyeBonus = isBullseye ? 50 : 0;
        int pointsEarned = 100 + speedBonus + bullseyeBonus + (currentStreak * 5);
        score += pointsEarned;

        if (spawnInPatternWaves)
        {
            if (targets.Count == 0) SpawnNextPattern();
        }
        else
        {
            while (targets.Count < targetsToSpawn)
            {
                SpawnTargetAtFreePosition();
            }
        }

        UpdateUI();
        return pointsEarned;
    }

    public void OnTargetExpired(VRTarget target)
    {
        targets.Remove(target.gameObject);
        targets.RemoveAll(t => t == null);
        if (!isRoundActive) return;

        currentStreak = 0;
        if (spawnerAudio != null && streakBreakClip != null)
        {
            spawnerAudio.pitch = 1f;
            spawnerAudio.PlayOneShot(streakBreakClip);
        }

        if (spawnInPatternWaves)
        {
            if (targets.Count == 0) SpawnNextPattern();
        }
        else
        {
            while (targets.Count < targetsToSpawn)
            {
                SpawnTargetAtFreePosition();
            }
        }
        UpdateUI();
    }

    private void SpawnNextPattern()
    {
        SpawnPattern nextPattern = GetRandomAlternatingPattern();
        lastPattern = nextPattern;

        Vector3 anchorPos = GetRandomPointInArea(0.65f);
        List<Vector3> patternOffsets = new List<Vector3>();

        switch (nextPattern)
        {
            case SpawnPattern.SingleFlick:
                patternOffsets.Add(Vector3.zero);
                break;
            case SpawnPattern.HorizontalPair:
                patternOffsets.Add(new Vector3(-patternSpacing * 0.5f, 0f, 0f));
                patternOffsets.Add(new Vector3(patternSpacing * 0.5f, 0f, 0f));
                break;
            case SpawnPattern.VerticalPair:
                patternOffsets.Add(new Vector3(0f, -patternSpacing * 0.5f, 0f));
                patternOffsets.Add(new Vector3(0f, patternSpacing * 0.5f, 0f));
                break;
            case SpawnPattern.TriangleCluster:
                patternOffsets.Add(new Vector3(0f, patternSpacing * 0.6f, 0f));
                patternOffsets.Add(new Vector3(-patternSpacing * 0.6f, -patternSpacing * 0.4f, 0f));
                patternOffsets.Add(new Vector3(patternSpacing * 0.6f, -patternSpacing * 0.4f, 0f));
                break;
            case SpawnPattern.DiagonalSweep:
                float dir = Random.value > 0.5f ? 1f : -1f;
                for (int i = -1; i <= 1; i++)
                    patternOffsets.Add(new Vector3(i * patternSpacing * 0.8f, i * dir * patternSpacing * 0.8f, 0f));
                break;
            case SpawnPattern.RandomMultiBurst:
                int burstCount = Random.Range(2, Mathf.Max(3, targetsToSpawn + 1));
                for (int i = 0; i < burstCount; i++) SpawnTargetAtFreePosition();
                return;
        }

        foreach (Vector3 offset in patternOffsets)
        {
            Vector3 candidatePos = ClampToArea(anchorPos + offset);
            if (!Physics.CheckSphere(candidatePos, spawnRadius, spawnBlockingLayer))
                InstantiateTarget(candidatePos);
            else
                SpawnTargetAtFreePosition();
        }
    }

    private SpawnPattern GetRandomAlternatingPattern()
    {
        SpawnPattern[] allPatterns = (SpawnPattern[])System.Enum.GetValues(typeof(SpawnPattern));
        SpawnPattern chosen = lastPattern;
        for (int i = 0; i < 10; i++)
        {
            chosen = allPatterns[Random.Range(0, allPatterns.Length)];
            if (chosen != lastPattern) break;
        }
        return chosen;
    }

    private void SpawnTargetAtFreePosition()
    {
        float checkRadius = Mathf.Max(0.15f, spawnRadius * targetScaleRange.y);
        for (int attempt = 0; attempt < 40; attempt++)
        {
            Vector3 spawnPos = GetRandomPointInArea(1f);
            if (!Physics.CheckSphere(spawnPos, checkRadius, spawnBlockingLayer))
            {
                InstantiateTarget(spawnPos);
                return;
            }
        }
        InstantiateTarget(GetRandomPointInArea(0.8f));
    }

    private void InstantiateTarget(Vector3 position)
    {
        GameObject newTarget = Instantiate(targetPrefab, position, Quaternion.identity);
        float scaleMult = Random.Range(targetScaleRange.x, targetScaleRange.y);
        newTarget.transform.localScale = targetPrefab.transform.localScale * scaleMult;

        if (newTarget.TryGetComponent<VRTarget>(out var vrTarget))
        {
            vrTarget.Initialize(this, false, targetLifetime);
        }
        targets.Add(newTarget);
    }

    private Vector3 GetRandomPointInArea(float marginMultiplier)
    {
        Vector3 halfArea = (area * 0.5f) * marginMultiplier;
        return transform.position + new Vector3(
            Random.Range(-halfArea.x, halfArea.x),
            Random.Range(-halfArea.y, halfArea.y),
            Random.Range(-halfArea.z, halfArea.z)
        );
    }

    private Vector3 ClampToArea(Vector3 worldPos)
    {
        Vector3 half = area * 0.5f;
        Vector3 min = transform.position - half;
        Vector3 max = transform.position + half;
        return new Vector3(
            Mathf.Clamp(worldPos.x, min.x, max.x),
            Mathf.Clamp(worldPos.y, min.y, max.y),
            Mathf.Clamp(worldPos.z, min.z, max.z)
        );
    }

    private void ClearAllTargets()
    {
        CancelInvoke(nameof(SpawnStartTarget));
        foreach (var t in targets)
        {
            if (t != null) Destroy(t);
        }
        targets.Clear();
    }

    private void UpdateUI(string customMessage = null)
    {
        if (timerText != null)
        {
            timerText.text = isRoundActive ? $"Time: {timeRemaining:F1}s" : "READY TO START";
        }

        if (statsText != null)
        {
            if (!string.IsNullOrEmpty(customMessage))
            {
                statsText.text = customMessage;
            }
            else
            {
                float accuracy = shotsFired > 0 ? ((float)targetsHit / shotsFired) * 100f : 100f;
                float avgReactionMs = targetsHit > 0 ? (totalReactionTime / targetsHit) * 1000f : 0f;
                statsText.text = $"Score: {score} | Streak: {currentStreak}x\nMode: {currentMode}\nAcc: {accuracy:F1}% | Hits: {targetsHit}\nAvg Speed: {avgReactionMs:F0}ms";
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, area);
    }
}