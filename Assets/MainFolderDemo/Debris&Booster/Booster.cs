// LaunchPad.cs
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LaunchPad : MonoBehaviour
{
    [Header("Launch")] 
    public float launchSpeed = 24f;  // Upward speed to add when hitting the pad.
    public float forwardBoost = 0f;   // Optional extra forward boost along the pad's forward.
    public string playerTag = "Player";      // Tag of the player object that should be launched.
    public bool grantKineticJump = true;        // If true, calls EnableKineticJumpNow on PlayerMovement when launched.
    [Header("Cancel / Ignore Conditions")]
    public bool ignoreWhileDashing = true;   // If true, pad does nothing if the player is currently dashing.
    public bool ignoreWhileSlamming = true;   // If true, pad does nothing if the player is currently slamming

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) == false)
        {
            return;
        }

        // find the player root
        GameObject root;
        if (other.attachedRigidbody != null)
        {
            root = other.attachedRigidbody.gameObject;
        }
        else
        {
            root = other.gameObject;
        }

        var pm = root.GetComponent<PlayerMovement>();
        var cc = root.GetComponent<CharacterController>();
        var rb = root.GetComponent<Rigidbody>();

        // if we have PlayerMovement, use it as the main authority
        if (pm != null)
        {
            if (ignoreWhileDashing == true && pm.IsDashing() == true)
            {
                return;
            }

            if (ignoreWhileSlamming == true && pm.IsSlamming() == true)
            {
                return;
            }

            // Give Kinetic Jump window if desired
            if (grantKineticJump == true)
            {
                pm.EnableKineticJumpNow();
            }

            // --- MAIN BEHAVIOR: add an upward launch impulse ---
            // This hands the Y-velocity to your normal movement system.
            if (launchSpeed > 0f)
            {
                pm.AddUpwardVelocity(launchSpeed);
            }

            // Optional forward impulse: your normal air control + slam still work.
            if (forwardBoost != 0f)
            {
                // Example if you add this later:
                // pm.AddHorizontalImpulse(transform.forward * forwardBoost);
            }

            // Optional: a tiny camera shake on launch (NOT on landing)
            CameraScript.Main?.Shake(0.25f, 1.5f, 40f, true);
            return;
        }

        // ---- Fallbacks if there is NO PlayerMovement ----

        // Rigidbody-based character: set their velocity directly
        if (rb != null)
        {
            if (rb.isKinematic == true)
            {
                rb.isKinematic = false;
            }

            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            Vector3 v = rb.linearVelocity;

            // Upward launch
            if (launchSpeed > 0f)
            {
                // keep whichever is stronger so you don't "kill" an existing bigger jump
                v.y = Mathf.Max(v.y, launchSpeed);
            }

            // Optional forward boost
            if (forwardBoost != 0f)
            {
                v += transform.forward * forwardBoost;
            }

            rb.linearVelocity = v;

            CameraScript.Main?.Shake(0.25f, 1.5f, 40f, true);
            return;
        }

        // CharacterController without PlayerMovement:
        // we can only do a small, instant upward move – after that,
        // your own movement code is in charge.
        if (cc != null)
        {
            Vector3 move = Vector3.up * launchSpeed * Time.deltaTime;

            if (forwardBoost != 0f)
            {
                move += transform.forward * forwardBoost * Time.deltaTime;
            }

            cc.Move(move);
            CameraScript.Main?.Shake(0.25f, 1.5f, 40f, true);
        }
    }
}
