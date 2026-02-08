using UnityEngine;

public class PoliceLightbarController : MonoBehaviour
{
    [Header("Light References")]
    [SerializeField] private Light redLight;
    [SerializeField] private Light blueLight;

    [Header("Flash Settings")]
    [Tooltip("Total seconds for a full red/blue cycle.")]
    [SerializeField] private float cycleSeconds = 1.0f;

    [Tooltip("Maximum intensity reached during the sweep.")]
    [SerializeField] private float maxIntensity = 5.0f;

    [Tooltip("Minimum intensity when the light is 'off'.")]
    [SerializeField] private float minIntensity = 0.0f;

    [Tooltip("Phase offset applied to the blue light sweep (in seconds).")]
    [SerializeField] private float sweepPhaseOffset = 0.5f;

    [Header("Optional Rotation")]
    [SerializeField] private bool rotateLightbar = false;

    [Tooltip("Degrees per second to rotate the lightbar around Y.")]
    [SerializeField] private float rotationSpeed = 45f;

    private float _timeOffset;

    private void OnEnable()
    {
        // Small random offset so multiple cruisers don't sync perfectly
        _timeOffset = Random.Range(0f, cycleSeconds);

        // Ensure initial state is applied
        float t = (Time.time + _timeOffset) / Mathf.Max(cycleSeconds, 0.01f);
        t -= Mathf.Floor(t); // wrap to [0,1)
        UpdateLightIntensities(t);
    }

    private void Update()
    {
        if (cycleSeconds <= 0f)
        {
            // If invalid, just keep both at max.
            UpdateLightIntensities(0.5f);
        }
        else
        {
            float t = (Time.time + _timeOffset) / cycleSeconds;
            t -= Mathf.Floor(t); // wrap to [0,1)
            UpdateLightIntensities(t);
        }

        UpdateOptionalRotation();
    }

    /// <summary>
    /// Updates the red/blue light intensities based on a normalized time t01 in [0,1).
    /// Red and blue are offset to create an alternating sweep.
    /// </summary>
    private void UpdateLightIntensities(float t01)
    {
        if (redLight == null && blueLight == null)
            return;

        // Use a smooth pulsing curve: 0..1..0 over t in [0,1]
        // curve(t) = 0.5 * (1 - cos(2πt))
        float Pulse(float t)
        {
            return 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * t));
        }

        float phaseDuration = Mathf.Max(cycleSeconds, 0.01f);
        float phaseOffsetNorm = Mathf.Repeat(sweepPhaseOffset / phaseDuration, 1f);

        // Red is driven by t
        float redT = Mathf.Repeat(t01, 1f);
        float redPulse = Pulse(redT);

        // Blue is offset in time
        float blueT = Mathf.Repeat(t01 + phaseOffsetNorm, 1f);
        float bluePulse = Pulse(blueT);

        float intensityRange = Mathf.Max(0f, maxIntensity - minIntensity);

        if (redLight != null)
        {
            redLight.intensity = minIntensity + redPulse * intensityRange;
            redLight.enabled = redLight.intensity > 0.01f;
        }

        if (blueLight != null)
        {
            blueLight.intensity = minIntensity + bluePulse * intensityRange;
            blueLight.enabled = blueLight.intensity > 0.01f;
        }
    }

    /// <summary>
    /// Optionally rotates the entire lightbar for extra motion.
    /// </summary>
    private void UpdateOptionalRotation()
    {
        if (!rotateLightbar || Mathf.Approximately(rotationSpeed, 0f))
            return;

        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
    }
}