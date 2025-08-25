using UnityEngine;
//using static UnityEditor.PlayerSettings;

public class Bullet : MonoBehaviour
{
    public float speed = 20f;
    public float lifeTime = 2f;
    public float damage = 1f;
   
    
    public GameObject[] bloodEffects;
    public GameObject groundHitEffect;
    public LayerMask layersToIgnore;

    private static PlayerAttributes _playerCache;  //// Cache the player so we don’t search every hit  -- cirmson bullet infusion
    public Weapon sourceWeapon;  // set by Weapon.Shoot()

    Rigidbody rb;
    Collider myCol;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        myCol = GetComponent<Collider>();
    }
    void Start()
    {
        rb.linearVelocity = transform.forward * speed;

        Destroy(gameObject, lifeTime);

        Collider[] playerColliders = GameObject.FindGameObjectWithTag("Player").GetComponentsInChildren<Collider>();  // we use this to make sure it does not hit us 

        foreach (Collider col in playerColliders)
        {
            Physics.IgnoreCollision(GetComponent<Collider>(), col);
        }
    }

    //void Update()
    //{
    //    transform.position += transform.forward * speed * Time.deltaTime;
    //}

    void OnTriggerEnter(Collider other)
    {
        {
            if ((layersToIgnore.value & (1 << other.gameObject.layer)) != 0)
                return;
        }

        // Ground hit
        if (other.CompareTag("Ground"))
        {
            if (groundHitEffect)
                Instantiate(groundHitEffect, transform.position, Quaternion.identity);

            Destroy(gameObject);
            return;
        }

        // Only do blood + damage if we actually hit an enemy
        var enemy = other.GetComponentInParent<EnemyHealthRagdoll>();  // safer than tag
        if (enemy != null)
        {
            if (PointManager.Instance != null)
                PointManager.Instance.AddPoints(5);

            if (bloodEffects != null && bloodEffects.Length > 0)
            {
                int index = Random.Range(0, bloodEffects.Length);
                var blood = Instantiate(bloodEffects[index], transform.position, Quaternion.identity);
                blood.transform.SetParent(enemy.transform, true);
            }

            Vector3 dir = transform.forward;
            enemy.TakeDamage(damage, dir);

            //FIRE INDUSION (Rest in enemyHealthRagdoll)
            if (sourceWeapon != null && sourceWeapon.infusion == InfusionType.Fire)
            {
                enemy.ApplyFireInfusionEffect( sourceWeapon.fireDotDuration, sourceWeapon.fireDotPercentPerSec, sourceWeapon.fireOnEnemyVFXPrefab, 
                    sourceWeapon.fireOnEnemyVFXOffset, sourceWeapon.fireOnEnemyVFXEuler, sourceWeapon.fireOnEnemyVFXScale
                );
            }

            // CRYSTAL INFUSION
            if (sourceWeapon != null && sourceWeapon.infusion == InfusionType.Crystal)
            {
                Vector3 center = transform.position;   //find the center positon 

                if (sourceWeapon.crystalImpactVFXPrefab != null)       // Spawn VFX at impact
                {
                    var fx = Instantiate(sourceWeapon.crystalImpactVFXPrefab, center, Quaternion.identity);
                    fx.transform.SetParent(null, true);
                    fx.transform.position += sourceWeapon.crystalImpactVFXOffset;
                    fx.transform.rotation = Quaternion.Euler(sourceWeapon.crystalImpactVFXEuler);
                    fx.transform.localScale = sourceWeapon.crystalImpactVFXScale;
                    Destroy(fx, sourceWeapon.crystalImpactVFXLifetime);
                }

                // Find all enemies in radius
                Collider[] hits = Physics.OverlapSphere(center, sourceWeapon.crystalSplashRadius, sourceWeapon.crystalEnemyMask, QueryTriggerInteraction.Ignore);  // -- (center of the sphere, radisu of the sphere, layerMask,  Specifies whether this query should hit Triggers  tells Unity to ignore trigger colliders (only use solid hit colliders))  

                foreach (var c in hits)   //one collider from the sphere check
                {   // e is enemy
                    var e = c.GetComponentInParent<EnemyHealthRagdoll>();   //fetch script 
                    if (e == null || e.IsDead())  //ignore if nobody is there or theyre dead
                        continue;

                    float dmg = Mathf.Max(1f, e.Health * sourceWeapon.crystalSplashPercent);  // we dont need that 1f and math max this makes sure its never under 1 percent ? 
                    Vector3 pushDir = (e.transform.position - center).normalized;  //for ragdoll we find direction and in take damage implment that 

                    e.TakeDamage(dmg, pushDir); 
                }
            }


            // VENOM INFUSION (Rest in enemyHealthRagdoll)
            if (sourceWeapon != null && sourceWeapon.infusion == InfusionType.Venom)
            {
                enemy.ApplyVenomInfusionEffect( sourceWeapon.venomDotDuration, sourceWeapon.venomDotPercentPerSec, sourceWeapon.venomOnEnemyVFXPrefab,
                                        sourceWeapon.venomOnEnemyVFXOffset, sourceWeapon.venomOnEnemyVFXEuler, sourceWeapon.venomOnEnemyVFXScale
                );
            }

            // CIMRSON INFUSION ALL LOGIC
            if (sourceWeapon != null && sourceWeapon.infusion == InfusionType.Crimson)
            {
                // 0.1% of enemy MAX HP per bullet (tweak in Weapon inspector)
                float healAmt = enemy.Health * sourceWeapon.crimsonHealPercentPerHit; //WE Calc the amount so curren health of the enemy 

                // Heal player
                var player = GameObject.FindGameObjectWithTag("Player");  //find player with tag 
                if (player != null)
                {
                    var attrs = player.GetComponentInChildren<PlayerAttributes>();  //fethc attirbutes for health
                    if (attrs != null)
                    {
                        attrs.Heal(healAmt);         //heal the amount per bullet
                    }
                }
                // Spawn quick crimson VFX ON the enemy at custom offset/rotation/scale
                var vfxPrefab = sourceWeapon.crimsonOnEnemyVFXPrefab;   // spawn vfx 
                if (vfxPrefab != null)
                {
                    var fx = Instantiate(vfxPrefab, enemy.transform);
                    fx.transform.localPosition = sourceWeapon.crimsonOnEnemyVFXOffset;  
                    fx.transform.localRotation = Quaternion.Euler(sourceWeapon.crimsonOnEnemyVFXEuler);
                    fx.transform.localScale = sourceWeapon.crimsonOnEnemyVFXScale;
                    Destroy(fx, sourceWeapon.crimsonOnEnemyVFXLifetime);
                }
            }
        }

        Destroy(gameObject);
    }
}