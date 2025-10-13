using UnityEngine;
using System.Collections;

public class AdrenalinePerk : MonoBehaviour
{
    public PointManager points;

    [Header("Interact")]
    public Transform player;
    public int cost = 3000;
    public float interactDistance = 3f;

    [Header("Flask")]
    public GameObject flaskPrefab; // optional

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

    [Header("Perk Effect")]
    public float healthThreshold = 15f;   // trigger when current HP < this
    public float rearmBuffer = 1f;        // must heal above threshold+buffer to rearm
    public float speedMultiplier = 1.5f;  // 50% speed boost
    public float duration = 4f;           // seconds

    public GameObject PlayerDrinkVFX;
    [HideInInspector] public bool hasAdrenalinePerk = false;

    private Transform cam;
    private PlayerAttributes attrs;
    private PlayerMovement move;
    private UI ui;

    // runtime state
    private bool buffActive = false;
    private bool armed = true; // ready to trigger when dipping below threshold

    void Awake()
    {
        cam = (Camera.main != null) ? Camera.main.transform : null;
        if (player == null) player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (points == null) points = FindFirstObjectByType<PointManager>();

        attrs = FindFirstObjectByType<PlayerAttributes>();
        move = FindFirstObjectByType<PlayerMovement>();
        ui = FindFirstObjectByType<UI>();
    }

    void Update()
    {
        // purchase interaction
        bool inRange = player != null && Vector3.Distance(player.position, transform.position) <= interactDistance;
        if (inRange && KeybindManager.Instance.GetKeyDown("Interact") && !hasAdrenalinePerk)
        {
            UI uiSys = FindFirstObjectByType<UI>();
            if (!points.TrySpend(cost))
            {
                uiSys?.ShowTemporaryPerkMessage("Not enough points");
                return;
            }
            var arms = FindFirstObjectByType<ArmMovementMegaScript>();
            if (arms == null || arms.IsGrenadeAnimating || arms.IsPerkAnimating) return;
            StartCoroutine(DoPerkDrink(arms));
        }

        // passive effect after purchase
        if (!hasAdrenalinePerk || attrs == null || move == null) return;

        float currentHP = attrs.GetCurrentHealth01() * attrs.maxStartingHealth;

        // re-arm when safely above threshold
        if (!buffActive && !armed && currentHP > (healthThreshold + rearmBuffer))
            armed = true;

        // trigger when dipping below threshold
        if (!buffActive && armed && currentHP > 0f && currentHP < healthThreshold)
            StartCoroutine(DoAdrenalineBoost());
    }

    IEnumerator DoPerkDrink(ArmMovementMegaScript arms)
    {
        hasAdrenalinePerk = true;
        UI.Main?.ShowCenterPopup("Adrenaline Potion Acquired", Color.white);


        // UI icon (add PerkType.Adrenaline in your enum + UI if you want a unique icon)
        ui?.ShowPerkIcon(PerkType.Adrenaline); // or PerkType.Adrenaline if you add one

        if (player != null && PlayerDrinkVFX != null)
        {
            GameObject fx = Instantiate(PlayerDrinkVFX, player.transform.position, Quaternion.identity);
            fx.transform.SetParent(player.transform, true);
            Destroy(fx, 4f);
        }

        arms.StartCoroutine(arms.PerkDrinkDropOnly());

        // flask (optional)
        if (cam != null && flaskPrefab != null)
        {
            GameObject flask = Instantiate(flaskPrefab, cam, false);
            Transform tf = flask.transform;

            tf.localPosition = flaskStartLocalPos;
            tf.localRotation = Quaternion.Euler(flaskStartLocalEuler);

            yield return LerpLocal(tf, flaskStartLocalPos, flaskShowoffLocalPos,
                Quaternion.Euler(flaskStartLocalEuler), Quaternion.Euler(flaskShowoffLocalEuler), moveInTime);

            if (showoffHoldTime > 0f) yield return new WaitForSeconds(showoffHoldTime);

            yield return LerpLocal(tf, flaskShowoffLocalPos, flaskMouthLocalPos,
                Quaternion.Euler(flaskShowoffLocalEuler), Quaternion.Euler(flaskSipLocalEuler), 0.25f);

            if (sipTime > 0f) yield return new WaitForSeconds(sipTime);

            yield return LerpLocal(tf, flaskMouthLocalPos, flaskStartLocalPos,
                Quaternion.Euler(flaskSipLocalEuler), Quaternion.Euler(flaskStartLocalEuler), moveOutTime);

            Destroy(flask);
        }

        // wait out arm anim before leaving
        while (arms != null && arms.IsPerkAnimating) yield return null;
    }

    IEnumerator DoAdrenalineBoost()
    {
        buffActive = true;
        armed = false;

        // apply speed boost (multiplicative with your other perks)
        move.ExternalSpeedMult *= speedMultiplier;

        // HUD badge
        ui?.ShowTimedPowerup("adrenaline", "ADRENALINE", duration);

        yield return new WaitForSeconds(duration);

        // remove boost
        move.ExternalSpeedMult /= Mathf.Max(0.0001f, speedMultiplier);
        buffActive = false;
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

    void OnDisable()
    {
        // safety: if disabled mid-buff, restore speed
        if (buffActive && move != null)
        {
            move.ExternalSpeedMult /= Mathf.Max(0.0001f, speedMultiplier);
            buffActive = false;
        }
    }
}
