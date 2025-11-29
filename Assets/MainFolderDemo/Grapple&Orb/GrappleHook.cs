using UnityEngine;
using System.Collections;
//using Mono.Cecil.Cil;

[RequireComponent(typeof(CharacterController))]
public class GrappleHook : MonoBehaviour
{
    [Header("Refs")]
    public PlayerMovement playerMovement;      
    public ArmMovementMegaScript arm;
    //public ArmMagicSpell armMagic;
    //public Weapon weapon;
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
    public float releaseDistance = 20f;
    //public bool stopCoastWhenGrounded = true;  // end on land
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
        {
            TryLatch();
        }

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
        if (!cam) return;  //need cam

        //physics.Raycast (origin, direction, maxDistacne, layerMask, QueryTriggerInteractoin)
        //Ray is a struct with two fields: origin and direction.    ---  new Ray(origin, direction)


        if (Physics.Raycast(new Ray(cam.position, cam.forward),out var hit, maxGrappleDistance, grappleMask,QueryTriggerInteraction.Ignore))
        {
            anchor = hit.point;  //hook point of the world when we HIT !!!
            isGrappling = true;
            nextFireTime = Time.time + fireCooldown;   //with this we can control how many sedconds we want to add so we can fire again.
            pullVelocity = Vector3.zero;

            // rope head starts at hand and flies to anchor
            tipWorld = grappleTip ? grappleTip.position : transform.position + Vector3.up * 1.4f;  //set spawnPoint as gun tip and if we dont have that fake a point above the player

            //if (grappleTip != null) // or: if (grappleTip != null)
            //{
            //    tipWorld = grappleTip.position;   //the guns TRANSFORM is the origin. 
            //}
            //else
            //{
            //    tipWorld = transform.position + Vector3.up * 1.4f;  //fake a point above player if no grapple point
            //}   ---another way to write the line above

            flyT = 0f;          //we start the rope tip travel progress to 0
            springVel = 0f;     //stop any wobble or string motion
            anchoredTime = 0f;  //reset how long I have been attached back to 0 

            if (rope)
            {
                rope.positionCount = Mathf.Max(2, ropeSegments);   //we get maxiumum back so 2 is lowest and than we get back whatever rope segments we have.
            }

            // raise and keep the hand up
            if (arm)
            {
                arm.BeginGrappleHold();   //begin holding
            }
            // cancel any previous coast
            if (coastRoutine != null)  ///-----------------------------------------------------------------------------------COEMBACK TO THIS
            {
                StopCoroutine(coastRoutine);
                coastVelocity = Vector3.zero;
            }
        }
    }

    void TickPull()
    {
        float dt = Time.deltaTime; 
        if (dt <= 0f) return;  //Get frame time; if paused/frozen, do nothing.

        Vector3 toAnchor = anchor - transform.position;   //anchor is the hitPoint so we get that and player distance
        
        //THIS IS FOR RELASE  
        if (toAnchor.sqrMagnitude < releaseDistance)    
        {
            StopAndBeginCoast(); return;
        }

        Vector3 dir = toAnchor.normalized;  //get the direction

        // accelerate toward anchor
        pullVelocity += dir * (pullAcceleration * dt);  //how much speed to add to this frame
        pullVelocity = Vector3.ClampMagnitude(pullVelocity, maxPullSpeed);   //if that velocity gets too big, limit it to maxPullSpeed

        // optional tiny gravity drift while attached  --- took this out
        Vector3 move = pullVelocity * dt; 
        //if (gravityWhileGrappling != 0f)
        //{
        //    move += Vector3.up * (gravityWhileGrappling * dt);
        //}

        controller.Move(move);  //this is now physically moving the player now. NO TP were jus nudging the player in that direction.
    }

    void StopAndBeginCoast()
    {
        if (!isGrappling) return;
        isGrappling = false;

        // --------------------------- tame Y at release and also ADDING Y phyiscs upon release --------------------------
        // keep only a *small, capped* portion of your upward speed
        float up = Mathf.Max(0f, pullVelocity.y) * releaseUpScale;  //max so never below 0 and shrink it down to percent so we dont rocket upwards
        if (playerMovement != null)
        {
            playerMovement.AddUpwardVelocity(Mathf.Min(up, releaseUpMax));  //we are adding + capping y release so we dont go flying up
        }

        // --- keep horizontal carry exactly as-is ---
        coastVelocity = new Vector3(pullVelocity.x, 0f, pullVelocity.z);  //Zero out Y so the “coast” is horizontal only.

        // clear grapple motion/visuals
        pullVelocity = Vector3.zero;   //no more pull

        if (arm)
        {
            arm.EndGrappleHold();  //get rid of animaitons
        }
        
        ClearRope();  //no rope

        if (coastRoutine != null)   //------------------------COME BACAK TO THIS 
        {
            StopCoroutine(coastRoutine);
        }

        coastRoutine = StartCoroutine(CoastMomentum());
    }


    IEnumerator CoastMomentum()  //run this frame by frame 
    {
        float timer = 0f; // start a timer
        while (timer < releaseMaxSeconds)   //keep glidingn until we hit max glide time 
        {
            float dt = Time.deltaTime;   
            if (dt <= 0f)    //how much time ahas passed this frame if pasued or frozen than wait a frame and try again
            { 
                yield return null; 
                continue; 
            }

            // stop on landing (and shake once)
            //if (stopCoastWhenGrounded && controller.isGrounded)  
            if (controller.isGrounded)  //we do this for camera shake
            {
                CameraScript.Main?.Shake(0.5f, 2.5f, 55f, true);
                break;
            }

            // --- AIR CONTROL: steer coast toward current input ---
            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");
            if (Mathf.Abs(x) + Mathf.Abs(z) > 0f)  //we get abs so we dont get neg numbers
            {
                Vector3 inputDir = (transform.right * x + transform.forward * z).normalized;   //right is left/right adn forward is back adn foruth -- find direction of this (find direciot of all movements 
                float speed = coastVelocity.magnitude;          // keep your current speed -- magnitude is the length of the vector 
                Vector3 target = inputDir * speed;              // where you want to steer  
                coastVelocity = Vector3.Lerp(coastVelocity, target, airControl * dt);  //(a,b,t)
            }

            // move using the (possibly steered) coast velocity (horizontal only)
            Vector3 horiz = new Vector3(coastVelocity.x, 0f, coastVelocity.z);   //Take your glide velocity but remove any up/down (Y=0). So it’s purely horizontal.
            controller.Move(horiz * dt);  //Move the CharacterController this frame by that horizontal distance.
            // decay speed over time
            coastVelocity = Vector3.Lerp(coastVelocity, Vector3.zero, dt * coastFriction);

            timer += dt;
            yield return null;   //Wait one frame, then loop again.
        }

        coastVelocity = Vector3.zero;
        coastRoutine = null;
    }

    // external interruption (e.g., dash)
    public void InjectDash(Vector3 dashVelocity, bool cutRope)
    {
        if (cutRope && isGrappling)
        {
            StopAndBeginCoast();
        }
        
        if (coastRoutine != null)
        {
            StopCoroutine(coastRoutine);
        }

        coastVelocity = dashVelocity;
        coastRoutine = StartCoroutine(CoastMomentum());
    }

    // ------------- rope visual (flight + spring + settle) ------------- wave frequnecy style
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

    // --- Cooldown helpers ---
    public bool IsGrappleReady()
    {
        return Time.time >= nextFireTime && !isGrappling;  //if we are exceeded cooldown so we can shoot adn were not already grappling 
    }

    public float GetCooldownProgress01()
    {
        if (fireCooldown <= 0f) return 1f;

        // time left until we can fire again
        float remaining = Mathf.Max(0f, nextFireTime - Time.time);
        float p = 1f - (remaining / fireCooldown);

        // optional: if you prefer "busy" look while attached, clamp to 0 while grappling
        // if (isGrappling) return 0f;

        return Mathf.Clamp01(p);
    }

    public float GetCooldownSecondsRemaining()
    {
        return Mathf.Max(0f, nextFireTime - Time.time);
    }

}