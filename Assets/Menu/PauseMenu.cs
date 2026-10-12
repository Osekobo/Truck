using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pausePanel;
    bool _paused;

    void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
        if (Application.isFocused == false && !_paused) TogglePause();
    }

    public void TogglePause()
    {
        _paused = !_paused;
        if (pausePanel != null) pausePanel.SetActive(_paused);
        Time.timeScale = _paused ? 0f : 1f;
    }

    public void Resume() { TogglePause(); }

    public void QuitToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
