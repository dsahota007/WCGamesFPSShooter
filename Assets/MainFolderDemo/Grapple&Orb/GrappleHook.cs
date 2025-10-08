using UnityEngine;
using System.Collections;
using Mono.Cecil.Cil;

[RequireComponent(typeof(CharacterController))]
public class GrappleHook : MonoBehaviour
{
    [Header("Refs")]
    public PlayerMovement playerMovement;      
    public ArmMovementMegaScript arm;
    public ArmMagicSpell armMagic;
    public Weapon weapon;
    public Transform cam;                      
    public CharacterController controller;     
    public Transform grappleTip;              
    public LineRenderer rope;                  

    [Header("Raycast")]
    public LayerMask grappleMask;
    public float maxGrappleDistance = 35f;

    [Header("Pull")]
    public float pullAcceleration = 60f;       
    public float maxPullSpeed = 18f;         
    public float gravityWhileGrappling = -2f;  

    [Header("Input/Timing")]
    public float fireCooldown = 0.2f;         
    public bool holdToGrapple = true;          

    [Header("Rules")]
    public bool blockStartWhileSprinting = true;
    public bool respectPause = true;

    [Header("Rope Style (tip flight + wobble)")]
    public float tipTravelSpeed = 80f;
    public float springK = 22f;
    public float springDamping = 6f;
    public int ropeSegments = 10;
    public float waveAmplitude = 0.10f;
    public float waveFrequency = 18f;

    [Header("Rope Settle")]
    public float settleSpeed = 6f;

    [Header("Release Momentum")]
    public float releaseMaxSeconds = 2.0f;     // cap coasting
    public bool stopCoastWhenGrounded = true;  // end on land
    public float coastFriction = 0.6f;         // lerp factor per second

    // runtime
    private bool isGrappling = false;
    private Vector3 anchor;
    private Vector3 pullVelocity;
    private float nextFireTime = 0f;

    // rope state
    private Vector3 tipWorld;
    private float flyT = 0f;          // 0→1 during tip flight
    private float springVel = 0f;     // small spring wobble
    private float anchoredTime = 0f;  // time after tip arrived

    // post-release coasting (horizontal)
    private Coroutine coastRoutine;
    private Vector3 coastVelocity = Vector3.zero;

    [Header("Release Tuning")]
    public float releaseUpScale = 0.25f; // how much of your upward speed you keep
    public float releaseUpMax = 6f;    // cap the upward pop

    [Header("Air Control")]
    public float airControl = 4f; // higher = turns faster while coasting


    void Reset()
    {
        controller = GetComponent<CharacterController>();  //controller
        if (rope)   
        {    
            rope.useWorldSpace = true; 
            rope.positionCount = 0; //grapple must be 0
        }

    }

    void Update()
    {
        if (respectPause && PauseUI.IsPaused) return; // if game is paused then leave

        bool pressBtn = KeybindManager.Instance && KeybindManager.Instance.GetKeyDown("Grapple");  //this is that we have clicked it
        bool releaseBtn = Input.GetKeyUp(KeybindManager.Instance.GetKey("Grapple")); //when we release the key to let go of grappling

        if (!isGrappling && pressBtn && Time.time >= nextFireTime && CanStartGrapple()) //nextFireTime is 0 and canStart grapple all this needs to be met
            TryLatch();

        if (isGrappling)
        {
            TickPull();

            if (holdToGrapple && releaseBtn)
                StopAndBeginCoast();
        }

        UpdateRopeVisual();
    }

    bool CanStartGrapple()
    {
        // BLOCK OFF
        if (KeybindManager.Instance != null && KeybindManager.Instance.GetKeyHeld("AimDownSight")) return false; 
        if (arm)
        {
            if (arm.IsGrenadeAnimating) return false;
            if (arm.DrinkingPerk) return false;
            if (arm.IsCasting) return false;   //we just added a getter in armScipt
            if (arm.isReloading) return false;
        }
        //if (armMagic != null && armMagic.IsCasting()) return false;
        if (playerMovement && playerMovement.IsSprinting()) return false;
        if (KeybindManager.Instance && KeybindManager.Instance.GetKeyHeld("Sprint") && controller && controller.velocity.magnitude > 0.1f) return false;

        //if (blockStartWhileSprinting)
        //{
        //    if (playerMovement && playerMovement.IsSprinting()) return false;
        //    if (KeybindManager.Instance &&
        //        KeybindManager.Instance.GetKeyHeld("Sprint") &&
        //        controller && controller.velocity.magnitude > 0.1f)
        //        return false;
        //}

        return true;
    }


    void TryLatch()
    {
        if (!cam) return;

        if (Physics.Raycast(new Ray(cam.position, cam.forward),
                            out var hit, maxGrappleDistance, grappleMask,
                            QueryTriggerInteraction.Ignore))
        {
            anchor = hit.point;
            isGrappling = true;
            nextFireTime = Time.time + fireCooldown;
            pullVelocity = Vector3.zero;

            // rope head starts at hand and flies to anchor
            tipWorld = grappleTip ? grappleTip.position : transform.position + Vector3.up * 1.4f;
            flyT = 0f; springVel = 0f; anchoredTime = 0f;
            if (rope) rope.positionCount = Mathf.Max(2, ropeSegments);

            // raise and keep the hand up
            if (arm) arm.BeginGrappleHold();

            // cancel any previous coast
            if (coastRoutine != null) StopCoroutine(coastRoutine);
            coastVelocity = Vector3.zero;
        }
    }

    void TickPull()
    {
        float dt = Time.deltaTime; if (dt <= 0f) return;

        Vector3 toAnchor = anchor - transform.position;
        if (toAnchor.sqrMagnitude < 0.0001f) { StopAndBeginCoast(); return; }

        Vector3 dir = toAnchor.normalized;

        // accelerate toward anchor
        pullVelocity += dir * (pullAcceleration * dt);
        pullVelocity = Vector3.ClampMagnitude(pullVelocity, maxPullSpeed);

        // optional tiny gravity drift while attached
        Vector3 move = pullVelocity * dt;
        if (gravityWhileGrappling != 0f) move += Vector3.up * (gravityWhileGrappling * dt);

        controller.Move(move);
    }

    void StopAndBeginCoast()
    {
        if (!isGrappling) return;

        isGrappling = false;

        // --- tame Y at release ---
        // keep only a *small, capped* portion of your upward speed
        float up = Mathf.Max(0f, pullVelocity.y) * releaseUpScale;
        if (playerMovement != null)
            playerMovement.AddUpwardVelocity(Mathf.Min(up, releaseUpMax));

        // --- keep horizontal carry exactly as-is ---
        coastVelocity = new Vector3(pullVelocity.x, 0f, pullVelocity.z);

        // clear grapple motion/visuals
        pullVelocity = Vector3.zero;
        if (arm) arm.EndGrappleHold();
        ClearRope();

        if (coastRoutine != null) StopCoroutine(coastRoutine);
        coastRoutine = StartCoroutine(CoastMomentum());
    }


    IEnumerator CoastMomentum()
    {
        float t = 0f;
        while (t < releaseMaxSeconds)
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) { yield return null; continue; }

            // stop on landing (and shake once)
            if (stopCoastWhenGrounded && controller.isGrounded)
            {
                CameraScript.Main?.Shake(0.5f, 2.5f, 55f, false);
                break;
            }

            // --- AIR CONTROL: steer coast toward current input ---
            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");
            if (Mathf.Abs(x) + Mathf.Abs(z) > 0f)
            {
                Vector3 inputDir = (transform.right * x + transform.forward * z).normalized;
                float speed = coastVelocity.magnitude;          // keep your current speed
                Vector3 target = inputDir * speed;              // where you want to steer
                coastVelocity = Vector3.Lerp(coastVelocity, target, airControl * dt);
            }

            // move using the (possibly steered) coast velocity (horizontal only)
            Vector3 horiz = new Vector3(coastVelocity.x, 0f, coastVelocity.z);
            controller.Move(horiz * dt);

            // decay speed over time
            coastVelocity = Vector3.Lerp(coastVelocity, Vector3.zero, dt * coastFriction);

            t += dt;
            yield return null;
        }

        coastVelocity = Vector3.zero;
        coastRoutine = null;
    }

    // external interruption (e.g., dash)
    public void InjectDash(Vector3 dashVelocity, bool cutRope)
    {
        if (cutRope && isGrappling) StopAndBeginCoast();

        if (coastRoutine != null) StopCoroutine(coastRoutine);
        coastVelocity = dashVelocity;
        coastRoutine = StartCoroutine(CoastMomentum());
    }

    // ------------- rope visual (flight + spring + settle) -------------
    void UpdateRopeVisual()
    {
        if (!rope) return;

        if (!isGrappling) { ClearRope(); return; }

        // --- ORIGIN = FIREPOINT ---
        Vector3 firepoint = grappleTip ? grappleTip.position : transform.position + Vector3.up * 1.4f;

        // 1) tip flight (head flies from firepoint to anchor)
        float dist = Vector3.Distance(firepoint, anchor);
        float travel = tipTravelSpeed * Time.deltaTime;
        flyT = Mathf.Clamp01(flyT + (travel / Mathf.Max(0.01f, dist)));
        tipWorld = Vector3.Lerp(firepoint, anchor, flyT);

        // 2) settle
        if (flyT < 1f) anchoredTime = 0f;
        else anchoredTime += Time.deltaTime;
        float settle = 1f - Mathf.Exp(-settleSpeed * Mathf.Max(0f, anchoredTime));

        // 3) tiny spring wobble
        float targetLen = Vector3.Distance(firepoint, anchor);
        float currentLen = Vector3.Distance(firepoint, tipWorld);
        float x = currentLen - targetLen;
        float extraDamping = (flyT >= 1f) ? settleSpeed : 0f;
        springVel += (-springK * x - (springDamping + extraDamping) * springVel) * Time.deltaTime;
        float lengthOffset = springVel * 0.02f * (1f - settle);
        Vector3 tipWithSpring = Vector3.Lerp(firepoint, tipWorld, 1f + lengthOffset);

        // 4) draw from firepoint outward
        int N = Mathf.Max(2, ropeSegments);
        rope.useWorldSpace = true;
        rope.positionCount = N;

        // force the origin to be the firepoint
        rope.SetPosition(0, firepoint);

        Vector3 fwd = (anchor - firepoint).sqrMagnitude > 0.0001f ? (anchor - firepoint).normalized : transform.forward;
        Vector3 side = Vector3.Cross(Vector3.up, fwd).normalized;

        for (int i = 1; i < N; i++)
        {
            float t = i / (float)(N - 1);
            Vector3 p = Vector3.Lerp(firepoint, tipWithSpring, t);

            if (waveAmplitude > 0f)
            {
                float flyFade = (flyT < 1f) ? (1f - flyT) : 0f;
                float wave = Mathf.Sin(Time.time * waveFrequency + t * Mathf.PI * 2f)
                           * waveAmplitude * Mathf.Max(0f, flyFade) * (1f - settle);
                p += side * wave;
            }

            rope.SetPosition(i, p);
        }
    }


    void ClearRope()
    {
        if (rope) rope.positionCount = 0;
    }

    //bool IsGrappleKeyUp()   ---took this out and made release variable into one line
    //{
    //    if (!KeybindManager.Instance) return false;  
    //    var code = KeybindManager.Instance.GetKey("Grapple"); //get grapple key
    //    return code != KeyCode.None && Input.GetKeyUp(code); //return true if it is a key and its grapple  (GetKeyUp THIS IS THE RELEASE
    //}

    public bool IsGrappling => isGrappling;
}