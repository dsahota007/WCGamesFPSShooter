using UnityEngine;
using System.Collections;


public class VoidMinibossEntity : MonoBehaviour
{
    [Header("Tracking Settings")]
    public float moveSpeed = 10f;         // how fast it moves
    public float rotateSpeed = 5f;        // how quickly it rotates to face the player
    public float lifeTime = 8f;           // auto-destroy after this time

    [Header("Slam Attack on Impact")]
    public float slamRadius = 6f;
    public float slamDamage = 25f;       // hurts player instead of enemies
    public LayerMask playerMask;         // make sure Player is on this layer

    [Header("Wind Pull + Knockback")]
    public float pullForce = 20f;        // how hard it sucks player inward
    public float centerThreshold = 1f;   // how close to center before launching
    public float knockbackForce = 25f;   // outward push
    public float upwardForce = 12f;      // vertical lift

    [Header("VFX Effects")]
    public GameObject PlayerImpactVFX;
    public GameObject PlayerImpactVFX2;

    [Header("Ground Slam VFX")]
    public GameObject GroundEntitySlamVFX;
    public Vector3 GroundVFXOffset = Vector3.zero;  // local/world offset
    public Vector3 GroundVFXEuler = Vector3.zero;   // rotation override
    public Vector3 GroundVFXScale = Vector3.one;    // scale override
    public float GroundVFXLifetime = 10f;           // how long it lasts

    private Transform playerTarget; // cached player
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            playerTarget = playerObj.transform;

        // safety destroy after lifetime
        Destroy(gameObject, lifeTime);

        // Ignore collision with enemy who spawned this projectile
        Collider[] shooterColliders = GameObject.FindGameObjectWithTag("Enemy")?.GetComponentsInChildren<Collider>();
        if (shooterColliders != null)
        {
            foreach (Collider col in shooterColliders)
            {
                Physics.IgnoreCollision(GetComponent<Collider>(), col);
            }
        }
    }

    void Update()
    {
        if (playerTarget == null) return;

        // rotate toward player smoothly
        Vector3 dir = (playerTarget.position - transform.position).normalized;
        Quaternion lookRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotateSpeed * Time.deltaTime);

        // move forward constantly
        transform.position += transform.forward * moveSpeed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ground") || other.CompareTag("Wall"))
        {
            StartCoroutine(ApplyWindMeteorLogic());
            SpawnGroundEffects();
            return;
        }

        if (other.CompareTag("Player"))
        {
            StartCoroutine(ApplyWindMeteorLogic());
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        StartCoroutine(ApplyWindMeteorLogic());
        SpawnGroundEffects();
    }

    void SpawnGroundEffects()
    {
        if (GroundEntitySlamVFX != null)
        {
            GameObject vfx1 = Instantiate(GroundEntitySlamVFX, transform.position, Quaternion.identity);

            // Apply customization
            vfx1.transform.position += GroundVFXOffset;
            vfx1.transform.rotation = Quaternion.Euler(GroundVFXEuler);
            vfx1.transform.localScale = GroundVFXScale;

            Destroy(vfx1, GroundVFXLifetime);
        }
    }

    IEnumerator ApplyWindMeteorLogic()
    {
        // Check for player in radius
        Collider[] hitPlayers = Physics.OverlapSphere(transform.position, slamRadius, playerMask);

        foreach (Collider hit in hitPlayers)
        {
            PlayerAttributes player = hit.GetComponent<PlayerAttributes>();  // fetch player script
            if (player != null)
            {
                player.TakeDamagefromEnemy(slamDamage);   // simple health damage

                // Get movement ref
                var movement = player.GetComponent<PlayerMovement>();
                if (movement != null)
                {
                    // === Phase 1: pull until close to center ===
                    while (Vector3.Distance(player.transform.position, transform.position) > centerThreshold)
                    {
                        Vector3 pullDir = (transform.position - player.transform.position).normalized;
                        movement.ApplyExternalForce(pullDir * pullForce);
                        yield return null; // wait a frame, then continue pulling
                    }

                    // === Phase 2: launch outward + upward ===
                    Vector3 dir = (player.transform.position - transform.position).normalized;
                    dir.y = 0f; // only horizontal push
                    Vector3 knockDir = dir * knockbackForce + Vector3.up * upwardForce;
                    movement.ApplyExternalForce(knockDir);
                }

                // Spawn player hit VFX
                if (PlayerImpactVFX != null)
                {
                    GameObject fx = Instantiate(PlayerImpactVFX, player.transform.position + Vector3.up * 1f, Quaternion.identity);
                    Destroy(fx, 5f);
                }

                if (PlayerImpactVFX2 != null)
                {
                    GameObject fx = Instantiate(PlayerImpactVFX2, player.transform.position, Quaternion.Euler(-90f, 0f, -90f));
                    Destroy(fx, 25f);
                }
            }
        }

        Destroy(gameObject);
    }
}