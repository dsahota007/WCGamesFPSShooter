using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

public class PauseUI : MonoBehaviour
{
    [Header("Chaos Mode")]
    public Button chaosButton;        // assign your UI button here
    public GameObject chaosObject;    // assign the hidden object here


    //--------------------------------------------



    [Header("UI - Pause Menu")]
    public GameObject pausePanel;       // assign PausePanel here
    public Button resumeButton;         // assign ResumeButton here
    bool isPaused;

    [Header("UI")]
    public KeyCode toggleKey = KeyCode.Escape; // ESC to toggle
    public bool lockCursorWhenPlaying = true;  // optional nicety

    [Header("Frames")]
    public Toggle vsyncToggle;          // assign Toggle for VSync here
    public Slider fpsSlider;            // assign Slider for FPS cap here
    public Text fpsValueText;           // optional Text to display FPS
    public int minFPS = 30;
    public int maxFPS = 240;
    public int defaultFPS = 60;

 
    [Header("UI - Sensitivity")]
    public Slider sensitivitySlider;   // drag your UI slider here
    public Text sensitivityValueText;  // optional text to display current sens
    public float defaultSensitivity = 100f;
    public float minSensitivity = 10f;
    public float maxSensitivity = 400f;
    private CameraScript cameraScript;

    [Header("UI - FOV")]
    public Slider fovSlider;           // NEW: drag in FOV slider
    public Text fovValueText;          // optional, shows FOV number
    public float defaultFOV = 90f;
    public float minFOV = 60f;
    public float maxFOV = 120f;

    [Header("PostProcess")]
    public Toggle motionBlurToggle;
    public Toggle vignetteToggle;
    public Toggle ambientOcclusionToggle;
    public Toggle grainToggle;
    public PostProcessVolume postProcessVolume;

    private MotionBlur motionBlur;
    private Vignette vignette;
    private AmbientOcclusion ambientOcclusion;
    private Grain grain;

    [Header("UI - Enemy Health Bars")]
    public Toggle enemyHealthBarToggle; // assign in inspector
    public static bool showEnemyHealthBars = true;


    //--------------------------------------------
    public void EnableChaos()
    {
        if (chaosObject != null)
        {
            chaosObject.SetActive(true);
            Debug.Log("[PauseUI] CHAOS ENABLED!");
        }
    }

    //--------------------------------------------
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

        cameraScript = FindObjectOfType<CameraScript>();

        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = minSensitivity;
            sensitivitySlider.maxValue = maxSensitivity;
            sensitivitySlider.wholeNumbers = true;
            sensitivitySlider.value = defaultSensitivity;
            sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
        }

        SetSensitivity(defaultSensitivity);

        // FOV
        if (fovSlider != null)
        {
            fovSlider.minValue = minFOV;
            fovSlider.maxValue = maxFOV;
            fovSlider.wholeNumbers = true;
            fovSlider.value = defaultFOV;
            fovSlider.onValueChanged.AddListener(SetFOV);
        }
        SetFOV(defaultFOV);

        // Grab all post-processing overrides from the profile -------------------
        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
            postProcessVolume.profile.TryGetSettings(out motionBlur);
            postProcessVolume.profile.TryGetSettings(out vignette);
            postProcessVolume.profile.TryGetSettings(out ambientOcclusion);
            postProcessVolume.profile.TryGetSettings(out grain);
        }

        // --- Hook UI Toggles ---
        if (motionBlurToggle != null && motionBlur != null)
        {
            motionBlurToggle.isOn = motionBlur.active;
            motionBlurToggle.onValueChanged.AddListener(val => motionBlur.active = val);
        }

        if (vignetteToggle != null && vignette != null)
        {
            vignetteToggle.isOn = vignette.active;
            vignetteToggle.onValueChanged.AddListener(val => vignette.active = val);
        }

        if (ambientOcclusionToggle != null && ambientOcclusion != null)
        {
            ambientOcclusionToggle.isOn = ambientOcclusion.active;
            ambientOcclusionToggle.onValueChanged.AddListener(val => ambientOcclusion.active = val);
        }

        if (grainToggle != null && grain != null)
        {
            grainToggle.isOn = grain.active;
            grainToggle.onValueChanged.AddListener(val => grain.active = val);
        }

        //-----------------------------------------

        if (enemyHealthBarToggle != null)
        {
            enemyHealthBarToggle.isOn = showEnemyHealthBars;
            enemyHealthBarToggle.onValueChanged.AddListener(ToggleEnemyHealthBars);
        }
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
        //AudioListener.pause = true;

        CameraScript cam = FindObjectOfType<CameraScript>();
        if (cam != null) cam.cameraLocked = true;
    }

    public void Resume()
    {
        if (!isPaused) return;
        isPaused = false;

        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
        ApplyCursorState(false);
        //AudioListener.pause = false;

        CameraScript cam = FindObjectOfType<CameraScript>();
        if (cam != null) cam.cameraLocked = false;
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
    public void SetSensitivity(float value)
    {
        if (cameraScript != null)
            cameraScript.mouseSensitivity = value;

        if (sensitivityValueText != null)
            sensitivityValueText.text = value.ToString("F0");
    }

    public void SetFOV(float value)
    {
        if (cameraScript != null)
        {
            cameraScript.SetBaseFOV(value);  // updates default + sprint FOV
            if (cameraScript.playerCamera != null)
                cameraScript.playerCamera.fieldOfView = value; // update instantly
        }

        if (fovValueText != null)
            fovValueText.text = (value + 30f).ToString("F0");  //bc 120 is high so it gives the player the feel
    }

    public void ToggleEnemyHealthBars(bool enabled)
    {
        showEnemyHealthBars = enabled;

        // Update all active health bars
        var allBars = FindObjectsOfType<EnemyHealthBar>(true); // true = include inactive
        foreach (var bar in allBars)
            bar.ApplyGlobalVisibility();
    }
}
