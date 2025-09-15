using UnityEngine;
using System.Collections.Generic;

public class KeybindManager : MonoBehaviour
{
    public static KeybindManager Instance;

    private Dictionary<string, KeyCode> keybinds = new Dictionary<string, KeyCode>();

    void Awake()
    {
        if (Instance == null) 
        { 
            Instance = this; DontDestroyOnLoad(gameObject);
        }

        else 
        { 
            Destroy(gameObject); 
        }

        // Load defaults (or saved ones)
        LoadKey("Jump", KeyCode.Space);
        LoadKey("Slam", KeyCode.Space);
        LoadKey("Interact", KeyCode.F);
        LoadKey("Dash", KeyCode.E);
        LoadKey("Slide", KeyCode.C);
        LoadKey("Sprint", KeyCode.LeftShift);
        LoadKey("SummonMagic", KeyCode.Q);
        LoadKey("FireWeapon", KeyCode.Mouse0);     // Left click
        LoadKey("AimDownSight", KeyCode.Mouse1);   // Right click
        LoadKey("Grenade", KeyCode.G);
        LoadKey("Reload", KeyCode.R);
        LoadKey("SwitchWeapons", KeyCode.Alpha1);

    }

    private void LoadKey(string action, KeyCode defaultKey)
    {
        if (PlayerPrefs.HasKey(action))
        {
            keybinds[action] = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString(action));
        }

        else 
        { 
            keybinds[action] = defaultKey; 
        }
    }

    public KeyCode GetKey(string action) => keybinds.ContainsKey(action) ? keybinds[action] : KeyCode.None;

    public void SetKey(string action, KeyCode newKey)
    {
        keybinds[action] = newKey;
        PlayerPrefs.SetString(action, newKey.ToString()); // save
    }

    public bool GetKeyDown(string action) => Input.GetKeyDown(GetKey(action));
    public bool GetKeyHeld(string action) => Input.GetKey(GetKey(action));
}
