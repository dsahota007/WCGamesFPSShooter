using UnityEngine;
using System.Collections;

public class MoreRevivePerk : MonoBehaviour
{
    public PointManager points;

    [Header("Interact")]
    public Transform player;
    public PerkType type = PerkType.Revive;
    public int cost = 3000;
    public float interactDistance = 2.2f;

    [Header("Flask")]
    public GameObject flaskPrefab;           // leave null if you only want the arm anim

    [Header("Flask Offsets")]
    public Vector3 flaskStartLocalPos = new Vector3(-0.09f, -1.1f, 0.42f);
    public Vector3 flaskShowoffLocalPos = new Vector3(-0.05f, -0.5f, 0.25f);
    public Vector3 flaskMouthLocalPos = new Vector3(-0.01f, -0.12f, 0.16f);

    public Vector3 flaskStartLocalEuler = Vector3.zero;
    public Vector3 flaskShowoffLocalEuler = new Vector3(-20f, 0f, 0f);
    public Vector3 flaskSipLocalEuler = new Vector3(-65f, 0f, 0f);

    [Header("Timing")]
    public float moveInTime = 0.5f;
    public float showoffHoldTime = 0.5f;
    public float sipTime = 1f;
    public float moveOutTime = 0.25f;

    [Header("Perk Upgrade")]
    public float timeToRegen = 3f;
    public float regenRatePerSecondIncrease = 10f;
    public GameObject PlayerDrinkVFX;
    [HideInInspector] public bool hasMoreRevivePerk = false;

    Transform cam;

    void Awake()
    {
        cam = (Camera.main != null) ? Camera.main.transform : null;

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (points == null)
            points = FindFirstObjectByType<PointManager>();
    }

    void Update()
    {
        bool inRange = Vector3.Distance(player.position, transform.position) <= interactDistance;
        if (inRange && Input.GetKeyDown(KeyCode.E) && !hasMoreRevivePerk)
        {
            UI ui = FindFirstObjectByType<UI>();
            if (!points.TrySpend(cost))
            {
                if (ui != null) ui.ShowTemporaryPerkMessage("Not enough points");
                return;
            }

            ArmMovementMegaScript arms = FindFirstObjectByType<ArmMovementMegaScript>();
            if (arms == null || arms.IsGrenadeAnimating || arms.IsPerkAnimating) return;

            StartCoroutine(DoPerkDrink(arms));
        }
    }

    IEnumerator DoPerkDrink(ArmMovementMegaScript arms)
    {
        hasMoreRevivePerk = true;
        player?.GetComponent<PlayerAttributes>()?.IncreaseRegenFromMoreRevivePerk(timeToRegen, regenRatePerSecondIncrease);

        FindFirstObjectByType<UI>()?.ShowPerkIcon(PerkType.Revive);

        if (player != null && PlayerDrinkVFX != null)
        {
            GameObject fx = Instantiate(PlayerDrinkVFX, player.transform.position, Quaternion.identity);
            fx.transform.SetParent(player.transform, true);
            Destroy(fx, 4f);
        }

        arms.StartCoroutine(arms.PerkDrinkDropOnly());

        if (cam != null && flaskPrefab != null)
        {
            GameObject flask = Instantiate(flaskPrefab, cam, false);
            Transform tf = flask.transform;

            tf.localPosition = flaskStartLocalPos;
            tf.localRotation = Quaternion.Euler(flaskStartLocalEuler);

            yield return LerpLocal(tf, flaskStartLocalPos, flaskShowoffLocalPos,
                                   Quaternion.Euler(flaskStartLocalEuler), Quaternion.Euler(flaskShowoffLocalEuler), moveInTime);

            if (showoffHoldTime > 0f)
                yield return new WaitForSeconds(showoffHoldTime);

            yield return LerpLocal(tf, flaskShowoffLocalPos, flaskMouthLocalPos,
                                   Quaternion.Euler(flaskShowoffLocalEuler), Quaternion.Euler(flaskSipLocalEuler), 0.25f);

            if (sipTime > 0f)
                yield return new WaitForSeconds(sipTime);

            yield return LerpLocal(tf, flaskMouthLocalPos, flaskStartLocalPos,
                                   Quaternion.Euler(flaskSipLocalEuler), Quaternion.Euler(flaskStartLocalEuler), moveOutTime);

            Destroy(flask);
        }

        while (arms != null && arms.IsPerkAnimating) yield return null;
    }

    IEnumerator LerpLocal(Transform t, Vector3 p0, Vector3 p1, Quaternion r0, Quaternion r1, float dur)
    {
        dur = Mathf.Max(0.01f, dur);
        float k = 0f;
        while (k < 1f)
        {
            k += Time.deltaTime / dur;
            t.localPosition = Vector3.Lerp(p0, p1, k);
            t.localRotation = Quaternion.Slerp(r0, r1, k);
            yield return null;
        }
    }
}
