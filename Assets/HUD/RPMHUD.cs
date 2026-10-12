using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RPMHUD : MonoBehaviour
{
    public TruckController truck;
    public TextMeshProUGUI rpmValueText;
    public Image rpmBarFill;

    public Color normalColor = new Color(0.96f, 0.62f, 0.04f);
    public Color redlineColor = new Color(0.94f, 0.27f, 0.27f);

    void Start()
    {
        if (rpmBarFill != null)
        {
            rpmBarFill.type = Image.Type.Filled;
            rpmBarFill.fillMethod = Image.FillMethod.Horizontal;
            rpmBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            rpmBarFill.fillAmount = 0f;
        }
    }

    void Update()
    {
        if (truck == null || rpmValueText == null || rpmBarFill == null) return;
        float rpm = truck.currentRpm;
        rpmValueText.text = Mathf.RoundToInt(rpm).ToString();
        float t = Mathf.Clamp01((rpm - truck.idleRpm) / (truck.redlineRpm - truck.idleRpm));
        rpmBarFill.fillAmount = t;
        rpmBarFill.color = t > 0.9f ? redlineColor : normalColor;
    }
}