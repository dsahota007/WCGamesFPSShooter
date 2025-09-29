using UnityEngine;
using System.Collections;

public class EnemyAIChaseFlying : MonoBehaviour
{
    // movement
    public float speed = 6f;
    public float turnSpeed = 360f;     // deg/sec
    public float stopDistance = 0.5f;  // don't overlap player
    public bool yawOnly = true;        // rotate only left/right (keep upright)

    // attack
    public float attackRange = 2f;
    public float attackCooldown = 1.2f;
    public float projectileSpeed = 18f;

    // shooting
    public Transform attackPoint;      // where bullets spawn (fallback = this transform)
    public GameObject attackPrefab;    // projectile prefab (with optional Rigidbody)
    public float projectileLife = 3f;

    // visual facing (optional)
    public Transform visual;           // rotate this child; fallback = self
    public float modelYawOffset = 180f;

    // target
    public string playerTag = "Player";

    Transform target;
    bool attacking;
    float nextAttackTime;

    // burst constants (keep simple)
    const int BurstCount = 3;
    const float BurstInterval = 0.12f;

    void Awake()
    {
        if (!visual) visual = transform;
        if (!attackPoint) attackPoint = transform;

        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p) target = p.transform;
    }

    void Update()
    {
        if (!target) return;

        Vector3 to = target.position - transform.position;
        float d = to.magnitude;

        // face target
        Vector3 face = yawOnly ? new Vector3(to.x, 0f, to.z) : to;
        if (face.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(face.normalized, Vector3.up)
                            * Quaternion.Euler(0f, modelYawOffset, 0f);
            visual.rotation = Quaternion.RotateTowards(visual.rotation, look, turnSpeed * Time.deltaTime);
        }

        // attack if close and off cooldown
        if (!attacking && d <= attackRange && Time.time >= nextAttackTime)
        {
            StartCoroutine(FireBurst());
            return;
        }

        // move when not attacking
        if (!attacking && d > stopDistance)
            transform.position += to.normalized * speed * Time.deltaTime;
    }

    IEnumerator FireBurst()
    {
        attacking = true;
        nextAttackTime = Time.time + attackCooldown;

        for (int i = 0; i < BurstCount; i++)
        {
            // aim at player now
            Vector3 dir = attackPoint.forward;
            if (target)
            {
                dir = (target.position - attackPoint.position).normalized;
                if (dir.sqrMagnitude < 0.0001f) dir = attackPoint.forward;
            }

            // spawn projectile
            if (attackPrefab)
            {
                var go = Instantiate(attackPrefab, attackPoint.position, Quaternion.LookRotation(dir, Vector3.up));
                if (projectileLife > 0f) Destroy(go, projectileLife);

                var rb = go.GetComponent<Rigidbody>();
                if (rb)
                {
                    // if you're on Unity 6, you can use rb.linearVelocity instead
                    rb.linearVelocity = dir * projectileSpeed;
                }
            }

            if (i < BurstCount - 1)
                yield return new WaitForSeconds(BurstInterval);
        }

        // done
        attacking = false;
    }
}
