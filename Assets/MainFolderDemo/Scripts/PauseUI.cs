using UnityEngine;
using UnityEngine.UI;

public class PauseUI : MonoBehaviour
{
    [Header("UI - Pause Menu")]
    public GameObject pausePanel;       // assign PausePanel here
    public Button resumeButton;         // assign ResumeButton here

    [Header("UI - Settings")]
    public Toggle vsyncToggle;          // assign Toggle for VSync here
    public Slider fpsSlider;            // assign Slider for FPS cap here
    public Text fpsValueText;           // optional Text to display FPS

    [Header("Settings")]
    public KeyCode toggleKey = KeyCode.Escape; // ESC to toggle
    public bool lockCursorWhenPlaying = true;  // optional nicety
    public int minFPS = 30;
    public int maxFPS = 240;
    public int defaultFPS = 60;

    bool isPaused;

    void Awake()
    {
        // Pause setup
        if (pausePanel != null) pausePanel.SetActive(false);
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        ApplyCursorState(false);

        // Settings setup
        if (vsyncToggle != null)
        {
            vsyncToggle.isOn = (QualitySettings.vSyncCount > 0);
            vsyncToggle.onValueChanged.AddListener(SetVSync);
        }

        if (fpsSlider != null)
        {
            fpsSlider.minValue = minFPS;
            fpsSlider.maxValue = maxFPS;
            fpsSlider.wholeNumbers = true;
            fpsSlider.value = defaultFPS;
            fpsSlider.onValueChanged.AddListener(SetFPS);
        }

        // Apply defaults
        SetVSync(vsyncToggle != null && vsyncToggle.isOn);
        SetFPS(defaultFPS);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    public void Pause()
    {
        if (isPaused) return;
        isPaused = true;

        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);
        ApplyCursorState(true);
        AudioListener.pause = true;
    }

    public void Resume()
    {
        if (!isPaused) return;
        isPaused = false;

        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
        ApplyCursorState(false);
        AudioListener.pause = false;
    }

    void ApplyCursorState(bool paused)
    {
        if (paused)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else if (lockCursorWhenPlaying)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    // === Settings Methods ===
    public void SetVSync(bool enabled)
    {
        QualitySettings.vSyncCount = enabled ? 1 : 0;

        if (enabled)
        {
            Application.targetFrameRate = -1; // let VSync control framerate
        }
        else
        {
            Application.targetFrameRate = Mathf.RoundToInt(
                fpsSlider != null ? fpsSlider.value : defaultFPS
            );
        }
    }

    public void SetFPS(float fps)
    {
        if (QualitySettings.vSyncCount == 0) // only apply when VSync is off
        {
            Application.targetFrameRate = Mathf.RoundToInt(fps);
        }

        if (fpsValueText != null)
        {
            fpsValueText.text = $"{Mathf.RoundToInt(fps)} FPS";
        }
    }
}
