// LaunchPad.cs
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class LaunchPad : MonoBehaviour
{
    [Header("Launch")]
    public float launchSpeed = 24f;
    //public float launchDuration = 1.25f;
    public string playerTag = "Player";
    public bool grantKineticJump = true;

    [Header("Cancel Conditions")]
    public bool cancelOnDash = true;   // stop pad the moment player dashes
    public bool cancelOnSlam = true;   // stop pad the moment player starts slam

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        // find the player root
        var root = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;

        var pm = root.GetComponent<PlayerMovement>();
        var cc = root.GetComponent<CharacterController>();
        var rb = root.GetComponent<Rigidbody>();

        // If already dashing and we don't want to interfere, ignore pad
        if (pm && cancelOnDash && pm.IsDashing()) return;

        // Give Kinetic Jump window if desired
        if (pm && grantKineticJump) pm.EnableKineticJumpNow();

        Vector3 dir = transform.forward.normalized;

        if (cc) { StartCoroutine(LaunchCC(cc, dir, pm)); return; }
        if (rb) { StartCoroutine(LaunchRB(rb, dir, pm)); }
    }

    IEnumerator LaunchCC(CharacterController cc, Vector3 dir, PlayerMovement pm)
    {
        //float end = Time.time + launchDuration;

        // BOOST PHASE (actively push)
        //while (Time.time )
        //{
            if (ShouldCancel(pm)) yield break;

            cc.Move(dir * launchSpeed * Time.deltaTime);
            yield return null;
        

        // MOMENTUM PHASE (coast with gravity until grounded)
        Vector3 vel = dir * launchSpeed;
        while (true)
        {
            if (ShouldCancel(pm)) yield break;

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

        //float end = Time.time + launchDuration;

        // BOOST PHASE (actively set velocity)
        //while (Time.time < end)
        //{
            if (ShouldCancel(pm)) yield break;

            yield return new WaitForFixedUpdate();
            rb.linearVelocity = dir * launchSpeed;
        

        // After boost, RB just falls under normal physics
    }

    // Helper to centralize cancel rules
    bool ShouldCancel(PlayerMovement pm)
    {
        if (pm == null) return false;
        if (cancelOnSlam && pm.IsSlamming()) return true;
        if (cancelOnDash && pm.IsDashing()) return true;
        return false;
    }
}
