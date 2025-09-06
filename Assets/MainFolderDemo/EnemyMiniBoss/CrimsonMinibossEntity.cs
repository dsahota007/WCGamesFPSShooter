using UnityEngine;
using System.Collections;

public class CrimsonMinibossEntity : MonoBehaviour
{
    [Header("Tracking Settings")]
    public float moveSpeed = 10f;         // how fast it moves
    public float rotateSpeed = 5f;        // how quickly it rotates to face the player
    public float lifeTime = 8f;           // auto-destroy if it never lands

    [Header("Void Field Settings")]
    public float slamRadius = 6f;         // radius of damaging field
    public float slamDamagePerSecond = 10f; // DPS applied to player
    public float fieldDuration = 4f;      // how long the void field lasts
    public LayerMask playerMask;          // player must be on this layer

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
    private bool hasLanded = false; // track if we already impacted

    private Vector3 moveDirection; // direction at spawn

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
            moveDirection = (playerTarget.position - transform.position).normalized;
        }
        else
        {
            moveDirection = transform.forward; // fallback if no player found
        }

        // === Meteor-style movement ===
        rb.linearVelocity = moveDirection * moveSpeed;

        // safety destroy after lifetime if it never collides
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
        if (hasLanded) return; // stop logic after impact
        if (playerTarget == null) return;

        // rotate toward player smoothly (cosmetic only, doesn't affect flight path)
        Vector3 dir = (playerTarget.position - transform.position).normalized;
        Quaternion lookRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotateSpeed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ground") || other.CompareTag("Wall") || other.CompareTag("Player"))
        {
            if (!hasLanded)
            {
                hasLanded = true;
                rb.linearVelocity = Vector3.zero; // stop moving
                StartCoroutine(VoidFieldDamageOverTime());
                SpawnGroundEffects();
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!hasLanded)
        {
            hasLanded = true;
            rb.linearVelocity = Vector3.zero; // stop moving
            StartCoroutine(VoidFieldDamageOverTime());
            SpawnGroundEffects();
        }
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

    IEnumerator VoidFieldDamageOverTime()
    {
        float timer = 0f;

        while (timer < fieldDuration)
        {
            // Check for player in radius
            Collider[] hitPlayers = Physics.OverlapSphere(transform.position, slamRadius, playerMask);

            foreach (Collider hit in hitPlayers)
            {
                PlayerAttributes player = hit.GetComponent<PlayerAttributes>();  // fetch player script
                if (player != null)
                {
                    player.TakeDamagefromEnemy(slamDamagePerSecond * Time.deltaTime); // DPS tick

                    // Spawn hit VFX occasionally (optional)
                    if (PlayerImpactVFX != null)
                    {
                        GameObject fx = Instantiate(PlayerImpactVFX, player.transform.position + Vector3.up * 1f, Quaternion.identity);
                        Destroy(fx, 1f);
                    }
                }
            }

            timer += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject); // remove void after duration
    }
}
