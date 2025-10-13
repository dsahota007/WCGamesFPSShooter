using UnityEngine;

public class GrapplingHookBox : MonoBehaviour
{
    [Header("Interact")]
    public float interactDistance = 3f;   // proximity check

    [Header("Cost")]
    public int cost = 2500;

    [Header("Player refs to enable on pickup")]
    public GameObject grappleGunMesh;     // the visual mesh on the player

    private Transform player;
    private GrappleHook playerGrapple;
    private bool used = false;

    // --- exposed state/helpers (optional for UI use) ---
    public bool IsPurchased => used;
    public bool InRange(Transform t) =>
        t != null && Vector3.Distance(transform.position, t.position) <= interactDistance;

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p)
        {
            player = p.transform;
            // find GrappleHook anywhere under the player (even if disabled)
            playerGrapple = p.GetComponentInChildren<GrappleHook>(true);
        }

        // enforce locked state at start (safety)
        if (playerGrapple) playerGrapple.enabled = false;
        if (grappleGunMesh) grappleGunMesh.SetActive(false);
    }

    void Update()
    {
        if (used || player == null || KeybindManager.Instance == null) return;
        if (Vector3.Distance(transform.position, player.position) > interactDistance) return;

        // ONLY your Interact keybind
        if (KeybindManager.Instance.GetKeyDown("Interact"))
        {
            TryPurchase(PointManager.Instance);   // spend points, then enable if success
        }
    }


    public bool TryPurchase(PointManager pm)
    {
        if (used) return true;
        if (pm == null) return false;

        // spend with your PointManager API
        if (!pm.TrySpend(cost)) return false;

        EnableGrapple();
        return true;
    }

    void EnableGrapple()
    {
        if (playerGrapple) playerGrapple.enabled = true;
        if (grappleGunMesh) grappleGunMesh.SetActive(true);

        used = true;

        // tiny feedback (optional)
        CameraScript.Main?.Shake(0.2f, 1.6f, 70f, false);
        UI.Main?.ShowCenterPopup("Grappling Hook Acquired", Color.white);  //new Color(0f, 0f, 0f));

    }


}
