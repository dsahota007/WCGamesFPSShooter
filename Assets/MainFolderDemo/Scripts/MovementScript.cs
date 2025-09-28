//using Unity.Burst.Intrinsics;
//using Unity.VisualScripting;
//using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

//using static UnityEditor.Experimental.GraphView.GraphView;
//using static UnityEditorInternal.ReorderableList;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Run/Jump Settings")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 9f;
    public float gravity = -9.81f;
    public float jumpHeight = 2f;
    public float aimSpeed = 3.5f;

    private float _baseWalkSpeed;
    private float _baseSprintSpeed;


    [Header("Slide Settings")]
    public float slideSpeed = 12f;
    public float slideDuration = 1f;
    public float slideDeceleration = 5f;
    public float slideControllerHeight = 1f;   // Height during slide


    [Header("Kinetic Jump & Slam Settings")]
    public float KineticJumpForce = 12f;
    public float slamDownForce = -50f; // How fast you fall
    public float slamCooldown = 10f;

    private bool isKineticJump = false;
    public float minSlideTimeForKineticJump = 0.5f;
    //public bool CanKineticJumpNow => isGrounded && isSliding && (slideTimer >= minSlideTimeForKineticJump);  //this is a getter for UI 
    private bool isSlamming = false;
    private float lastSlamTime;
    public bool IsKineticJumping => isKineticJump;  //this is for SLAM downwards text 


    // --- Kinetic jump grace window (lets you jump right after slide ends) -- for dashSlam perk
    public float kineticJumpWindow = 0.35f;   // seconds after slide ends where Kinetic Jump is still allowed
    private float lastSlideEndTime = -999f;   // when the last slide ended


    [Header("Slam Attack Settings")]
    public float slamRadius = 5f;
    public float slamDamage = 100f;
    public LayerMask enemyMask;
    public GameObject slamImpactVFX; // blood splat + impact on enemies their VFX
    public GameObject KineticUnderneathSlamImpactVFX;
    public GameObject KineticUnderneathSlamImpactVFX2; //not in use
    public GameObject KineticUnderneathSlamImpactVFX3;
    public GameObject KineticUnderneathSlamImpactVFX4; //not in use
    public GameObject KineticUnderneathSlamImpactVFX5;

    //--------------------------------------------

    private CharacterController controller;
    private ArmMovementMegaScript armMover;
    private UI ui;
    private Vector3 velocity;
    private bool isGrounded;
    private Vector3 lastMoveDirection;  //stores last movement direction

    // Slide variables
    private bool isSliding = false;
    private float slideTimer = 0f;
    private Vector3 slideDirection;
    private float normalControllerHeight;
    private Vector3 normalControllerCenter;

    private Vector3 externalForce = Vector3.zero;

    [Header("Dash Settings")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 2f;
    //public GameObject dashVFX;

    private bool isDashing = false;
    private float dashTimer = 0f;
    private float lastDashTime;
    public float LastDashTime => lastDashTime; //some sort of getter
    private Vector3 dashDirection;



    [Header("Perk/Effects Multipliers")]
    public float ExternalSpeedMult = 1f; // used by perks like Adrenaline
    private float _baseDashSpeed, _baseDashDuration, _baseDashCooldown, _baseMinSlideTimeForKinetic;



    void Start()
    {
        controller = GetComponent<CharacterController>();
        armMover = GetComponent<ArmMovementMegaScript>();
        ui = GetComponent<UI>();
        if (ui == null) ui = FindFirstObjectByType<UI>();
        if (armMover == null) armMover = FindFirstObjectByType<ArmMovementMegaScript>();  //For some reason this allows to nto slide when drinking. 

        // Store normal controller dimensions so we can like reset
        normalControllerHeight = controller.height;
        normalControllerCenter = controller.center;

        _baseWalkSpeed = walkSpeed;             //-- for perk resetting
        _baseSprintSpeed = sprintSpeed;

        lastDashTime = -dashCooldown;  //bar starts full / dash ready

        //-- this is for the perk 
        _baseDashSpeed = dashSpeed;
        _baseDashDuration = dashDuration;
        _baseDashCooldown = dashCooldown;
        _baseMinSlideTimeForKinetic = minSlideTimeForKineticJump;


    }

    void Update()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;  //stay grounded so dont do 0
            if (isKineticJump)
            {
                isKineticJump = false;
                if (isSlamming)
                {
                    isSlamming = false;
                    ApplyKineticSlamDamage();   //we trigged kineticSlam so we apply this. 

                    if (KineticUnderneathSlamImpactVFX != null)         //this is the shit underneath the player
                    {
                        GameObject vfx1 = Instantiate(KineticUnderneathSlamImpactVFX, transform.position, Quaternion.identity);
                        Destroy(vfx1, 10f);
                        //Instantiate(KineticUnderneathSlamImpactVFX2, transform.position, Quaternion.identity);
                        GameObject vfx2 = Instantiate(KineticUnderneathSlamImpactVFX3, transform.position, Quaternion.identity);
                        Destroy(vfx2, 10f);
                        //Instantiate(KineticUnderneathSlamImpactVFX4, transform.position, Quaternion.identity);
                        GameObject vfx3 = Instantiate(KineticUnderneathSlamImpactVFX5, transform.position, Quaternion.identity);
                        Destroy(vfx3, 10f);
                    }

                }
            }
        }


        HandleSlideInput();
        //HandleMovement();   ------   we could divide it up well
        //HandleJump();
        //ApplyGravity();

        // Prevent sprint while firing
        bool isAiming = KeybindManager.Instance.GetKeyHeld("AimDownSight");

        bool isFiring = KeybindManager.Instance.GetKeyHeld("AimDownSight");
        bool isSprinting = KeybindManager.Instance.GetKeyHeld("Sprint") && !isFiring;

        float currentSpeed;

        if (isAiming)
            currentSpeed = aimSpeed;
        else if (isSprinting)
            currentSpeed = sprintSpeed;
        else
            currentSpeed = walkSpeed;

        currentSpeed *= ExternalSpeedMult; // this is for the adrenaline ICU type perk

        float x_input = Input.GetAxisRaw("Horizontal");
        float z_input = Input.GetAxisRaw("Vertical");

        Vector3 inputDirection = transform.right * x_input + transform.forward * z_input;  

        if (isGrounded)
        {
            lastMoveDirection = inputDirection.normalized;  //if you jump right after walking forward, it "remembers" that direction
        }
        else
        {
            // If you aren’t pressing any movement keys and in the air we have that stored last direction and if u move mid air we update it
            if (inputDirection.magnitude == 0)
            {
                inputDirection = lastMoveDirection;
            }
            else
            {
                // If player provides new input in air, update last direction
                lastMoveDirection = inputDirection.normalized;
            }
        }

        //------------------------------------------------------- Slam logic
        bool canSlam = Time.time >= lastSlamTime + slamCooldown;   //for cooldown so u dont spam.
        if (isKineticJump && !isGrounded && !isSlamming && Time.time > lastSlamTime)
        {
            if (KeybindManager.Instance.GetKeyDown("Jump&Slam") && !isGrounded && canSlam && !PauseUI.IsPaused) // make sure ur not on ground and are ALOUD TO SLAM based off the bool above
            {
                StartKineticSlam();
            }
        }


        controller.Move(inputDirection * currentSpeed * Time.deltaTime);

        if (KeybindManager.Instance.GetKeyDown("Jump&Slam") && isGrounded)
        {
            if (isSliding)
            {
                // Only allow Kinetic Jump if we’ve slid long enough
                bool allowKinetic = slideTimer >= minSlideTimeForKineticJump;

                if (allowKinetic)
                {
                    velocity.y = KineticJumpForce; // boosted jump
                    isKineticJump = true;          // enables slam in-air
                }
                else
                {
                    velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity); // normal jump
                    isKineticJump = false;                                // no slam
                }

                EndSlide();
            }
            else
            {
                // NEW: short grace window after slide
                bool withinGrace = (Time.time - lastSlideEndTime) <= kineticJumpWindow;
                if (withinGrace)
                {
                    velocity.y = KineticJumpForce;
                    isKineticJump = true;
                }
                else
                {
                    velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }
            }
        }
        //grav
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // === Apply external knockback / launch ===
        if (externalForce.magnitude > 0.1f)
        {
            transform.position += externalForce * Time.deltaTime;

            // Smoothly reduce the force over time
            externalForce = Vector3.Lerp(externalForce, Vector3.zero, 5f * Time.deltaTime);
        }

        // === DASH ===
        if (!isDashing && KeybindManager.Instance.GetKeyDown("Dash") && Time.time >= lastDashTime + dashCooldown && !PauseUI.IsPaused)
        {
            StartDash();
        }

        if (isDashing)
        {
            dashTimer += Time.deltaTime;
            controller.Move(dashDirection * dashSpeed * Time.deltaTime);

            if (dashTimer >= dashDuration)
            {
                EndDash();
            }
        }


    }

    void StartDash()
    {

 
        isDashing = true;
        dashTimer = 0f;
        lastDashTime = Time.time;

        // Direction = input direction, fallback to forward
        float x_input = Input.GetAxisRaw("Horizontal");
        float z_input = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = (transform.right * x_input + transform.forward * z_input).normalized;

        dashDirection = inputDir.magnitude > 0 ? inputDir : transform.forward;

        // Optional: zero vertical velocity during dash
        velocity.y = 0f;

        // Spawn VFX
        //if (dashVFX != null)
        //{
        //    GameObject vfx = Instantiate(dashVFX, transform.position, Quaternion.identity);
        //    Destroy(vfx, 2f);
        //}


        if (ui.dashVFXPicture != null)
        {
            StartCoroutine(ShowDashVFXPicture());
        }


    }

    //private IEnumerator ShowDashVFXPicture()
    //{
    //    ui.dashVFXPicture.gameObject.SetActive(true);
    //    yield return new WaitForSeconds(1f); // show for 1 second
    //    ui.dashVFXPicture.gameObject.SetActive(false);
    //}

    private IEnumerator ShowDashVFXPicture()
    {
        if (ui == null || ui.dashVFXPicture == null)
            yield break;

        Image img = ui.dashVFXPicture;

        // Make sure the object is active
        img.gameObject.SetActive(true);

        // Fade in
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.1f; // 0.25 sec fade in
            Color c = img.color;
            img.color = new Color(c.r, c.g, c.b, Mathf.Lerp(0f, 0.4f, t));  //we dont do 1 bc we drop oppacity
            yield return null;
        }

        // Hold for 0.5s
        yield return new WaitForSeconds(0.1f);

        // Fade out
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.3f; // 0.5 sec fade out
            Color c = img.color;
            img.color = new Color(c.r, c.g, c.b, Mathf.Lerp(0.4f, 0f, t));  //we dont do 1 bc we drop oppacity
            yield return null;
        }

        // Optionally disable after fade out
        img.gameObject.SetActive(false);
    }


    void EndDash()
    {
        isDashing = false;
    }


    //------------------------------------------------------------ Kinetic Slam

    public void ApplyMoreDashSlamBuffs(
    float dashSpeedMult = 1.25f,
    float dashDurationMult = 1.15f,
    float dashCooldownMult = 0.60f,
    float minSlideTimeForKineticMult = 0.60f,
    float newKineticJumpWindow = 0.40f)   // optional: widen grace a bit
    {
        dashSpeed = _baseDashSpeed * dashSpeedMult;
        dashDuration = _baseDashDuration * dashDurationMult;
        dashCooldown = _baseDashCooldown * dashCooldownMult;

        minSlideTimeForKineticJump = _baseMinSlideTimeForKinetic * minSlideTimeForKineticMult;
        kineticJumpWindow = newKineticJumpWindow;
    }

    // (optional) reset if you ever remove the perk
    public void ResetDashSlamToBase()
    {
        dashSpeed = _baseDashSpeed;
        dashDuration = _baseDashDuration;
        dashCooldown = _baseDashCooldown;
        minSlideTimeForKineticJump = _baseMinSlideTimeForKinetic;

        // if you added this field for grace window, reset to your default
        kineticJumpWindow = 0.35f;

        // also clear runtime dash state
        isDashing = false;
        dashTimer = 0f;
        lastDashTime = -dashCooldown; // bar shows ready again
    }


    public bool CanKineticJumpNow
    {
        get
        {
            bool readyDuringSlide = isGrounded && isSliding && (slideTimer >= minSlideTimeForKineticJump);
            bool readyAfterSlide = isGrounded && !isSliding && ((Time.time - lastSlideEndTime) <= kineticJumpWindow);
            return readyDuringSlide || readyAfterSlide;
        }
    }

    public bool CanSlamNow  //this is for the slam text
    {
        get
        {
            // same logic you use before calling StartKineticSlam()
            bool canSlam = Time.time >= lastSlamTime + slamCooldown;
            return isKineticJump && !isGrounded && !isSlamming && canSlam;
        }
    }


    void StartKineticSlam()
    {
        isSlamming = true;
        lastSlamTime = Time.time;
        velocity.y = slamDownForce;

        // Optional FX trigger here
        // e.g. CameraShake.ShakeOnce(), play sound, etc.
    }



    void ApplyKineticSlamDamage()
    {
        Collider[] hitEnemies = Physics.OverlapSphere(transform.position, slamRadius, enemyMask);   //parameter(center of sphere, radiusOFSphere, a layermask defines which layers of colliders to include in the query)
        // ^^ It checks for all colliders that are on the enemyMask layer These are the enemies within range of the slam.


        foreach (Collider enemy in hitEnemies)  //For each enemy in range, this block will:
        {
            EnemyHealthRagdoll health = enemy.GetComponent<EnemyHealthRagdoll>();  //fetch script
            if (health != null)
            {
                Vector3 direction = (enemy.transform.position - transform.position).normalized; //we find direction from us teh player to enemy 
                health.TakeDamage(slamDamage, direction);   //in enemyHealthRagdoll script 

                // Apply explosion force to all rigidbodies in the enemy
                Rigidbody[] rbs = enemy.GetComponentsInChildren<Rigidbody>();   //so now we get rigidBodies of every enemy in 
                foreach (Rigidbody rb in rbs) //For each rigidBody in range, this block will:
                {
                    if (rb != null)
                    {
                        float dist = Vector3.Distance(transform.position, rb.transform.position);    //check how far the rigidbody itself and the player is. 
                        float force = Mathf.Lerp(105f, 105f, dist / slamRadius);   // we use linear interpolation so the closer you are the more the damage and force 
                                                                                // If the bone is very close, dist / slamRadius ≈ 0 → force ≈ 45
                                                                                // If the bone is at the edge, dist / slamRadius ≈ 1 → force ≈ 5
                        rb.AddExplosionForce(force, transform.position, slamRadius, 1552.3f, ForceMode.Impulse); // Lower upward lift (how strong, expolision origin, hjow far explosion affects, upward modifer gives the bone vertical lift, ForceMode.Impulse is an instant kick like a punch.)

                    }
                }

                if (slamImpactVFX != null)
                {
                    GameObject deathVFXEnemy = Instantiate(slamImpactVFX, enemy.transform.position + Vector3.up * 1f, Quaternion.identity);
                    Destroy(deathVFXEnemy,5f);
                }
            }
        }

        CameraScript.Main?.Shake(0.5f, 2.5f, 55f, false);   // we write false so we dont have VFX on for this
    }


    //------------------------------------------------------- Slideing Logic


    void HandleSlideInput()
    {
        if (armMover != null && armMover.DrinkingPerk)  
        {
            if (isSliding) EndSlide();
            return;
        }

        bool canSlide = KeybindManager.Instance.GetKeyHeld("Sprint") && KeybindManager.Instance.GetKeyDown("Slide") && isGrounded && !isSliding;

        if (canSlide)
        {
            StartSlide(); }
        if (isSliding)
        {
            UpdateSlide();
        }

    }

    void StartSlide()
    {

        isSliding = true;
        slideTimer = 0f;   //Resets the slide timer to start counting from zero

        float x_input = Input.GetAxisRaw("Horizontal");
        float z_input = Input.GetAxisRaw("Vertical");
        slideDirection = (transform.right * x_input + transform.forward * z_input).normalized;   //we get horizontal adn back fourth input and normalized makes the vector lentgth 1 for consisten speed 

        if (slideDirection.magnitude == 0)   //this is sliding adn not moving which is wack -- i take this back
        {
            slideDirection = transform.forward;   //If no movement keys are pressed when slide starts Default to sliding forward
        }

        controller.height = slideControllerHeight;          //!!! COME BACK
        controller.center = new Vector3(normalControllerCenter.x, slideControllerHeight / 2f, normalControllerCenter.z);  //!!! COME BACK
    }

    void UpdateSlide()
    {
        slideTimer += Time.deltaTime;   //how long ive been sliding

        // Calculate slide speed with deceleration
        float currentSlideSpeed = Mathf.Lerp(slideSpeed, walkSpeed, slideTimer / slideDuration);   //Mathf.Lerp(startValue, endValue, t) we go from fast to walkspeed over 0 - 1 with slidetimer over how long the slide it (confusing tbh) 
         
        controller.Move(slideDirection * currentSlideSpeed * Time.deltaTime);  // move(where to go, how fast)

        // End slide when timer expires or player stops holding shift  !! COME BACK SO WE CAN EDIT THIS BEHAVIOUR
        if (slideTimer >= slideDuration) // || !Input.GetKey(KeyCode.LeftShift))
        {
            EndSlide();
        }
    }

    void EndSlide()
    {
        isSliding = false;
        slideTimer = 0f;
        lastSlideEndTime = Time.time;  //for the dashSlam perk
        controller.height = normalControllerHeight;        // Restore normal controller dimensions
        controller.center = normalControllerCenter;
    }

    public void IncreaseSpeedFromMoreSpeedPerk(float WalkSpeed, float SprintSpeed)
    {
        walkSpeed = WalkSpeed;
        sprintSpeed = SprintSpeed;
    }

    //----------------------- Getters

    public void ApplyExternalForce(Vector3 force)
    {
        StartCoroutine(ApplyKnockbackWithArc(force));
    }

    private IEnumerator ApplyKnockbackWithArc(Vector3 initialForce)
    {
        float gravity = -30f;  // tweak to match your world gravity
        Vector3 velocity = initialForce; // start with our launch force

        float timer = 0f;
        while (timer < 2f)  // stop after 2s if not landed
        {
            // apply gravity each frame
            velocity += Vector3.up * gravity * Time.deltaTime;

            // movement this frame
            Vector3 move = velocity * Time.deltaTime;

            // check collision
            if (!Physics.Raycast(transform.position, move.normalized, move.magnitude + 0.1f))
            {
                transform.position += move;
            }
            else
            {
                // hit something -> stop knockback
                break;
            }

            timer += Time.deltaTime;
            yield return null;
        }
    }




    public void ResetSpeedsToBase()   //after death for speed perk
    {
        walkSpeed = _baseWalkSpeed;
        sprintSpeed = _baseSprintSpeed;
        ExternalSpeedMult = 1f;

    }


    public bool IsGrounded() => isGrounded;
    public bool IsSliding()  // for Cam script so i can reset it 
    {
        return isSliding;
    }

    public float LastSlamTime => lastSlamTime;  // for UI cooldown

    public bool IsSprinting()
    {
        return KeybindManager.Instance.GetKeyHeld("Sprint") && controller.velocity.magnitude > 0.1f;
    }

    public bool IsDashing()
    {
        return isDashing;
    }

    public float GetSlideTimer() => slideTimer; //getter for the small bar 
    public float GetKJProgress01WhileSliding()  //getter for the small bar to showcase you can jump 
    {
        return Mathf.Clamp01(slideTimer / Mathf.Max(0.0001f, minSlideTimeForKineticJump)); //idk ?
    }

    public void EnableKineticJumpNow()   //getter for the booster Jump Pad. u can kinetic slam off that 
    {
        isKineticJump = true;   // you'll auto-reset to false on landing (you already do that)
    }
    public bool IsSlamming() => isSlamming;  //getter for the booster Jump Pad. u can kinetic slam off that 

}
