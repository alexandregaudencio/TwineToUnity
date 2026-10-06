using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Component that performs a single pulse (scale up then back) and stops.
public class ImagePulser : MonoBehaviour
{
    // Magnitude of the pulse (fractional change, e.g. 0.05 = 5%)
    public float magnitude = 0.05f;

    private Vector3 baseScale;
    private Coroutine pulseCoroutine;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    /// <summary>
    /// Trigger a single anisotropic pulse. Speed is pulses per second; a value of 1 means
    /// the full pulse sequence (three phases) takes 1 second. If speed <= 0, no pulse occurs.
    /// The pulse does: (1) vertical up & horizontal compress, (2) vertical compress & horizontal stretch,
    /// (3) return to base.
    /// </summary>
    public void PulseOnce(float speed)
    {
        if (speed <= 0f) return;
        float duration = 1f / speed;
        if (pulseCoroutine != null)
            StopCoroutine(pulseCoroutine);
        pulseCoroutine = StartCoroutine(PulseCoroutine(duration));
    }

    private IEnumerator PulseCoroutine(float duration)
    {
        // Divide into three phases
        float phase = duration / 3f;

        // Targets for each phase (relative multipliers)
        Vector3 target1 = new Vector3(1f - magnitude, 1f + magnitude, 1f); // vertical up, horizontal compress
        Vector3 target2 = new Vector3(1f + magnitude, 1f - magnitude, 1f); // vertical compress, horizontal stretch

        // Phase 1: base -> target1
        float t = 0f;
        while (t < phase)
        {
            t += Time.deltaTime;
            float norm = Mathf.Clamp01(t / phase);
            float ease = Mathf.SmoothStep(0f, 1f, norm);
            Vector3 cur = Vector3.Lerp(Vector3.one, target1, ease);
            transform.localScale = Vector3.Scale(baseScale, cur);
            yield return null;
        }

        // Phase 2: target1 -> target2
        t = 0f;
        while (t < phase)
        {
            t += Time.deltaTime;
            float norm = Mathf.Clamp01(t / phase);
            float ease = Mathf.SmoothStep(0f, 1f, norm);
            Vector3 cur = Vector3.Lerp(target1, target2, ease);
            transform.localScale = Vector3.Scale(baseScale, cur);
            yield return null;
        }

        // Phase 3: target2 -> base
        t = 0f;
        while (t < phase)
        {
            t += Time.deltaTime;
            float norm = Mathf.Clamp01(t / phase);
            float ease = Mathf.SmoothStep(0f, 1f, norm);
            Vector3 cur = Vector3.Lerp(target2, Vector3.one, ease);
            transform.localScale = Vector3.Scale(baseScale, cur);
            yield return null;
        }

        // Ensure exact base scale at end
        transform.localScale = baseScale;
        pulseCoroutine = null;
    }
}
