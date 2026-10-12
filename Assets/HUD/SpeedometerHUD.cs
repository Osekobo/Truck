using TMPro;
using UnityEngine;

/// <summary>
/// Drives the speedometer readout. Reads the truck's speed, smooths it, and writes
/// the rounded km/h value into a TextMeshPro label.
/// </summary>
public class SpeedometerHUD : MonoBehaviour
{
    public TextMeshProUGUI speedValueText;
    public TruckController truckController;
    public float smoothing = 5f;

    float _smoothed;

    void Update()
    {
        if (truckController == null || speedValueText == null) return;

        float target = Mathf.Abs(truckController.forwardSpeedKmh);
        _smoothed = Mathf.Lerp(_smoothed, target, smoothing * Time.deltaTime);
        speedValueText.text = Mathf.RoundToInt(_smoothed).ToString();
    }
}
