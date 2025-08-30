using UnityEngine;

public class CrystalAndWindMinibossEntity : MonoBehaviour
{
    [Header("Slam Attack on Impact")]
    public float slamRadius = 5f;
    public float slamDamage = 40f;   // hurts player instead of enemies
    public LayerMask playerMask;     // make sure Player is on this layer

    [Header("Knockback Settings")]
    public float knockbackForce = 10f;  // horizontal push
    public float upwardForce = 8f;      // vertical lift
    public float outwardForce = 5f;     // extra outward (radial) push

    //[Header("Player Impact VFX")]
    //public GameObject PlayerImpactVFX;
    //public Vector3 PlayerImpactVFXOffset = Vector3.up * 1f;
    //public Vector3 PlayerImpactVFXEuler = Vector3.zero;
    //public Vector3 PlayerImpactVFXScale = Vector3.one;
    //public float PlayerImpactVFXLifetime = 5f;

    //[Header("Player Impact VFX 2")]
    //public GameObject PlayerImpactVFX2;
    //public Vector3 PlayerImpactVFX2Offset = Vector3.zero;
    //public Vector3 PlayerImpactVFX2Euler = new Vector3(-90f, 0f, -90f);
    //public Vector3 PlayerImpactVFX2Scale = Vector3.one;
    //public float PlayerImpactVFX2Lifetime = 25f;

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
                player.TakeDamagefromEnemy(slamDamage);   // simple health damage

                // === Knockback / Launch Upwards + Outwards ===
                var movement = player.GetComponent<PlayerMovement>(); // player movement script
                if (movement != null)
                {
                    Vector3 dir = (player.transform.position - transform.position).normalized;  //direction from impact and player get direction
                    dir.y = 0f; // base horizontal direction

                    // Combine outward + upward
                    Vector3 knockDir = (dir * (knockbackForce + outwardForce)) + Vector3.up * upwardForce;  //calc this and this goes ot player script

                    movement.ApplyExternalForce(knockDir);  // custom method in PlayerMovement
                }

                //// === Spawn Player Impact VFX 1 ===
                //if (PlayerImpactVFX != null)
                //{
                //    GameObject fx = Instantiate(PlayerImpactVFX, player.transform.position, Quaternion.identity);
                //    fx.transform.localPosition += PlayerImpactVFXOffset;
                //    fx.transform.localRotation = Quaternion.Euler(PlayerImpactVFXEuler);
                //    fx.transform.localScale = PlayerImpactVFXScale;
                //    Destroy(fx, PlayerImpactVFXLifetime);
                //}

                //// === Spawn Player Impact VFX 2 ===
                //if (PlayerImpactVFX2 != null)
                //{
                //    GameObject fx = Instantiate(PlayerImpactVFX2, player.transform.position, Quaternion.identity);
                //    fx.transform.localPosition += PlayerImpactVFX2Offset;
                //    fx.transform.localRotation = Quaternion.Euler(PlayerImpactVFX2Euler);
                //    fx.transform.localScale = PlayerImpactVFX2Scale;
                //    Destroy(fx, PlayerImpactVFX2Lifetime);
                //}
            }
        }
    }
}
