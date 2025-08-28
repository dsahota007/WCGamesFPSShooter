using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class MiniBossAIChase : MonoBehaviour
{
    private NavMeshAgent enemyAgent;
    public Transform target;
    private Animator animator;

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

    [Header("Attack VFX")]
    public GameObject attackVFXPrefab;
    public Transform vfxSpawnPoint;

    private float vfxTimer = 0f;         // time tracking for active flames

    void Start()
    {
        enemyAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                target = playerObj.transform;
        }
    }

    void Update()
    {
        if (target != null && enemyAgent.isOnNavMesh)
        {
            // Only check for attacks if not currently attacking and not in cooldown
            if (!isAttacking && !isInCooldown && CanSeePlayer())
            {
                StartCoroutine(AttackCycle());
            }
            // Only chase if not attacking
            else if (!isAttacking)
            {
                enemyAgent.isStopped = false;
                enemyAgent.SetDestination(target.position);
            }
        }

        // Handle animations - just Speed parameter for run/stop
        if (animator != null)
        {
            if (isAttacking)
            {
                // Stop moving, speed = 0 for attack state
                animator.SetFloat("Speed", 0f);
            }
            else
            {
                // Normal movement - speed > 0 for running
                float speed = enemyAgent.velocity.magnitude;
                animator.SetFloat("Speed", speed);

                // Set random run index when starting to run
                if (speed > 0.1f)
                {
                    int randomRun = Random.Range(0, 2);
                    animator.SetInteger("RunIndex", randomRun);
                }
            }
        }

        // During attack, rotate toward player (but don't move)
        if (isAttacking && target != null)
        {
            Vector3 dir = (target.position - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, 200f * Time.deltaTime);
            }
        }

        // Apply damage only when VFX window is active
        if (isAttacking && vfxTimer > 0f)
        {
            DoConeDamage();
            vfxTimer -= Time.deltaTime;
        }
    }

    bool CanSeePlayer()
    {
        Vector3 dirToPlayer = (target.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, target.position);

        if (distanceToPlayer < sightRange)
        {
            float angle = Vector3.Angle(transform.forward, dirToPlayer);
            if (angle < sightAngle / 2f)
            {
                if (!Physics.Raycast(transform.position + Vector3.up, dirToPlayer, distanceToPlayer, obstructionMask))
                {
                    return true;
                }
            }
        }
        return false;
    }

    IEnumerator AttackCycle()
    {
        // Set attack state
        isAttacking = true;
        isInCooldown = true;

        // Stop movement completely
        enemyAgent.isStopped = true;
        enemyAgent.velocity = Vector3.zero;

        // Trigger attack animation 
        animator.SetTrigger("Attack");
        // Set speed to 0 so animator transitions to attack state
        animator.SetFloat("Speed", 0f);

        // Wait before flames start
        yield return new WaitForSeconds(vfxDelay);

        // Spawn VFX
        if (attackVFXPrefab != null)
        {
            Transform spawnAt = vfxSpawnPoint != null ? vfxSpawnPoint : transform;
            GameObject vfx = Instantiate(attackVFXPrefab, spawnAt.position, spawnAt.rotation, spawnAt);
            Destroy(vfx, vfxLifetime);
        }

        // Enable damage window
        vfxTimer = vfxLifetime;

        // Wait until animation finishes
        yield return new WaitForSeconds(attackAnimLength - vfxDelay);

        // Attack animation done - ready to resume movement
        isAttacking = false;

        // Cooldown period before next attack
        float cooldown = Random.Range(attackCooldownMin, attackCooldownMax);
        yield return new WaitForSeconds(cooldown);

        // Ready to attack again
        isInCooldown = false;
    }

    void DoConeDamage()
    {
        if (target == null) return;

        Vector3 dirToPlayer = (target.position - transform.position).normalized;
        dirToPlayer.y = 0f;
        float distance = Vector3.Distance(transform.position, target.position);
        float angle = Vector3.Angle(transform.forward, dirToPlayer);

        if (distance <= attackRange && angle <= attackAngle / 2f)
        {
            PlayerAttributes player = target.GetComponent<PlayerAttributes>();
            if (player != null)
            {
                player.TakeDamagefromEnemy(vfxDamage * Time.deltaTime);
            }
        }
    }
}