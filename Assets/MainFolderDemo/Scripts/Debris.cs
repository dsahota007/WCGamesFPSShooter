using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Debris : MonoBehaviour
{
    [Header("Interaction")]
    public float interactRange = 3f;
    public int costToOpen = 750;
    public Transform player;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float destroyDelay = 2f;   // time it keeps moving up before destroy

    private bool isOpened = false;

    // fetched from UI (do NOT assign here)
    private Text promptText;

    void Start()
    {
        // find player if not set
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        // fetch the single shared DebrisText from your UI script
        var ui = FindFirstObjectByType<UI>();
        if (ui != null) promptText = ui.DebrisText;

        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(player.position, transform.position);
        bool inRange = dist <= interactRange;

        // show/hide prompt while not opened
        if (!isOpened && promptText != null)
        {
            if (inRange)
            {
                int pts = (PointManager.Instance != null) ? PointManager.Instance.GetPoints() : 0;
                if (pts < costToOpen)
                {
                    promptText.text = $"Need {costToOpen} Points to Clear Debris";
                }
                else
                {
                    string key = KeybindManager.Instance != null
                        ? KeybindManager.Instance.GetKeyName("Interact")
                        : "E";
                    promptText.text = $"Press [{key}] to Clear Debris ({costToOpen})";
                }

                if (!promptText.gameObject.activeSelf)
                    promptText.gameObject.SetActive(true);
            }
            else if (promptText.gameObject.activeSelf)
            {
                promptText.gameObject.SetActive(false);
            }
        }

        // interact
        if (!isOpened && inRange && KeybindManager.Instance != null && KeybindManager.Instance.GetKeyDown("Interact"))
        {
            var pm = PointManager.Instance;
            if (pm != null && pm.TrySpend(costToOpen))
            {
                OpenDebris();
            }
            // if not enough points, the “Need X Points” text is already shown
        }
    }

    void OpenDebris()
    {
        isOpened = true;
        if (promptText != null) promptText.gameObject.SetActive(false);
        StartCoroutine(MoveUpAndDestroy());
    }

    private IEnumerator MoveUpAndDestroy()
    {
        float t = 0f;
        while (t < destroyDelay)
        {
            transform.position += Vector3.up * moveSpeed * Time.deltaTime;
            t += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
