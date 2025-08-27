using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class MiniBossAIChase : MonoBehaviour
{
    private NavMeshAgent enemyAgent;
    public Transform target;
    private Animator animator;

    [Header("Vision Settings")]
    public float sightRange = 15f;      // how far miniboss can see
    public float sightAngle = 60f;      // FOV angle
    public LayerMask obstructionMask;   // walls/objects that block vision

    private bool isWaiting = false;

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
        if (target != null && enemyAgent.isOnNavMesh && !isWaiting)
        {
            if (CanSeePlayer())
            {
                StartCoroutine(StopAndWait());
            }
            else
            {
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
                Debug.Log($"Switched to RunIndex: {randomRun}");
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
                // raycast to check line of sight
                if (!Physics.Raycast(transform.position + Vector3.up, dirToPlayer, distanceToPlayer, obstructionMask))
                {
                    return true;
                }
            }
        }
        return false;
    }

    IEnumerator StopAndWait()
    {
        isWaiting = true;
        enemyAgent.isStopped = true;
        animator.SetFloat("Speed", 0f);

        Debug.Log("[MiniBoss] Spotted player! Standing still for 3 seconds.");
        yield return new WaitForSeconds(3f);

        enemyAgent.isStopped = false;
        isWaiting = false;
    }
}
