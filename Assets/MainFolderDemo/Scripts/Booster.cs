using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class LaunchPad : MonoBehaviour
{
    public float launchSpeed = 24f;
    public float launchDuration = 1.25f;
    public string playerTag = "Player";

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        var root = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;

        var cc = root.GetComponent<CharacterController>();
        if (cc) { StartCoroutine(LaunchCC(cc, transform.forward.normalized)); return; }

        var rb = root.GetComponent<Rigidbody>();
        if (rb) { StartCoroutine(LaunchRB(rb, transform.forward.normalized)); }
    }

    IEnumerator LaunchCC(CharacterController cc, Vector3 dir)
    {
        float end = Time.time + launchDuration;

        // boost phase (constant velocity in 'dir')
        while (Time.time < end)
        {
            cc.Move(dir * launchSpeed * Time.deltaTime);
            yield return null;
        }

        // momentum phase (carry velocity; gravity affects Y)
        Vector3 vel = dir * launchSpeed;
        while (true)
        {
            float dt = Time.deltaTime;
            vel += Physics.gravity * dt;
            cc.Move(vel * dt);

            // stop once we land while moving downward
            if (cc.isGrounded && vel.y <= 0f) yield break;

            yield return null;
        }
    }

    IEnumerator LaunchRB(Rigidbody rb, Vector3 dir)
    {
        if (rb.isKinematic) rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        float end = Time.time + launchDuration;

        // boost phase (lock velocity)
        while (Time.time < end)
        {
            yield return new WaitForFixedUpdate();
            rb.linearVelocity = dir * launchSpeed;
        }

        // after this, physics (including gravity) just takes over
    }
}
