using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using TMPro;

public class ModeMenu : MonoBehaviour
{
    public enum AimGameMode
    {
        Gridshot,     // 3 persistent targets, fast respawns
        Spidershot,   // 1 target at a time, wide flicks
        Microshot,    // Tiny targets in a tight cluster
        Reflex,       // 1 target that shrinks fast and expires
        PatternWave   // Original geometric pattern waves
    }

    [System.Serializable]
    public class ModePreset
    {
        public string title;
        public AimGameMode mode;
        [TextArea] public string description;
        public Vector3 areaSize = new Vector3(5f, 3f, 0.5f);
        public Vector2 scaleRange = new Vector2(0.8f, 1.2f);
        public int simultaneousTargets = 3;
        public float targetLifetime = 0f;
        public bool usePatterns = false;
    }

    [Header("Core References")]
    [SerializeField] private TargetSpawner spawner;
    [SerializeField] private CanvasGroup menuCanvasGroup;

    [Header("UI Text References")]
    [SerializeField] private TMP_Text modeTitleText;
    [SerializeField] private TMP_Text modeDescriptionText;
    [SerializeField] private TMP_Text highScoreText;
    [SerializeField] private TMP_Text durationButtonText;
    [SerializeField] private TMP_Text startStopButtonText;

    [SerializeField] private Button[] modeButtons;
    [SerializeField] private Button durationButton;
    [SerializeField] private Button startStopButton;
    [SerializeField] private Color selectedButtonColor = new Color(0.2f, 0.8f, 1f);
    [SerializeField] private Color normalButtonColor = new Color(0.2f, 0.2f, 0.25f);

    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private float buttonHoverScale = 1.08f;

    [SerializeField]
    private ModePreset[] presets = new ModePreset[]
    {
        new ModePreset {
            title = "GRIDSHOT", mode = AimGameMode.Gridshot,
            description = "3 targets active at all times. Pure speed & rhythm.",
            areaSize = new Vector3(4f, 2.5f, 0.2f), scaleRange = new Vector2(0.9f, 1f),
            simultaneousTargets = 3, targetLifetime = 0f, usePatterns = false
        },
        new ModePreset {
            title = "SPIDERSHOT", mode = AimGameMode.Spidershot,
            description = "1 target at a time across a wide field. Practice big flicks.",
            areaSize = new Vector3(6f, 3.5f, 0.5f), scaleRange = new Vector2(0.75f, 1.1f),
            simultaneousTargets = 1, targetLifetime = 0f, usePatterns = false
        },
        new ModePreset {
            title = "MICROSHOT", mode = AimGameMode.Microshot,
            description = "Small targets in a tight box. Wrist micro-adjustments.",
            areaSize = new Vector3(2.2f, 1.5f, 0.1f), scaleRange = new Vector2(0.35f, 0.45f),
            simultaneousTargets = 3, targetLifetime = 0f, usePatterns = false
        },
        new ModePreset {
            title = "REFLEX", mode = AimGameMode.Reflex,
            description = "Shoot before the target shrinks! High-pressure reaction test.",
            areaSize = new Vector3(4.5f, 2.5f, 0.3f), scaleRange = new Vector2(0.85f, 1f),
            simultaneousTargets = 1, targetLifetime = 1.15f, usePatterns = false
        },
        new ModePreset {
            title = "PATTERN WAVES", mode = AimGameMode.PatternWave,
            description = "Clear geometric clusters & pairs to spawn the next wave.",
            areaSize = new Vector3(5f, 3f, 0.5f), scaleRange = new Vector2(0.8f, 1.1f),
            simultaneousTargets = 3, targetLifetime = 0f, usePatterns = true
        }
    };

    private int currentIndex = 0;
    private float[] durations = { 30f, 60f, 90f };
    private int durationIndex = 1;

    private void Start()
    {
        if (spawner == null) spawner = TargetSpawner.Instance;

        SetupButtons();
        ApplyMode(0);
        ApplyDuration(durationIndex);
    }

    private void SetupButtons()
    {
        for (int i = 0; i < modeButtons.Length; i++)
        {
            int index = i;
            if (modeButtons[i] != null)
            {
                modeButtons[i].onClick.AddListener(() => OnSelectModeClicked(index));
                AddButtonJuice(modeButtons[i].gameObject);
            }
        }

        if (durationButton != null)
        {
            durationButton.onClick.AddListener(OnCycleDurationClicked);
            AddButtonJuice(durationButton.gameObject);
        }

        if (startStopButton != null)
        {
            startStopButton.onClick.AddListener(OnStartOrStopClicked);
            AddButtonJuice(startStopButton.gameObject);
        }
    }

    public void OnSelectModeClicked(int index)
    {
        if (index < 0 || index >= presets.Length) return;
        PlayClickFeedback();
        ApplyMode(index);
    }

    public void OnCycleDurationClicked()
    {
        PlayClickFeedback();
        durationIndex = (durationIndex + 1) % durations.Length;
        ApplyDuration(durationIndex);
    }

    public void OnStartOrStopClicked()
    {
        PlayClickFeedback();
        if (spawner == null) return;

        if (spawner.IsRoundActive)
        {
            spawner.StopRoundEarly();
        }
        else
        {
            spawner.StartRound();
        }
        UpdateMenuUI();
    }

    private void ApplyMode(int index)
    {
        currentIndex = index;
        ModePreset p = presets[index];

        if (spawner != null)
        {
            spawner.ConfigureMode(p);
        }

        for (int i = 0; i < modeButtons.Length; i++)
        {
            if (modeButtons[i] != null && modeButtons[i].TryGetComponent<Image>(out var img))
            {
                img.color = (i == currentIndex) ? selectedButtonColor : normalButtonColor;
            }
        }

        UpdateMenuUI();
    }

    private void ApplyDuration(int dIndex)
    {
        float sec = durations[dIndex];
        if (spawner != null) spawner.SetRoundDuration(sec);
        if (durationButtonText != null) durationButtonText.text = $"Duration: {sec:F0}s";
    }

    public void UpdateMenuUI()
    {
        ModePreset p = presets[currentIndex];
        if (modeTitleText != null) modeTitleText.text = p.title;
        if (modeDescriptionText != null) modeDescriptionText.text = p.description;

        int best = PlayerPrefs.GetInt($"HighScore_{p.mode}", 0);
        if (highScoreText != null) highScoreText.text = $"BEST SCORE: {best}";

        if (startStopButtonText != null && spawner != null)
        {
            startStopButtonText.text = spawner.IsRoundActive ? "STOP ROUND" : "START ROUND";
        }
    }

    private void AddButtonJuice(GameObject btnObj)
    {
        EventTrigger trigger = btnObj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = btnObj.AddComponent<EventTrigger>();

        Vector3 originalScale = btnObj.transform.localScale;

        EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener((data) =>
        {
            btnObj.transform.localScale = originalScale * buttonHoverScale;
            if (uiAudioSource != null && hoverClip != null)
            {
                uiAudioSource.pitch = 1.15f;
                uiAudioSource.PlayOneShot(hoverClip, 0.5f);
            }
            TriggerUIHaptics(data, 0.15f, 0.02f);
        });
        trigger.triggers.Add(enterEntry);

        EventTrigger.Entry exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener((data) =>
        {
            btnObj.transform.localScale = originalScale;
        });
        trigger.triggers.Add(exitEntry);
    }

    private void PlayClickFeedback()
    {
        if (uiAudioSource != null && clickClip != null)
        {
            uiAudioSource.pitch = 1f;
            uiAudioSource.PlayOneShot(clickClip, 0.9f);
        }
    }

    private void TriggerUIHaptics(BaseEventData eventData, float amplitude, float duration)
    {
        if (eventData is PointerEventData pointerData && EventSystem.current.currentInputModule is XRUIInputModule xrInput)
        {
            var interactor = xrInput.GetInteractor(pointerData.pointerId) as XRBaseInputInteractor;
            if (interactor != null)
            {
                interactor.SendHapticImpulse(amplitude, duration);
            }
        }
    }
}