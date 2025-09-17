using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;

public class PauseUI : MonoBehaviour
{
    [Header("UI - Pause Menu")]
    public GameObject pausePanel;
    public Button resumeButton;
    public KeyCode toggleKey = KeyCode.P;
    public bool lockCursorWhenPlaying = true;

    [Header("HUD Roots To Hide When Paused")]
    public GameObject[] hudRoots;

    [Header("PostProcess (optional)")]
    public PostProcessVolume postProcessVolume;
    public Toggle motionBlurToggle, vignetteToggle, ambientOcclusionToggle, grainToggle;
 
    [Header("Frames (optional)")]
    public Toggle vsyncToggle;
    public Slider fpsSlider;
    public Text fpsValueText;
    public int minFPS = 30, maxFPS = 240, defaultFPS = 60;

    [Header("Sensitivity (optional)")]
    public Slider sensitivitySlider;
    public Text sensitivityValueText;
    public float defaultSensitivity = 100f, minSensitivity = 10f, maxSensitivity = 400f;
    private CameraScript cameraScript;

    [Header("FOV (optional)")]
    public Slider fovSlider;
    public Text fovValueText;
    public float defaultFOV = 90f, minFOV = 60f, maxFOV = 120f;

    [Header("Enemy Health Bars (optional)")]
    public Toggle enemyHealthBarToggle;
    public static bool showEnemyHealthBars = true;


    [Header("Mythical Border")]
    public Toggle mythicalBorderToggle;   // drag your Toggle here
    public GameObject mythicalBorder;     // drag the border root (Image’s GameObject) here

    [Header("Control Toggle")]
    public Toggle ControlToggle;         
    public GameObject ControlObject;     



    // ---------------- SIMPLE KEYBIND SECTION ----------------
    [Header("Keybind Buttons + Labels (all Text)")]
    public Text hintText; // one shared hint label (optional)

    public Button jumpBtn; public Text jumpKeyText;
    public Button interactBtn; public Text interactKeyText;
    //public Button backOutBtn; public Text backOutKeyText;
    public Button dashBtn; public Text dashKeyText;
    public Button slideBtn; public Text slideKeyText;
    public Button sprintBtn; public Text sprintKeyText;
    public Button summonBtn; public Text summonKeyText;
    public Button fireBtn; public Text fireKeyText;
    public Button adsBtn; public Text adsKeyText;
    public Button grenadeBtn; public Text grenadeKeyText;
    public Button reloadBtn; public Text reloadKeyText;
    public Button switchBtn; public Text switchKeyText;

    public bool preventDuplicates = true;

    // internal
    private bool isPaused;
    public static bool IsPaused { get; private set; }

    private void Awake()
    {
        // basic pause setup
        if (pausePanel) pausePanel.SetActive(false);
        if (resumeButton) resumeButton.onClick.AddListener(Resume);
        ApplyCursorState(false);

        // optional systems you already had
        cameraScript = FindObjectOfType<CameraScript>();
        if (vsyncToggle) { vsyncToggle.isOn = (QualitySettings.vSyncCount > 0); vsyncToggle.onValueChanged.AddListener(SetVSync); }
        if (fpsSlider) { fpsSlider.minValue = minFPS; fpsSlider.maxValue = maxFPS; fpsSlider.wholeNumbers = true; fpsSlider.value = defaultFPS; fpsSlider.onValueChanged.AddListener(SetFPS); }
        SetVSync(vsyncToggle ? vsyncToggle.isOn : false);
        SetFPS(defaultFPS);

        if (sensitivitySlider)
        {
            sensitivitySlider.minValue = minSensitivity;
            sensitivitySlider.maxValue = maxSensitivity;
            sensitivitySlider.wholeNumbers = true;
            sensitivitySlider.value = defaultSensitivity;
            sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
            SetSensitivity(defaultSensitivity);
        }

        if (fovSlider)
        {
            fovSlider.minValue = minFOV; fovSlider.maxValue = maxFOV; fovSlider.wholeNumbers = true; fovSlider.value = defaultFOV;
            fovSlider.onValueChanged.AddListener(SetFOV);
            SetFOV(defaultFOV);
        }

        if (enemyHealthBarToggle)
        {
            enemyHealthBarToggle.isOn = showEnemyHealthBars;
            enemyHealthBarToggle.onValueChanged.AddListener(ToggleEnemyHealthBars);
        }
        //-- border

        // --- Mythical Border toggle ---
        if (mythicalBorderToggle != null && mythicalBorder != null)
        {
            // default OFF (0)
            int saved = PlayerPrefs.GetInt("MythicalBorderOn", 0);
            bool isOn = saved == 1;

            mythicalBorderToggle.isOn = isOn;
            mythicalBorder.SetActive(isOn);

            mythicalBorderToggle.onValueChanged.AddListener(on =>
            {
                if (mythicalBorder) mythicalBorder.SetActive(on);
                PlayerPrefs.SetInt("MythicalBorderOn", on ? 1 : 0);
            });
        }
        else
        {
            // even if there’s no toggle assigned, start OFF
            if (mythicalBorder) mythicalBorder.SetActive(false);
        }


        // --- On-screen Controls toggle ---
        if (ControlToggle != null && ControlObject != null)
        {
            // default OFF (0)
            bool isOn = PlayerPrefs.GetInt("SimpleToggleOn", 0) == 1;

            ControlToggle.isOn = isOn;
            ControlObject.SetActive(isOn);

            ControlToggle.onValueChanged.AddListener(on =>
            {
                ControlObject.SetActive(on);
                PlayerPrefs.SetInt("SimpleToggleOn", on ? 1 : 0);
            });
        }
        else
        {
            // start OFF if no toggle wired
            if (ControlObject) ControlObject.SetActive(false);
        }



        //------ Keybinds

        // hook up keybind buttons (one line per control)
        Wire("Jump&Slam", jumpBtn, jumpKeyText);
        Wire("Interact", interactBtn, interactKeyText);
        //Wire("BackOutInteract", backOutBtn, backOutKeyText);
        Wire("Dash", dashBtn, dashKeyText);
        Wire("Slide", slideBtn, slideKeyText);
        Wire("Sprint", sprintBtn, sprintKeyText);
        Wire("SummonMagic", summonBtn, summonKeyText);
        Wire("FireWeapon", fireBtn, fireKeyText);
        Wire("AimDownSight", adsBtn, adsKeyText);
        Wire("Grenade", grenadeBtn, grenadeKeyText);
        Wire("Reload", reloadBtn, reloadKeyText);
        Wire("SwitchWeapons", switchBtn, switchKeyText);

        HideHint();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (isPaused) Resume(); else Pause();
        }
    }

    // ---------- Pause ----------
    public void Pause()
    {
        if (isPaused) return;
        isPaused = true; IsPaused = true;

        Time.timeScale = 0f;
        if (pausePanel) pausePanel.SetActive(true);
        ApplyCursorState(true);
        SetHUDVisible(false);

        var cam = FindObjectOfType<CameraScript>();
        if (cam) cam.cameraLocked = true;
    }

    public void Resume()
    {
        if (!isPaused) return;
        isPaused = false; IsPaused = false;

        Time.timeScale = 1f;
        if (pausePanel) pausePanel.SetActive(false);
        ApplyCursorState(false);
        SetHUDVisible(true);

        var cam = FindObjectOfType<CameraScript>();
        if (cam) cam.cameraLocked = false;
        HideHint();
    }

    private void ApplyCursorState(bool paused)
    {
        if (paused) { Cursor.visible = true; Cursor.lockState = CursorLockMode.None; }
        else if (lockCursorWhenPlaying) { Cursor.visible = false; Cursor.lockState = CursorLockMode.Locked; }
    }

    private void SetHUDVisible(bool visible)
    {
        if (hudRoots == null) return;
        foreach (var go in hudRoots) if (go) go.SetActive(visible);
    }

    // ---------- Simple wiring ----------
    private void Wire(string action, Button btn, Text label)
    {
        if (!btn || !label || KeybindManager.Instance == null) return; //gtfo if have nothing

        label.text = KeybindManager.Instance.GetKeyName(action);  //shows the current key (“E”, “Mouse0”, etc.) next to the button
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            StartCoroutine(CaptureAndBind(action, label));
        });
    }

    private IEnumerator CaptureAndBind(string action, Text label)
    {
        if (label) label.text = "Press any key...";
        ShowHint("Press any key, or ESC to cancel");

        // wait one frame so button click doesn't count
        yield return null;

        while (true)
        {
            // cancel
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                label.text = KeybindManager.Instance.GetKeyName(action);
                HideHint();
                yield break;
            }

            // detect any KeyCode down
            foreach (KeyCode kc in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(kc))
                {
                    // keep pause toggle reserved
                    if (kc == toggleKey)
                    {
                        ShowHint("That key is reserved for Pause. Pick another.");
                        yield return null; // keep listening
                        goto ContinueListening;
                    }

                    // duplicate rule
                    if (preventDuplicates && KeybindManager.Instance.IsKeyTaken(kc, action))
                    {
                        ShowHint(kc + " already in use. Pick another.");
                        yield return null;
                        goto ContinueListening;
                    }

                    KeybindManager.Instance.SetKey(action, kc);
                    label.text = kc.ToString();
                    HideHint();
                    yield break;
                }
            }

        ContinueListening:
            yield return null;
        }
    }

    private void ShowHint(string s)
    {
        if (!hintText) return;
        hintText.gameObject.SetActive(true);
        hintText.text = s;
    }

    private void HideHint()
    {
        if (!hintText) return;
        hintText.gameObject.SetActive(false);
    }

    // ---------- Optional settings ----------
    public void SetVSync(bool enabled)
    {
        QualitySettings.vSyncCount = enabled ? 1 : 0;
        if (enabled) Application.targetFrameRate = -1;
        else SetFPS(fpsSlider ? fpsSlider.value : defaultFPS);
    }

    public void SetFPS(float fps)
    {
        if (QualitySettings.vSyncCount == 0)
            Application.targetFrameRate = Mathf.RoundToInt(fps);
        if (fpsValueText) fpsValueText.text = Mathf.RoundToInt(fps) + " FPS";
    }

    public void SetSensitivity(float value)
    {
        if (cameraScript) cameraScript.mouseSensitivity = value;
        if (sensitivityValueText) sensitivityValueText.text = value.ToString("F0");
    }

    public void SetFOV(float value)
    {
        if (cameraScript)
        {
            cameraScript.SetBaseFOV(value);
            if (cameraScript.playerCamera) cameraScript.playerCamera.fieldOfView = value;
        }
        if (fovValueText) fovValueText.text = (value + 30f).ToString("F0");
    }

    public void ToggleEnemyHealthBars(bool enabled)
    {
        showEnemyHealthBars = enabled;
        var all = FindObjectsOfType<EnemyHealthBar>(true);
        foreach (var bar in all) bar.ApplyGlobalVisibility();
    }

    public void SetMythicalBorder(bool on)
    {
        if (mythicalBorder != null)
            mythicalBorder.SetActive(on);

        PlayerPrefs.SetInt("MythicalBorderOn", on ? 1 : 0);
    }

}
