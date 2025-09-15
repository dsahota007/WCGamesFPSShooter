using UnityEngine;

public class SimpleDebris : MonoBehaviour
{
    [Header("Setup")]
    public Transform player;
    public float interactRange = 3f;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float destroyDelay = 2f;

    [Header("Cost")]
    public int costToOpen = 750;

    private bool isMoving = false;

    void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(player.position, transform.position);

        // Interact + in range + enough points -> start moving & schedule destroy
        if (!isMoving && distance <= interactRange && KeybindManager.Instance.GetKeyDown("Interact"))
        {
            var pm = PointManager.Instance;
            if (pm != null && pm.TrySpend(costToOpen))
            {
                isMoving = true;
                Invoke(nameof(DestroySelf), destroyDelay);
            }
            // else: not enough points or no PointManager -> do nothing (kept simple as requested)
        }

        if (isMoving)
        {
            transform.position += Vector3.up * moveSpeed * Time.deltaTime;
        }
    }

    void DestroySelf()
    {
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
