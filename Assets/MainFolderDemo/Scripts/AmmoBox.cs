using UnityEngine;
using System.Collections;

public class AmmoBox : MonoBehaviour
{
    public float interactDistance = 3f;

    private Transform player;


    [Header("Ammo Box Open VFX")]
    public Transform chestTop;   // Assign this in Inspector
    public float openAngle = -7.73f;
    public float openTime = 1f;
    public GameObject openVFX;   //shine vfx


    private Quaternion closedRotation;
    private bool isOpening = false;

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;

        if (chestTop != null)  
        {
            closedRotation = chestTop.localRotation; //capping the closed rotation. 
        }
    }

    void Update()
    {
        if (player == null || KeybindManager.Instance == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= interactDistance && KeybindManager.Instance.GetKeyDown("Interact"))
        {
            TryRefillWithCost();
        }
    }

    void TryRefillWithCost()
    {
        Weapon current = WeaponManager.ActiveWeapon;
        if (current == null) return;

        // If already full, don’t charge or refill
        bool clipFull = current.GetCurrentAmmo() >= current.clipSize;
        bool reserveFull = current.GetAmmoReserve() >= current.maxReserve;
        if (clipFull && reserveFull) return;

        int cost = GetAmmoCost(current);

        PointManager pm = PointManager.Instance;
        if (pm != null && pm.TrySpend(cost))
        {
            // successful purchase → refill
            RefillCurrentWeaponAmmo(current);
            Debug.Log($"[AmmoBox] Refilled {current.weaponName} for {cost} pts (tier {current.upgradeLevel}).");
            UI.Main?.ShowCenterPopup("Ammo Replenished", Color.white);

        }
        else
        {
            Debug.Log($"[AmmoBox] Not enough points ({cost} needed).");
            // optional: ping UI toast if you want (UI stays in UI by your rule)
            // FindFirstObjectByType<UI>()?.ShowTemporaryPerkMessage("Not enough points");
        }

        if (!isOpening)
        {
            StartCoroutine(OpenChestLid());
        }
    }


    //animation of the lid opening


    //IEnumerator OpenChestLid()
    //{
    //    if (chestTop == null) yield break;

    //    isOpening = true;

    //    // Rotate open
    //    chestTop.localRotation = Quaternion.Euler(openAngle,
    //                                               chestTop.localEulerAngles.y,
    //                                               chestTop.localEulerAngles.z);

    //    // Wait 1 second
    //    yield return new WaitForSeconds(openTime);

    //    // Return to original rotation
    //    chestTop.localRotation = closedRotation;

    //    isOpening = false;
    //}


    IEnumerator OpenChestLid()
    {
        if (chestTop == null) yield break;

        isOpening = true;

        Quaternion openRotation = Quaternion.Euler(
            openAngle,
            closedRotation.eulerAngles.y,
            closedRotation.eulerAngles.z
        );

        float t = 0f;
        float speed = 4f; // opening speed

        // Smooth open
        while (t < 1f)
        {
            t += Time.deltaTime * speed;
            chestTop.localRotation = Quaternion.Lerp(closedRotation, openRotation, t);
            yield return null;
        }

        // Stay open for 1 second
        yield return new WaitForSeconds(openTime);

        t = 0f;

        // Smooth close
        while (t < 1f)
        {
            t += Time.deltaTime * speed;
            chestTop.localRotation = Quaternion.Lerp(openRotation, closedRotation, t);
            yield return null;
        }

        chestTop.localRotation = closedRotation;
        isOpening = false;
    }



    int GetAmmoCost(Weapon w)
    {
        // Map your tiers to costs:
        // base (unpacked) = 750
        // tier 1 = 2500
        // tier 2 = 3500
        // tier 5 = 5000
        // For any tiers not specified (3–4), we’ll charge 4500 by default.
        int lvl = Mathf.Max(0, w.upgradeLevel);
        if (lvl == 0) return 750;
        if (lvl == 1) return 2500;
        if (lvl == 2) return 3500;
        if (lvl >= 3) return 5000;
        return 4500; // tiers 3–4
    }

    void RefillCurrentWeaponAmmo(Weapon current)
    {
        // Refill logic for current weapon only
        int maxClip = current.clipSize;
        int maxReserve = current.maxReserve;

        typeof(Weapon).GetField("currentAmmo",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(current, maxClip);

        typeof(Weapon).GetField("ammoReserve",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(current, maxReserve);

        // If your Weapon has public setters/methods, use those instead of reflection.
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }
}
