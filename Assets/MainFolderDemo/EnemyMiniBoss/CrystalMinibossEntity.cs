using UnityEngine;

public class CrystalMinibossEntity : MonoBehaviour
{
    [Header("Slam Attack on Impact")]
    public float slamRadius = 5f;
    public float slamDamage = 40f;   // hurts player instead of enemies
    public LayerMask playerMask;     // make sure Player is on this layer

    [Header("Knockback Settings")]
    public float knockbackForce = 15f;   // horizontal push force
    public float upwardForce = 10f;      // vertical launch force (new)

    [Header("VFX Effects")]
    public GameObject PlayerImpactVFX;
    public GameObject PlayerImpactVFX2;

    [Header("Ground Slam VFX")]
    public GameObject GroundEntitySlamVFX;
    public Vector3 GroundVFXOffset = Vector3.zero;  // local/world offset
    public Vector3 GroundVFXEuler = Vector3.zero;   // rotation override
    public Vector3 GroundVFXScale = Vector3.one;    // scale override
    public float GroundVFXLifetime = 10f;           // how long it lasts

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Ignore collision with enemy who spawned this projectile (so it doesn’t instantly explode)
        Collider[] shooterColliders = GameObject.FindGameObjectWithTag("Enemy")?.GetComponentsInChildren<Collider>();
        if (shooterColliders != null)
        {
            foreach (Collider col in shooterColliders)
            {
                Physics.IgnoreCollision(GetComponent<Collider>(), col);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ground") || other.CompareTag("Wall"))
        {
            ApplyCrystalSlamDamage();
            SpawnGroundEffects();
            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Player"))
        {
            ApplyCrystalSlamDamage();
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Backup collision detection in case trigger doesn't work
        ApplyCrystalSlamDamage();
        SpawnGroundEffects();
        Destroy(gameObject);
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

    void ApplyCrystalSlamDamage()
    {
        // Check for player in radius
        Collider[] hitPlayers = Physics.OverlapSphere(transform.position, slamRadius, playerMask);

        foreach (Collider hit in hitPlayers)
        {
            PlayerAttributes player = hit.GetComponent<PlayerAttributes>();  // fetch player script
            if (player != null)
            {
                // === Apply damage ===
                player.TakeDamagefromEnemy(slamDamage);   // simple health damage

                // === Knockback / bounce back ===
                Rigidbody prb = player.GetComponent<Rigidbody>();
                if (prb != null)
                {
                    // Horizontal push
                    Vector3 dir = (player.transform.position - transform.position).normalized;
                    dir.y = 0f; // keep only horizontal direction
                    prb.AddForce(dir * knockbackForce, ForceMode.Impulse);

                    // Separate upward launch
                    prb.AddForce(Vector3.up * upwardForce, ForceMode.Impulse);
                }

                // === VFX on hit ===
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
    }
}
