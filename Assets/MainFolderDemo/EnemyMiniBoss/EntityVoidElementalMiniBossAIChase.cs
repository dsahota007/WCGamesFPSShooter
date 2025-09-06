using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EntityVoidElementalMiniBossAIChase : MonoBehaviour
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

    [Header("Projectile Attack Settings")]
    public float projectileDelay = 1f;          // delay before first projectile is fired
    public float projectileSpeed = 10f;         // speed of fireball/iceball/whatever
    public float projectileLifetime = 5f;       // how long projectile exists
    public Transform projectileSpawnPoint;      // where projectile comes out of

    [Header("Projectile Prefab")]
    public GameObject projectilePrefab;       // reference to fireball prefab (BallEntity, Iceball, etc.)

    [Header("Multi-Shot Settings")]
    public int projectilesPerAttack = 3;        //NEW: how many projectiles fired in a single attack cycle
    public float timeBetweenProjectiles = 0.4f; //spacing between each projectile fired

    [Header("Launch VFX")]
    public GameObject launchVFXPrefab;        // VFX that appears when miniboss is preparing to launch
    public float launchVFXDelay = 0.5f;       // delay before showing launch VFX
    public float launchVFXLifetime = 2f;      // how long launch VFX lasts
    public Vector3 launchVFXOffset = Vector3.zero; // offset relative to spawn point
    public Vector3 launchVFXEuler = Vector3.zero;  // rotation override
    public Vector3 launchVFXScale = Vector3.one;   // scale

    [Header("Aura VFX")]
    public GameObject auraVFXPrefab;
    public Vector3 auraVFXOffset = Vector3.zero;   // position offset
    public Vector3 auraVFXEuler = Vector3.zero;    // rotation override
    public Vector3 auraVFXScale = Vector3.one;     // local scale

    private GameObject activeAuraVFX;

    void Start()
    {
        enemyAgent = GetComponent<NavMeshAgent>();  //fetch navMesh
        animator = GetComponent<Animator>();        //fetch animator
        healthScript = GetComponent<EnemyHealthRagdoll>();  //fetch health script

        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");  // find anything with player tag 
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

        if (target != null && enemyAgent.isOnNavMesh)   //is navmesh is true so this is attach to enemy so their navmesh 
        {
            if (!isAttacking && !isInCooldown && CanSeePlayer())
            {
                StartCoroutine(AttackCycle());    //attack if ur not attacking not cooldown and can see
            }
            else if (!isAttacking)
            {
                enemyAgent.isStopped = false;                   //turn off stopping so he can move
                enemyAgent.SetDestination(target.position);     //set destination to player so they can follow on the navmesh
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
            if (dir.sqrMagnitude > 0.01f)  //Make sure the direction vector isn’t “almost zero”
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);  //build rotation look towards player
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, 200f * Time.deltaTime);
            }
        }
    }

    bool CanSeePlayer()
    {
        Vector3 dirToPlayer = (target.position - transform.position).normalized;   //find direction of the enemy to the player 
        float distanceToPlayer = Vector3.Distance(transform.position, target.position);  //calc the dist between them as well

        if (distanceToPlayer < sightRange)   //create a range so if were in range
        {
            float angle = Vector3.Angle(transform.forward, dirToPlayer);   //if were in front of the player directly (face to face)
            if (angle < sightAngle / 2f)  //Only proceed if the player is inside the vision cone (field of view).
            {
                if (!Physics.Raycast(transform.position + Vector3.up, dirToPlayer, distanceToPlayer, obstructionMask))  //if your not hitting a wall then return true 
                {
                    return true;
                }
            }
        }
        return false;  //you're looking at a wall then
    }

    IEnumerator AttackCycle()
    {
        isAttacking = true;
        isInCooldown = true;

        enemyAgent.isStopped = true;
        enemyAgent.velocity = Vector3.zero;

        animator.SetTrigger("Attack");
        animator.SetFloat("Speed", 0f);

        // === Wait before showing launch VFX ===
        if (launchVFXPrefab != null && launchVFXDelay > 0f)
            yield return new WaitForSeconds(launchVFXDelay);

        // === Spawn launch VFX ===
        if (launchVFXPrefab != null)
        {
            Transform spawnAt = projectileSpawnPoint != null ? projectileSpawnPoint : transform;

            GameObject fx = Instantiate(
                launchVFXPrefab,
                spawnAt.position + spawnAt.TransformDirection(launchVFXOffset),
                spawnAt.rotation
            );

            fx.transform.localRotation = Quaternion.Euler(launchVFXEuler);
            fx.transform.localScale = launchVFXScale;

            Destroy(fx, launchVFXLifetime);
        }

        // === Wait remaining time until projectile fires ===
        float remaining = Mathf.Max(0f, projectileDelay - launchVFXDelay);
        if (remaining > 0f)
            yield return new WaitForSeconds(remaining);

        //loop to fire multiple projectiles per attack cycle
        for (int i = 0; i < projectilesPerAttack; i++)
        {
            if (projectilePrefab != null && target != null)
            {
                Transform spawnAt = projectileSpawnPoint != null ? projectileSpawnPoint : transform;
                GameObject proj = Instantiate(projectilePrefab, spawnAt.position, spawnAt.rotation);

                Rigidbody rb = proj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // Calculate direction for each projectile (still locked-on to player each shot)
                    Vector3 dir = (target.position - spawnAt.position).normalized;
                    rb.linearVelocity = dir * projectileSpeed;
                }

                Destroy(proj, projectileLifetime);
            }

            // Wait between each projectile, except after the last one
            if (i < projectilesPerAttack - 1)
                yield return new WaitForSeconds(timeBetweenProjectiles);
        }

        yield return new WaitForSeconds(attackAnimLength - projectileDelay);
        isAttacking = false;

        float cooldown = Random.Range(attackCooldownMin, attackCooldownMax);
        yield return new WaitForSeconds(cooldown);
        isInCooldown = false;
    }
}