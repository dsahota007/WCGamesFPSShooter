using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class GrappleHook : MonoBehaviour
{
    [Header("Refs")]
    public PlayerMovement playerMovement;      // for sprint check + momentum carry
    public ArmMovementMegaScript arm;          // to block + pose the arm
    public Transform cam;                      // camera for ray
    public CharacterController controller;     // same CC as player
    public Transform grappleTip;               // rope start (hand/muzzle)
    public LineRenderer rope;                  // optional line renderer

    [Header("Raycast")]
    public LayerMask grappleMask;
    public float maxGrappleDistance = 35f;

    [Header("Pull")]
    public float pullAcceleration = 60f;       // acceleration toward anchor
    public float maxPullSpeed = 18f;           // clamp speed
    public float gravityWhileGrappling = -2f;  // small down drift while attached

    [Header("Input/Timing")]
    public float fireCooldown = 0.2f;          // delay between shots
    public bool holdToGrapple = true;          // release to stop

    [Header("Rules")]
    public bool blockStartWhileSprinting = true;
    public bool respectPause = true;

    // runtime
    private bool isGrappling = false;
    private Vector3 anchor;
    private Vector3 pullVelocity;
    private float nextFireTime = 0f;

    void Reset()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (respectPause && PauseUI.IsPaused)
        {
            // if the game pauses, just stop visuals (don’t forcibly unhook movement by design)
            UpdateRope();
            return;
        }

        bool press = KeybindManager.Instance != null && KeybindManager.Instance.GetKeyDown("Grapple");
        bool release = IsGrappleKeyUp();

        // Try start
        if (!isGrappling && press && Time.time >= nextFireTime && CanStartGrapple())
            TryLatch();

        // While latched: pull
        if (isGrappling)
        {
            TickPull();

            // let go if using hold-to-grapple
            if (holdToGrapple && release)
                StopAndCarryMomentum();
        }

        UpdateRope();
    }

    bool CanStartGrapple()
    {
        // block by arm states
        if (arm != null)
        {
            if (arm.IsGrenadeAnimating) return false;
            if (arm.DrinkingPerk) return false;
            if (arm.IsGrappleAnimating) return false; // avoids fighting the arm pose transition
        }

        // optional: don't start while sprinting
        if (blockStartWhileSprinting)
        {
            if (playerMovement != null && playerMovement.IsSprinting()) return false;

            if (KeybindManager.Instance != null &&
                KeybindManager.Instance.GetKeyHeld("Sprint") &&
                controller != null && controller.velocity.magnitude > 0.1f)
                return false;
        }

        return true;
    }

    void TryLatch()
    {
        if (cam == null) return;

        Ray r = new Ray(cam.position, cam.forward);
        if (Physics.Raycast(r, out var hit, maxGrappleDistance, grappleMask, QueryTriggerInteraction.Ignore))
        {
            anchor = hit.point;
            isGrappling = true;
            nextFireTime = Time.time + fireCooldown;
            pullVelocity = Vector3.zero;

            // hold the arm up while latched
            if (arm != null) arm.BeginGrapplePose();
        }
        else
        {
            // optional: UI "no latch" feedback
        }
    }

    void TickPull()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // direction toward anchor
        Vector3 toAnchor = anchor - transform.position;
        if (toAnchor.sqrMagnitude < 0.0001f)
        {
            StopAndCarryMomentum();
            return;
        }
        Vector3 dir = toAnchor.normalized;

        // integrate pull + light gravity
        pullVelocity += dir * (pullAcceleration * dt);
        pullVelocity += Vector3.up * (gravityWhileGrappling * dt);

        // clamp speed
        float spd = pullVelocity.magnitude;
        if (spd > maxPullSpeed) pullVelocity = pullVelocity.normalized * maxPullSpeed;

        // apply via CharacterController (additive to your normal movement)
        controller.Move(pullVelocity * dt);
    }

    void StopAndCarryMomentum()
    {
        isGrappling = false;

        // hand momentum to PlayerMovement so we “keep going” like a booster
        if (playerMovement != null)
            playerMovement.ApplyExternalForce(pullVelocity);

        pullVelocity = Vector3.zero;

        // drop the arm back down
        if (arm != null) arm.EndGrapplePose();

        ClearRope();
    }

    void UpdateRope()
    {
        if (!rope)
            return;

        if (!isGrappling)
        {
            ClearRope();
            return;
        }

        rope.positionCount = 2;
        Vector3 tip = grappleTip ? grappleTip.position : transform.position + Vector3.up * 1.4f;
        rope.SetPosition(0, tip);
        rope.SetPosition(1, anchor);
    }

    void ClearRope()
    {
        if (rope) rope.positionCount = 0;
    }

    // helper: KeyUp using your KeybindManager
    bool IsGrappleKeyUp()
    {
        if (KeybindManager.Instance == null) return false;
        var code = KeybindManager.Instance.GetKey("Grapple");
        if (code == KeyCode.None) return false;
        return Input.GetKeyUp(code);
    }

    // Optional read-only
    public bool IsGrappling => isGrappling;
}
