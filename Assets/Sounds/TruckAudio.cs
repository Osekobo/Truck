using UnityEngine;

[RequireComponent(typeof(TruckController))]
public class TruckAudio : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource engineSource;
    public AudioSource deliverySource;
    public AudioSource brakeSource;

    [Header("Engine")]
    public float minPitch = 0.7f;
    public float maxPitch = 1.5f;

    [Header("Brake")]
    public float brakeMinInterval = 1.5f;

    TruckController _truck;
    float _lastBrakeTime = -999f;
    float _lastRpmNorm = 0f;

    void Awake()
    {
        _truck = GetComponent<TruckController>();
    }

    void Update()
    {
        if (_truck == null) return;

        // --- Engine pitch ---
        if (engineSource != null)
        {
            float rpmNorm = Mathf.InverseLerp(_truck.idleRpm, _truck.redlineRpm, _truck.currentRpm);
            _lastRpmNorm = Mathf.Lerp(_lastRpmNorm, rpmNorm, 4f * Time.deltaTime);
            engineSource.pitch = Mathf.Lerp(minPitch, maxPitch, _lastRpmNorm);
        }
    }

    public void PlayDeliveryDing()
    {
        if (deliverySource != null) deliverySource.Play();
    }

    public void PlayBrakeHiss()
    {
        if (brakeSource == null) return;
        if (Time.time - _lastBrakeTime < brakeMinInterval) return;
        _lastBrakeTime = Time.time;
        brakeSource.Play();
    }
}
