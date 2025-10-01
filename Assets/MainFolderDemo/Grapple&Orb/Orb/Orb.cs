using UnityEngine;

public class Orb : MonoBehaviour
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

    void Awake()    //finding prefab bc it wont let uis put in prefab for some reason
    {
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag(playerTag);  // find absorb 
            if (player != null)
            {
                var child = player.transform.Find(absorbChildName);
                target = child != null ? child : player.transform; // fallback to player
            }
        }
    }

    void Start()
    {
        Destroy(gameObject, 15f);
    }

    void Update()
    {
        // wait before starting to home
        if (startDelay > 0f)      // start timer so we dont pick right away
        {
            startDelay -= Time.deltaTime;    
            return;
        }

        if (!target) return;

        Vector3 to = target.position - transform.position;  //distance
        float direc = to.magnitude;   //find the direction by using magnitude
        if (direc > attractDistance) return;          // only start homing when close enough

        // snap + finish
        if (direc <= snapDistance)
        {
            transform.position = target.position;
            var pm = PointManager.Instance;
            if (pm != null)
            {
                pm.AddPoints(5);
            }
            Destroy(gameObject);
            return;
        }

        // move toward target
        //transform.position += (to / Mathf.Max(direc, 0.0001f)) * speed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);    //easier way to write this

    }


    //// Optional: set at runtime (e.g., from a spawner)
    //public void SetTarget(Transform t) => target = t;
}
