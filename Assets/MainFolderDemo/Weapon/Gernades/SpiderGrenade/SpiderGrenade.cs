using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpiderGrenade : MonoBehaviour
{
    [Header("Web Settings")]
    public float speed = 20f;                 // we can still launch forward if needed
    public float lifeTime = 5f;
    public float webRadius = 5f;              // same idea as web radisu
    public float webDuration = 8f;            // how long the web lingers
    public LayerMask enemyMask;

    [Header("VFX")]
    public GameObject GroundImpactVFX;
    //public GameObject GasCloudVFX;            // poison cloud but bio gas

    [Header("Physics")]
    public float spinTorque = 5f;             // small spin for style

    // -------------------- ONLY NEW SETTINGS (for slow) --------------------
    [Header("Spider Slow Settings")]
    [Range(0f, 1f)] public float slowPercent = 0.6f;  // 0.6 = 60% slow (enemy moves at 40%)
    public float refreshTick = 1f;                    // how often to refresh the slow while inside

    // optional per-enemy web VFX (uses EnemyHealthRagdoll.ApplyIceSlow just like the ICE bullet)
    public GameObject webOnEnemyVFXPrefab;
    public Vector3 webOnEnemyVFXOffset = Vector3.zero;
    public Vector3 webOnEnemyVFXEuler = Vector3.zero;
    public Vector3 webOnEnemyVFXScale = Vector3.one;
    public float webOnEnemyVFXLifetime = 0f; // 0 = let ApplyIceSlow manage; >0 auto-destroy

    private Rigidbody rb;
    private Vector3 impactPoint;
    private bool hasImpacted = false;
    private GameObject gasCloudInstance;

    // -----------------------------------------------------------------------------------
    // NOTE: we keep a set but we DO NOT kill anyone. We only use it to avoid double work
    // per refresh tick. Your Bio version used a list to prevent double-killing; here we
    // reapply a slow periodically so latecomers get caught too.
    // -----------------------------------------------------------------------------------
    private readonly HashSet<EnemyHealthRagdoll> seenThisTick = new HashSet<EnemyHealthRagdoll>();

    void Awake()       //Awake(): A Unity lifecycle method that runs before Start(),
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    void Start()
    {
        // Add a little spin so it rolls naturally
        if (spinTorque > 0f && rb != null)
            rb.AddTorque(Random.onUnitSphere * spinTorque, ForceMode.Impulse);    //spin logic

        // if you’re throwing via ApplyThrow() you can ignore this forward kick  --- THIS WAS COMMENTED OUT BC THIS WAS THE BUG WHERE IT WOULD GO RIGHT AND DOWN
        //if (rb != null && speed > 0f)
        //    rb.linearVelocity = transform.forward * speed;  //we wanna launch str8 forward

        Destroy(gameObject, lifeTime);

        // we use this to make sure it does not hit us 
        Collider[] playerColliders = GameObject.FindGameObjectWithTag("Player").GetComponentsInChildren<Collider>();
        foreach (Collider col in playerColliders)
        {
            Physics.IgnoreCollision(GetComponent<Collider>(), col);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasImpacted) return;
        if (other.CompareTag("Ground") || other.CompareTag("Wall") || other.CompareTag("Enemy"))
        {
            TriggerBioEffect();   // same as venom, just bio gas  (here: spider web gas that SLOWS)
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasImpacted) return;   //if already HIT the ground or enemy leave this code
        TriggerBioEffect();         // Backup collision detection in case trigger doesn't work
    }

    void TriggerBioEffect()
    {
        hasImpacted = true;
        impactPoint = transform.position;           //find im pact point

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;       //kill the movement
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;                  //kill the physics
        }

        if (GroundImpactVFX != null)
        {
            GameObject vfx = Instantiate(GroundImpactVFX, impactPoint, Quaternion.identity);
            Destroy(vfx, 5f);
        }

        // spawn the gas cloud that lingers ~ but instead of killing, it snares (slow)
        //if (GasCloudVFX != null)
        //{
        //    gasCloudInstance = Instantiate(GasCloudVFX, impactPoint, Quaternion.identity);
        //    Destroy(gasCloudInstance, gasDuration);
        //}

        //Starts checking for enemies inside the area every frame
        StartCoroutine(SlowEnemiesInRadius());
    }

    IEnumerator SlowEnemiesInRadius()
    {
        float timer = 0f;    //start a timer at 0 
        float nextTick = 0f;
        refreshTick = Mathf.Max(0.1f, refreshTick); // sanity so it always refreshes

        while (timer < webDuration)     //as long as the timer is less than the specified duration
        {
            timer += Time.deltaTime;     //Increases the timer by the time passed since the last frame

            // re-apply the slow on a schedule so it sticks + catches latecomers
            if (Time.time >= nextTick)
            {
                nextTick = Time.time + refreshTick;
                seenThisTick.Clear();

                //find enemeis within radius
                Collider[] hits = Physics.OverlapSphere(impactPoint, webRadius, enemyMask);
                foreach (Collider col in hits)
                {
                    // find colliders of enemies
                    EnemyHealthRagdoll enemy = col.GetComponentInParent<EnemyHealthRagdoll>();
                    if (enemy == null || enemy.IsDead()) continue;
                    if (seenThisTick.Contains(enemy)) continue;
                    seenThisTick.Add(enemy);

                    // ----------------------------- THE CHANGE -----------------------------
                    // apply the ICE slow logic (same function your Ice bullet uses)
                    // calc the speed multiplier: if slowPercent = 0.6f → enemy moves at 40% speed
                    float speedMultiplier = Mathf.Clamp01(1f - slowPercent);

                    // tiny overlap so it doesn't "blink" off between ticks
                    enemy.ApplyIceSlow(
                        durationSeconds: refreshTick + 0.1f,
                        speedMultiplier: speedMultiplier,
                        onEnemyVFXPrefab: webOnEnemyVFXPrefab,         // optional sticky web VFX on enemy
                        vfxLocalPos: webOnEnemyVFXOffset,
                        vfxLocalEuler: webOnEnemyVFXEuler,
                        vfxLocalScale: webOnEnemyVFXScale,
                        vfxLifetime: webOnEnemyVFXLifetime
                    );
                    // ----------------------------------------------------------------------
                }
            }

            yield return null;    //wait 1 frame, then repeat -- from time.delta
        }

        Destroy(gameObject);
    }

    // if you prefer to set velocity when you throw it from code
    public void ApplyThrow(Vector3 velocity)
    {
        if (rb != null) rb.linearVelocity = velocity;   // Unity 6 API you were using
    }
}
