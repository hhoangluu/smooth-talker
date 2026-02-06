using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls the highway officer character:
/// - Random idle variation triggers
/// - Keyboard/demo gesture triggers
/// - Optional "face target" rotation
/// - Plays radio SFX during gestures
/// 
/// Attach this to the parent logic GameObject (HighwayOfficerCharacter).
/// The visualRoot should be the child that holds the Animator + SkinnedMeshRenderer
/// (e.g., HighwayOfficerVisual).
/// </summary>
public class HighwayOfficerController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Animator visualAnimator;
    [SerializeField] private AudioSource oneShotAudioSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip radioCrackleShort;
    [SerializeField] private AudioClip radioBeepSoft;

    [Header("Facing / Interaction")]
    [SerializeField] private Transform faceTarget;
    [SerializeField] private bool enableFaceTarget = true;
    [SerializeField] private float faceTargetRange = 10f;
    [SerializeField] private float turnSpeedDegPerSec = 180f;

    [Header("Idle Variation Timing (seconds)")]
    [SerializeField] private float minIdleVariationInterval = 5f;
    [SerializeField] private float maxIdleVariationInterval = 12f;

    [Header("Demo Input")]
    [SerializeField] private InputAction trafficStopAction;
    [SerializeField] private InputAction greetingCheckAction;

    // Internal state
    private float nextIdleVariationTime;
    private bool isPlayingGesture;

    // Optional lock duration to prevent spam-triggering gestures
    [Header("Internal Tuning")]
    [SerializeField, Tooltip("Approximate duration during which other idles/gestures are suppressed.")]
    private float gestureLockDuration = 2f;

    // Animator parameter names (must match the AnimatorController configuration)
    private const string IdleShiftStanceTriggerName = "Idle_ShiftStance";
    private const string IdleAdjustBeltHatTriggerName = "Idle_AdjustBeltHat";
    private const string IdleScanRoadTriggerName = "Idle_ScanRoad";
    private const string GestureTrafficStopTriggerName = "Gesture_TrafficStop";
    private const string GestureGreetingCheckTriggerName = "Gesture_GreetingCheck";

    /// <summary>
    /// Unity Reset – called when the component is added or Reset is chosen in the inspector.
    /// Attempts to auto-wire obvious references and populates reasonable defaults.
    /// </summary>
    private void Reset()
    {
        // Try to find a child named "HighwayOfficerVisual" as the visual root.
        if (visualRoot == null)
        {
            Transform child = transform.Find("HighwayOfficerVisual");
            if (child != null)
            {
                visualRoot = child;
            }
            else if (transform.childCount > 0)
            {
                // Fallback: just use first child.
                visualRoot = transform.GetChild(0);
            }
        }

        if (visualRoot != null && visualAnimator == null)
        {
            visualAnimator = visualRoot.GetComponentInChildren<Animator>();
        }

        if (oneShotAudioSource == null)
        {
            oneShotAudioSource = GetComponent<AudioSource>();
            if (oneShotAudioSource == null)
            {
                oneShotAudioSource = gameObject.AddComponent<AudioSource>();
                oneShotAudioSource.playOnAwake = false;
                oneShotAudioSource.loop = false;
            }
        }

        // Default facing config
        enableFaceTarget = true;
        faceTargetRange = 10f;
        turnSpeedDegPerSec = 180f;

        // Default idle timing
        minIdleVariationInterval = 5f;
        maxIdleVariationInterval = 12f;

        // Default demo input actions
        trafficStopAction = new InputAction("TrafficStop", InputActionType.Button, "<Keyboard>/1");
        greetingCheckAction = new InputAction("GreetingCheck", InputActionType.Button, "<Keyboard>/2");

        // Default gesture lock
        gestureLockDuration = 2f;
    }

    private void Awake()
    {
        // Initialize InputActions with default bindings if not configured
        if (trafficStopAction == null || trafficStopAction.bindings.Count == 0)
        {
            trafficStopAction = new InputAction("TrafficStop", InputActionType.Button, "<Keyboard>/1");
        }
        if (greetingCheckAction == null || greetingCheckAction.bindings.Count == 0)
        {
            greetingCheckAction = new InputAction("GreetingCheck", InputActionType.Button, "<Keyboard>/2");
        }

        // No Animator hashes are cached; we use string parameter names per project standard.
        ScheduleNextIdleVariation();
    }

    private void OnEnable()
    {
        trafficStopAction?.Enable();
        greetingCheckAction?.Enable();
        Debug.Log($"[HighwayOfficerController] InputActions enabled. TrafficStop bindings: {trafficStopAction?.bindings.Count}, GreetingCheck bindings: {greetingCheckAction?.bindings.Count}");
    }

    private void OnDisable()
    {
        trafficStopAction?.Disable();
        greetingCheckAction?.Disable();
    }

    private void Update()
    {
        // Handle demo input
        if (trafficStopAction != null && trafficStopAction.WasPressedThisFrame())
        {
            Debug.Log("[HighwayOfficerController] Traffic Stop input received!");
            TriggerTrafficStop();
        }

        if (greetingCheckAction != null && greetingCheckAction.WasPressedThisFrame())
        {
            Debug.Log("[HighwayOfficerController] Greeting Check input received!");
            TriggerGreetingCheck();
        }

        // Debug: check if actions are enabled (press Space to check)
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log($"[HighwayOfficerController] DEBUG - TrafficStop enabled: {trafficStopAction?.enabled}, GreetingCheck enabled: {greetingCheckAction?.enabled}");
        }

        // Idle variations
        if (!isPlayingGesture && Time.time >= nextIdleVariationTime)
        {
            PlayRandomIdleVariation();
            ScheduleNextIdleVariation();
        }

        // Facing logic
        UpdateFacing();
    }

    /// <summary>
    /// Sets the target the officer should turn to face (if enabled).
    /// </summary>
    public void SetFaceTarget(Transform target)
    {
        faceTarget = target;
    }

    /// <summary>
    /// Public API to trigger the "traffic stop" gesture via its Animator trigger.
    /// Also plays a short radio crackle SFX.
    /// </summary>
    public void TriggerTrafficStop()
    {
        if (visualAnimator == null)
            return;

        if (isPlayingGesture)
            return;

        isPlayingGesture = true;

        // Ensure the opposite gesture trigger is cleared so transitions stay predictable.
        visualAnimator.ResetTrigger(GestureGreetingCheckTriggerName);
        visualAnimator.SetTrigger(GestureTrafficStopTriggerName);

        PlayOneShot(radioCrackleShort);
        Invoke(nameof(EndGestureLock), gestureLockDuration);
    }

    /// <summary>
    /// Public API to trigger the "greeting/check" gesture via its Animator trigger.
    /// Also plays a soft radio beep SFX.
    /// </summary>
    public void TriggerGreetingCheck()
    {
        if (visualAnimator == null)
            return;

        if (isPlayingGesture)
            return;

        isPlayingGesture = true;

        // Ensure the opposite gesture trigger is cleared so transitions stay predictable.
        visualAnimator.ResetTrigger(GestureTrafficStopTriggerName);
        visualAnimator.SetTrigger(GestureGreetingCheckTriggerName);

        PlayOneShot(radioBeepSoft);
        Invoke(nameof(EndGestureLock), gestureLockDuration);
    }

    /// <summary>
    /// High-level “intent” wrapper for the traffic stop gesture.
    /// Kept as a clear runtime API: calls <see cref="TriggerTrafficStop"/>.
    /// </summary>
    public void PlayTrafficStop()
    {
        TriggerTrafficStop();
    }

    /// <summary>
    /// High-level “intent” wrapper for the greeting / document check gesture.
    /// Kept as a clear runtime API: calls <see cref="TriggerGreetingCheck"/>.
    /// </summary>
    public void PlayGreetingCheck()
    {
        TriggerGreetingCheck();
    }

    /// <summary>
    /// Plays a specific Animator state immediately on the HighwayOfficerVisual Animator,
    /// blending using CrossFadeInFixedTime.
    /// 
    /// Example:
    /// officerController.PlayState("Gesture_TrafficStop_StateName", 0.1f);
    /// </summary>
    /// <param name="stateName">Exact name of the Animator state to play.</param>
    /// <param name="fadeTime">Fixed-time cross-fade duration in seconds.</param>
    public void PlayState(string stateName, float fadeTime)
    {
        if (visualAnimator == null)
            return;

        if (string.IsNullOrEmpty(stateName))
            return;

        // Respect gesture lock so animations don’t fight each other.
        if (isPlayingGesture)
            return;

        isPlayingGesture = true;

        // Layer 0 is the base layer; using fixed-time cross-fade for predictable blending.
        visualAnimator.CrossFadeInFixedTime(stateName, fadeTime, 0);

        Invoke(nameof(EndGestureLock), gestureLockDuration);
    }

    /// <summary>
    /// Plays an Animator trigger by name on the HighwayOfficerVisual Animator.
    /// Useful for debug buttons or generic systems that know only the trigger string.
    /// 
    /// Example:
    /// officerController.PlayTrigger("Gesture_TrafficStop");
    /// </summary>
    /// <param name="triggerName">Animator trigger parameter name.</param>
    public void PlayTrigger(string triggerName)
    {
        if (visualAnimator == null)
            return;

        if (string.IsNullOrEmpty(triggerName))
            return;

        // Respect gesture lock so animations don’t fight each other.
        if (isPlayingGesture)
            return;

        isPlayingGesture = true;

        visualAnimator.SetTrigger(triggerName);

        Invoke(nameof(EndGestureLock), gestureLockDuration);
    }

    /// <summary>
    /// Picks the next time at which an idle variation will be triggered.
    /// </summary>
    private void ScheduleNextIdleVariation()
    {
        if (maxIdleVariationInterval < minIdleVariationInterval)
        {
            maxIdleVariationInterval = minIdleVariationInterval;
        }

        float interval = Random.Range(minIdleVariationInterval, maxIdleVariationInterval);
        nextIdleVariationTime = Time.time + interval;
    }

    /// <summary>
    /// Randomly selects and plays one of the defined idle variation animations.
    /// Uses string parameter names (no cached hashes).
    /// </summary>
    private void PlayRandomIdleVariation()
    {
        if (visualAnimator == null)
            return;

        int choice = Random.Range(0, 3);

        switch (choice)
        {
            case 0:
                visualAnimator.SetTrigger(IdleShiftStanceTriggerName);
                break;
            case 1:
                visualAnimator.SetTrigger(IdleAdjustBeltHatTriggerName);
                break;
            case 2:
                visualAnimator.SetTrigger(IdleScanRoadTriggerName);
                break;
        }
    }

    /// <summary>
    /// Rotates the officer to face the target on the horizontal plane,
    /// when enabled and within the specified range.
    /// </summary>
    private void UpdateFacing()
    {
        if (!enableFaceTarget || faceTarget == null)
            return;

        Transform root = transform; // logic root rotates whole character

        Vector3 toTarget = faceTarget.position - root.position;
        toTarget.y = 0f;

        float sqrDistance = toTarget.sqrMagnitude;
        if (sqrDistance <= 0.0001f)
            return;

        float rangeSqr = faceTargetRange * faceTargetRange;
        if (sqrDistance > rangeSqr)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        root.rotation = Quaternion.RotateTowards(
            root.rotation,
            targetRotation,
            turnSpeedDegPerSec * Time.deltaTime
        );
    }

    /// <summary>
    /// Plays an AudioClip once via the configured oneShotAudioSource.
    /// </summary>
    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null || oneShotAudioSource == null)
            return;

        oneShotAudioSource.PlayOneShot(clip);
    }

    /// <summary>
    /// Internal callback used to clear the gesture lock after a short duration.
    /// </summary>
    private void EndGestureLock()
    {
        isPlayingGesture = false;
    }
}