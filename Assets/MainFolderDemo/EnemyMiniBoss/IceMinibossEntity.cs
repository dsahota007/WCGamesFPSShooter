using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class IceMinibossEntity : MonoBehaviour
{
    [Header("Slam")]
    public float slamRadius = 5f;
    public float slamDamage = 40f;
    public LayerMask playerMask;

    [Header("Slow")]
    [Range(0.05f, 1f)] public float slowMultiplier = 0.5f; // 0.5 = 50% speed
    public float slowDuration = 3f;                         // seconds

    [Header("VFX Effects")]
    public GameObject PlayerImpactVFX;
    public GameObject PlayerImpactVFX2;

    [Header("Ground Slam VFX")]
    public GameObject GroundEntitySlamVFX;
    public Vector3 GroundVFXOffset = Vector3.zero;  // local/world offset
    public Vector3 GroundVFXEuler = Vector3.zero;   // rotation override
    public Vector3 GroundVFXScale = Vector3.one;    // scale override
    public float GroundVFXLifetime = 10f;           // how long it lasts

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ground") || other.CompareTag("Wall"))
        {
            Impact();                // apply damage + slow + player VFX
            SpawnGroundEffects();    // ground decal/impact (same as other script)
            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Player"))
        {
            Impact();                // no ground VFX on direct player hit (same behavior)
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision _)
    {
        // Backup collision detection in case trigger doesn't work
        Impact();
        SpawnGroundEffects();
        Destroy(gameObject);
    }

    void Impact()
    {
        var hits = Physics.OverlapSphere(transform.position, slamRadius, playerMask);

        foreach (var col in hits)
        {
            // damage
            var attrs = col.GetComponent<PlayerAttributes>() ?? col.GetComponentInParent<PlayerAttributes>();
            if (attrs != null)
            {
                attrs.TakeDamagefromEnemy(slamDamage);   // simple health damage

                // same player VFX as your other script
                if (PlayerImpactVFX != null)
                {
                    GameObject fx = Instantiate(PlayerImpactVFX, attrs.transform.position + Vector3.up * 1f, Quaternion.identity);
                    Destroy(fx, 5f);
                }

                if (PlayerImpactVFX2 != null)
                {
                    GameObject fx = Instantiate(PlayerImpactVFX2, attrs.transform.position, Quaternion.Euler(-90f, 0f, -90f));
                    Destroy(fx, 25f);
                }
            }

            // slow (no PlayerMovement edits needed)
            var pm = col.GetComponent<PlayerMovement>() ?? col.GetComponentInParent<PlayerMovement>();
            if (pm != null) pm.StartCoroutine(SlowFor(pm, slowMultiplier, slowDuration));
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

    static IEnumerator SlowFor(PlayerMovement pm, float mult, float dur)
    {
        // snapshot current speeds, apply slow
        float ow = pm.walkSpeed, os = pm.sprintSpeed, oa = pm.aimSpeed;
        pm.walkSpeed = ow * mult;
        pm.sprintSpeed = os * mult;
        pm.aimSpeed = oa * mult;

        yield return new WaitForSeconds(dur);

        // restore (simple & minimal — overlapping slows will just restore to the last snapshot)
        if (pm != null)
        {
            pm.walkSpeed = ow;
            pm.sprintSpeed = os;
            pm.aimSpeed = oa;
        }
    }
}
