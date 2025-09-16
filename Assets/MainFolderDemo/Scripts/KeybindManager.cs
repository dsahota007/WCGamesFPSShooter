using System.Collections.Generic;
using UnityEngine;

public class KeybindManager : MonoBehaviour
{
    public static KeybindManager Instance;   //make it globally accessible

    private Dictionary<string, KeyCode> keybinds = new Dictionary<string, KeyCode>();  //stores key value pairs so "jump" string wil be SPACE or smthn whatevr the keycode variabel will be 

    void Awake()
    {
        if (Instance == null) 
        { 
            Instance = this; DontDestroyOnLoad(gameObject); 
        }
        else 
        { 
            Destroy(gameObject); return; 
        }

        // Load defaults (or saved ones)
        LoadKey("Jump&Slam", KeyCode.Space);
        LoadKey("Interact", KeyCode.F);
        LoadKey("BackOutInteract", KeyCode.Escape);
        LoadKey("Dash", KeyCode.E);
        LoadKey("Slide", KeyCode.C);
        LoadKey("Sprint", KeyCode.LeftShift);
        LoadKey("SummonMagic", KeyCode.Q);
        LoadKey("FireWeapon", KeyCode.Mouse0);
        LoadKey("AimDownSight", KeyCode.Mouse1);
        LoadKey("Grenade", KeyCode.G);
        LoadKey("Reload", KeyCode.R);
        LoadKey("SwitchWeapons", KeyCode.Alpha1);
    }

    private void LoadKey(string action, KeyCode defaultKey)  //action and than actual keycode
    {
        if (PlayerPrefs.HasKey(action))   //if have key 
            keybinds[action] = (KeyCode)System.Enum.Parse(typeof(KeyCode), //store in dictionary 
                PlayerPrefs.GetString(action));  //read string and convert to keyCode
        else
            keybinds[action] = defaultKey;                  // Use the provided default key.
    }

    public KeyCode GetKey(string action) =>
        keybinds.ContainsKey(action) ? keybinds[action] : KeyCode.None; //whatever action you do make that the keybind

    public void SetKey(string action, KeyCode newKey)  // Change (and save) a keybind
    {
        keybinds[action] = newKey;
        PlayerPrefs.SetString(action, newKey.ToString()); // save so it stays for the next time.
    }

    public bool GetKeyDown(string action) => Input.GetKeyDown(GetKey(action)); //True only on the frame the key is pressed.
    public bool GetKeyHeld(string action) => Input.GetKey(GetKey(action));  // True while key being held.

    public string GetKeyName(string action)
    {
        if (!keybinds.ContainsKey(action)) return "";   //if we dont know action return empty
        return keybinds[action].ToString();   // e.g., "E", "Mouse0", "LeftShift"
    }
    
    //------------- dup checker
    public bool IsKeyTaken(KeyCode key, string exceptAction = null)
    {
        foreach (var kv in keybinds)
            if (kv.Value == key && kv.Key != exceptAction)
                return true;
        return false;
    }
}
