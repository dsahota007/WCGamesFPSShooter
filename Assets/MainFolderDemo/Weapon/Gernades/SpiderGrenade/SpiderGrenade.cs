using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpiderWebGrenade : MonoBehaviour
{
    [Header("Web Settings")]
    public float speed = 20f;                  // optional forward launch if you use ApplyThrow()
    public float lifeTime = 5f;                // grenade self-destruct (safety)
    public float webRadius = 5f;               // area of effect
    public float webDuration = 4f;             // how long the web lingers
    [Range(0f, 1f)]
    public float slowPercent = 0.6f;           // 0.6 = 60% slower (i.e., 40% speed left)
    public LayerMask enemyMask;                // who gets slowed

    [Header("VFX")]
    public GameObject GroundImpactVFX;         // burst on impact
    public GameObject WebCloudVFX;             // lingering web cloud object (destroyed after webDuration)

    [Header("Optional: Per-Enemy Web VFX (uses EnemyHealthRagdoll.ApplyIceSlow)")]
    public GameObject webOnEnemyVFXPrefab;
    public Vector3 webOnEnemyVFXOffset = Vector3.zero;
    public Vector3 webOnEnemyVFXEuler = Vector3.zero;
    public Vector3 webOnEnemyVFXScale = Vector3.one;
    public float webOnEnemyVFXLifetime = 0f;   // 0 = managed by the slow end; >0 = auto-destroy

    [Header("Physics")]
    public float spinTorque = 5f;              // small spin for style

    private Rigidbody rb;
    private Vector3 impactPoint;
    private bool hasImpacted = false;
    private GameObject webCloudInstance;

    // we refresh slows each second; keep a small cache just to avoid duplicate work per frame
    private readonly HashSet<EnemyHealthRagdoll> _seenThisTick = new HashSet<EnemyHealthRagdoll>();

    void Awake()
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
        // stylish spin
        if (spinTorque > 0f && rb != null)
            rb.AddTorque(Random.onUnitSphere * spinTorque, ForceMode.Impulse);

        // lifetime failsafe
        Destroy(gameObject, lifeTime);

        // don't bonk the player
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
            TriggerWebEffect();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasImpacted) return;
        TriggerWebEffect(); // backup if trigger doesn’t fire
    }

    void TriggerWebEffect()
    {
        hasImpacted = true;
        impactPoint = transform.position;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (GroundImpactVFX != null)
        {
            var vfx = Instantiate(GroundImpactVFX, impactPoint, Quaternion.identity);
            Destroy(vfx, 5f);
        }

        if (WebCloudVFX != null)
        {
            webCloudInstance = Instantiate(WebCloudVFX, impactPoint, Quaternion.identity);
            Destroy(webCloudInstance, webDuration);
        }

        StartCoroutine(ApplyWebSlowsInRadius());
    }

    IEnumerator ApplyWebSlowsInRadius()
    {
        float timer = 0f;
        float tickInterval = 1f;   // refresh rate for reapplying slow (keeps enemies snared)
        float nextTick = 0f;

        while (timer < webDuration)
        {
            timer += Time.deltaTime;

            if (Time.time >= nextTick)
            {
                nextTick = Time.time + tickInterval;
                _seenThisTick.Clear();

                // find enemies in radius
                Collider[] hits = Physics.OverlapSphere(impactPoint, webRadius, enemyMask, QueryTriggerInteraction.Ignore);
                foreach (Collider col in hits)
                {
                    var enemy = col.GetComponentInParent<EnemyHealthRagdoll>();
                    if (enemy == null || enemy.IsDead()) continue;
                    if (_seenThisTick.Contains(enemy)) continue;
                    _seenThisTick.Add(enemy);

                    // Apply ICE slow API as a "web" — refreshes duration every tick while inside
                    float slowMultiplier = Mathf.Clamp01(1f - slowPercent); // e.g., 0.6 -> 0.4 speed left
                    enemy.ApplyIceSlow(
                        durationSeconds: tickInterval + 0.1f, // slight overlap so it never drops while inside
                        speedMultiplier: slowMultiplier,
                        onEnemyVFXPrefab: webOnEnemyVFXPrefab,
                        vfxLocalPos: webOnEnemyVFXOffset,
                        vfxLocalEuler: webOnEnemyVFXEuler,
                        vfxLocalScale: webOnEnemyVFXScale,
                        vfxLifetime: webOnEnemyVFXLifetime
                    );
                }
            }

            yield return null; // wait 1 frame
        }

        Destroy(gameObject); // clean up grenade shell if it’s still around
    }

    // set initial velocity from throw code (Unity 6 API)
    public void ApplyThrow(Vector3 velocity)
    {
        if (rb != null) rb.linearVelocity = velocity;
    }

    // OPTIONAL: visualize radius in editor
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.25f);
        Gizmos.DrawWireSphere(Application.isPlaying ? impactPoint : transform.position, webRadius);
    }
#endif
}
