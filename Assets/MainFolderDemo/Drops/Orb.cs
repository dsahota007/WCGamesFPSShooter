using UnityEngine;

public class SimpleOrbHoming : MonoBehaviour
{
    [Header("Attach point (auto if left blank)")]
    public Transform target;                         // auto-finds Player/AbsorbPoint if null
    public string playerTag = "Player";
    public string absorbChildName = "AbsorbPoint";

    [Header("Movement")]
    public float speed = 12f;
    public float attractDistance = 6f;               // only home when within this distance
    public float snapDistance = 0.1f;                // stop here and destroy
    public float startDelay = 0f;

    void Awake()
    {
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                var child = player.transform.Find(absorbChildName);
                target = child != null ? child : player.transform; // fallback to player
            }
        }
    }

    void Start()
    {
        Destroy(gameObject, 10f);
    }

    void Update()
    {
        // wait before starting to home
        if (startDelay > 0f)
        {
            startDelay -= Time.deltaTime;
            return;
        }

        if (!target) return;

        Vector3 to = target.position - transform.position;
        float d = to.magnitude;

        // only start homing when close enough
        if (d > attractDistance) return;

        // snap + finish
        if (d <= snapDistance)
        {
            transform.position = target.position;
            var pm = PointManager.Instance;
            if (pm != null) pm.AddPoints(5);
            Destroy(gameObject);
            return;
        }

        // move toward target
        transform.position += (to / Mathf.Max(d, 0.0001f)) * speed * Time.deltaTime;
    }


    // Optional: set at runtime (e.g., from a spawner)
    public void SetTarget(Transform t) => target = t;
}
