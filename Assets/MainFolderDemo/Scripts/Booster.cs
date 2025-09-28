// LaunchPad.cs
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

        var pm = root.GetComponent<PlayerMovement>();
        var cc = root.GetComponent<CharacterController>();
        var rb = root.GetComponent<Rigidbody>();

        if (pm) pm.EnableKineticJumpNow();  // keep this if you want the booster to grant KJ

        Vector3 dir = transform.forward.normalized;

        if (cc) { StartCoroutine(LaunchCC(cc, dir, pm)); return; }
        if (rb) { StartCoroutine(LaunchRB(rb, dir, pm)); }
    }

    IEnumerator LaunchCC(CharacterController cc, Vector3 dir, PlayerMovement pm)
    {
        float end = Time.time + launchDuration;

        // BOOST PHASE
        while (Time.time < end)
        {
            // if player started slam, give control back immediately
            if (pm && pm.IsSlamming()) yield break;

            cc.Move(dir * launchSpeed * Time.deltaTime);
            yield return null;
        }

        // MOMENTUM PHASE
        Vector3 vel = dir * launchSpeed;
        while (true)
        {
            if (pm && pm.IsSlamming()) yield break;

            float dt = Time.deltaTime;
            vel += Physics.gravity * dt;
            cc.Move(vel * dt);

            if (cc.isGrounded && vel.y <= 0f) yield break;
            yield return null;
        }
    }

    IEnumerator LaunchRB(Rigidbody rb, Vector3 dir, PlayerMovement pm)
    {
        if (rb.isKinematic) rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        float end = Time.time + launchDuration;

        // BOOST PHASE
        while (Time.time < end)
        {
            // if the player started slamming, stop boosting immediately
            if (pm != null && pm.IsSlamming())
                yield break;

            // run on physics tick
            yield return new WaitForFixedUpdate();

            // push rigidbody in launch direction
            rb.linearVelocity = dir * launchSpeed;
        }

        // after this, normal physics (including gravity) takes over
    }

}
