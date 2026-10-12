using UnityEngine;

public class AppBootstrap : MonoBehaviour
{
    void Awake()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        DontDestroyOnLoad(gameObject);
    }
}
