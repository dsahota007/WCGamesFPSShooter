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
    public float attackAnimLength = 4.5f;   // how long the animation lasts
    private bool isAttacking = false;

    [Header("Attack VFX")]
    public GameObject attackVFXPrefab;
    public Transform vfxSpawnPoint;
    public float vfxDelay = 0.5f;           // when to spawn effect after anim starts

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
        if (target != null && enemyAgent.isOnNavMesh && !isAttacking)
        {
            if (CanSeePlayer())
            {
                StartCoroutine(AttackCycle());
            }
            else
            {
                enemyAgent.isStopped = false;
                enemyAgent.SetDestination(target.position);
            }
        }

        if (animator != null)
        {
            float speed = enemyAgent.velocity.magnitude;
            animator.SetFloat("Speed", speed);

            if (speed > 0.1f && animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
            {
                int randomRun = Random.Range(0, 2);
                animator.SetInteger("RunIndex", randomRun);
            }
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
        isAttacking = true;
        enemyAgent.isStopped = true;
        animator.SetFloat("Speed", 0f);

        // Play attack
        animator.ResetTrigger("Attack");
        animator.SetTrigger("Attack");

        // Spawn VFX with delay
        yield return new WaitForSeconds(vfxDelay);
        if (attackVFXPrefab != null)
        {
            Transform spawnAt = vfxSpawnPoint != null ? vfxSpawnPoint : transform;
            Instantiate(attackVFXPrefab, spawnAt.position, spawnAt.rotation);
        }

        // Wait until animation finishes
        yield return new WaitForSeconds(attackAnimLength - vfxDelay);

        // ✅ Resume chasing IMMEDIATELY after animation
        enemyAgent.isStopped = false;

        // ✅ But still enforce cooldown before next attack
        float cooldown = Random.Range(attackCooldownMin, attackCooldownMax);
        yield return new WaitForSeconds(cooldown);

        isAttacking = false;
    }

}
