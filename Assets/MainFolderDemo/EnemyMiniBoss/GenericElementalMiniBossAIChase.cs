using UnityEngine;
using UnityEngine.AI;
using System.Collections;
 

public class GenericElementalMiniBossAIChase : MonoBehaviour
{
    private NavMeshAgent enemyAgent;
    public Transform target;
    private Animator animator;
    private EnemyHealthRagdoll healthScript; 

    [Header("Vision Settings")]
    public float sightRange = 15f;
    public float sightAngle = 60f;
    public LayerMask obstructionMask;

    [Header("Attack Settings")]
    public float attackCooldownMin = 3f;
    public float attackCooldownMax = 5f;
    public float attackAnimLength = 4.5f;
    private bool isAttacking = false;
    private bool isInCooldown = false;

    [Header("Attack Damage Settings")]
    public float vfxDelay = 0.5f;        // delay before fire "comes out"
    public float vfxLifetime = 2f;       // how long the flames are active
    public float vfxDamage = 10f;        // damage per second
    public float attackRange = 6f;       // how far the flames reach
    public float attackAngle = 60f;      // cone angle
    public float damageVFXDelay = 1f;

    [Header("Attack VFX")]
    public GameObject attackVFXPrefab;
    public Transform vfxSpawnPoint;

    private float vfxTimer = 0f;         // time tracking for active flames

    [Header("Aura VFX")]
    public GameObject auraVFXPrefab;
    public Vector3 auraVFXOffset = Vector3.zero;   // position offset
    public Vector3 auraVFXEuler = Vector3.zero;    // rotation override
    public Vector3 auraVFXScale = Vector3.one;     // local scale

    private GameObject activeAuraVFX;
 


    void Start()
    {
        enemyAgent = GetComponent<NavMeshAgent>();  //fethc navMesh
        animator = GetComponent<Animator>();        //fetch animator
        healthScript = GetComponent<EnemyHealthRagdoll>();  //fetch health script

        if (target == null) 
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");  // find anythign with player tag 
            if (playerObj != null)
                target = playerObj.transform; //make the target the player
        }
         
        if (auraVFXPrefab != null)
        {
            activeAuraVFX = Instantiate(auraVFXPrefab, transform);
            activeAuraVFX.transform.localPosition = auraVFXOffset;
            activeAuraVFX.transform.localRotation = Quaternion.Euler(auraVFXEuler);
            activeAuraVFX.transform.localScale = auraVFXScale;
        }
    }

    void Update()
    {

        if (healthScript != null && healthScript.IsDead())
        {
            if (activeAuraVFX != null)
            {
                Destroy(activeAuraVFX);
                activeAuraVFX = null;
            }
            return;
        }

        if (healthScript != null && healthScript.IsDead())
        {
            if (attackVFXPrefab != null) Destroy(attackVFXPrefab);
            isAttacking = false;
            return;
        }

        if (target != null && enemyAgent.isOnNavMesh)   //is navmesh is true so this is attach to enemy so their navmesh 
        {
            if (!isAttacking && !isInCooldown && CanSeePlayer())        
            {
                StartCoroutine(AttackCycle());    //attack if ur not attacking not cooldown and can see
            }
            else if (!isAttacking)
            {
                enemyAgent.isStopped = false;                   //turn off stopping so he can move
                enemyAgent.SetDestination(target.position);       //set deestiantion to player so they can follow on the navmesh
            }
        }

        if (animator != null) // Handle animations - just Speed parameter for run/stop
        {
            if (isAttacking)   
            {
                animator.SetFloat("Speed", 0f);    // Stop moving, speed = 0 for attack state
            }
            else
            {
                // Normal movement - speed > 0 for running
                float speed = enemyAgent.velocity.magnitude;
                animator.SetFloat("Speed", speed);

                if (speed > 0.1f)  //if moving ever
                {
                    int randomRun = Random.Range(0, 2);
                    animator.SetInteger("RunIndex", randomRun);
                }
            }
        }

        // During attack, rotate toward player (but don't move)
        if (isAttacking && target != null)
        {
            Vector3 dir = (target.position - transform.position);  //pointing vector --- player to enemy 
            dir.y = 0f;   //ignore up down diff
            if (dir.sqrMagnitude > 0.01f)  //Make sure the direction vector isn’t “almost zero” (e.g., player standing exactly on top of the miniboss).
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);  //build rotation look towards player
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, 200f * Time.deltaTime);  //RotateTowards(from , to and maxDegreeDelta
            }
        }

        if (isAttacking && vfxTimer > 0f)
        {
            DoConeDamage();
            vfxTimer -= Time.deltaTime;
        }
    }

    bool CanSeePlayer()
    {
        Vector3 dirToPlayer = (target.position - transform.position).normalized;   //find direction of the enenmy to the player 
        float distanceToPlayer = Vector3.Distance(transform.position, target.position);  //calc the dist between them as well

        if (distanceToPlayer < sightRange)   //create a range so if were in range
        {
            float angle = Vector3.Angle(transform.forward, dirToPlayer);   //if were in front of the player directly (face to face)
            if (angle < sightAngle / 2f)  //Only proceed if the player is inside the vision cone (field of view).
            {
                if (!Physics.Raycast(transform.position + Vector3.up, dirToPlayer, distanceToPlayer, obstructionMask))  //if your not hitting a wall than return true 
                {
                    return true;
                }
            }
        }
        return false;  //your looking at a wall than
    }

    IEnumerator AttackCycle()
    {
        isAttacking = true;
        isInCooldown = true;

        enemyAgent.isStopped = true;           // Stop movement completely
        enemyAgent.velocity = Vector3.zero;

        animator.SetTrigger("Attack");          // begin adn Trigger attack animation 
        animator.SetFloat("Speed", 0f);     // Set speed to 0 so animator transitions to attack state

        yield return new WaitForSeconds(vfxDelay); // Wait before flames start

        if (attackVFXPrefab != null)          // Spawn fire VFX
        {
            Transform spawnAt = vfxSpawnPoint != null ? vfxSpawnPoint : transform; //spawn at spawnPoint or the transform if empty (not important)
            GameObject vfx = Instantiate(attackVFXPrefab, spawnAt.position, spawnAt.rotation, spawnAt);
            Destroy(vfx, vfxLifetime);
        }

        yield return new WaitForSeconds(damageVFXDelay); //delay damage because it takes a sec for flames to begin
        vfxTimer = vfxLifetime - damageVFXDelay;  

        yield return new WaitForSeconds(attackAnimLength - vfxDelay);         // Wait until animation finishes
        isAttacking = false;   //when animation done you are no longer in attacking state

        // Cooldown period before next attack
        float cooldown = Random.Range(attackCooldownMin, attackCooldownMax);  //random range so theyer all not spawing the same time. 
        yield return new WaitForSeconds(cooldown);
        isInCooldown = false;    // Ready to attack again
    }

    void DoConeDamage()
    {
        if (target == null) return;  //GTFO this code 

        Vector3 dirToPlayer = (target.position - transform.position).normalized;   // find direciton 
        dirToPlayer.y = 0f;                     //ignore height 
        float distance = Vector3.Distance(transform.position, target.position);  //enemy to player dist
        float angle = Vector3.Angle(transform.forward, dirToPlayer);   //r

        if (distance <= attackRange && angle <= attackAngle / 2f)   ///in range and sight of the eplayer (FOV) 
        {
            PlayerAttributes player = target.GetComponent<PlayerAttributes>();
            if (player != null)
            {
                player.TakeDamagefromEnemy(vfxDamage * Time.deltaTime);  //do damage overall
            }
        }
    }

}