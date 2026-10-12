using UnityEngine;
using TMPro;

public class MissionManager : MonoBehaviour
{
    [Header("References")]
    public TruckController truck;
    public Transform pickupZone;
    public Transform[] destinationZones;

    [Header("HUD")]
    public TextMeshProUGUI missionText;
    public TextMeshProUGUI moneyText;
    public DeliveryFeedback deliveryFeedback;
    public TruckAudio truckAudio;

    [Header("Tuning")]
    public int rewardPerMission = 250;
    public int startingMoney = 0;

    [Header("Runtime (read only)")]
    public bool missionActive;
    public int missionsCompleted;

    int _money;
    int _currentDestinationIndex = -1;
    bool _waitingAtPickup = true;
    float _lastZoneEnterTime = -999f;

    void Start()
    {
        if (pickupZone == null)
        {
            Debug.LogWarning("[MissionManager] No pickupZone assigned.");
            return;
        }

        // Start in the delivery phase so the player drives straight to a destination.
        _money = PlayerPrefs.GetInt("money", startingMoney);
        UpdateMoneyHUD();
        StartDeliveryPhase();
        DeliveryZone.OnTruckEntered += HandleZoneEntered;
    }

    void OnDestroy()
    {
        DeliveryZone.OnTruckEntered -= HandleZoneEntered;
    }

    void StartPickupPhase()
    {
        _waitingAtPickup = true;
        missionActive = false;
        UpdateHUD("Return to pickup zone");
    }

    void StartDeliveryPhase()
    {
        if (destinationZones == null || destinationZones.Length == 0)
        {
            Debug.LogWarning("[MissionManager] No destination zones assigned.");
            return;
        }

        // Pick a random destination, different from the last one.
        int next = _currentDestinationIndex;
        if (destinationZones.Length > 1)
        {
            while (next == _currentDestinationIndex)
                next = Random.Range(0, destinationZones.Length);
        }
        else
        {
            next = 0;
        }
        _currentDestinationIndex = next;

        var zoneComp = destinationZones[next].GetComponent<DeliveryZone>();
        string zoneName = zoneComp != null ? zoneComp.zoneName : "Destination";

        _waitingAtPickup = false;
        missionActive = true;
        UpdateHUD($"Deliver to {zoneName}");
    }

    void HandleZoneEntered(DeliveryZone zone)
    {
        if (Time.time - _lastZoneEnterTime < 1f) return;
        _lastZoneEnterTime = Time.time;

        if (!missionActive) return;

        if (_currentDestinationIndex < 0 || _currentDestinationIndex >= destinationZones.Length)
            return;

        if (zone.transform == destinationZones[_currentDestinationIndex])
        {
            CompleteMission();
        }
    }

    void CompleteMission()
    {
        missionActive = false;
        missionsCompleted++;
        _money += rewardPerMission;
        PlayerPrefs.SetInt("money", _money);
        PlayerPrefs.Save();
        UpdateMoneyHUD();
        UpdateHUD($"Delivered! ({missionsCompleted})");
        if (deliveryFeedback != null) deliveryFeedback.Show(rewardPerMission);
        if (truckAudio != null) truckAudio.PlayDeliveryDing();
        Invoke(nameof(StartDeliveryPhase), 2f);
    }

    void UpdateHUD(string text)
    {
        if (missionText != null) missionText.text = text;
    }

    void UpdateMoneyHUD()
    {
        if (moneyText != null) moneyText.text = "$" + _money.ToString();
    }

    public void ResetMoney()
    {
        _money = startingMoney;
        PlayerPrefs.SetInt("money", _money);
        PlayerPrefs.Save();
        UpdateMoneyHUD();
    }
}
