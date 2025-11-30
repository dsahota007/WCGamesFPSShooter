using UnityEngine;

public class DropPickup : MonoBehaviour
{
    public DropType type = DropType.MaxAmmo;
    public float rotateSpeed = 90f;
    public float bobAmplitude = 0.15f;
    public float bobSpeed = 2f;
    public float lifeTime = 20f;

    Vector3 basePos;

    public GameObject pickupVFX;
    public float vfxLifeTime = 5f;


    void Start()
    {
        basePos = transform.position;
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // spin
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        // bob
        float y = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        transform.position = basePos + new Vector3(0f, y, 0f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        FindFirstObjectByType<DropManager>()?.Apply(type);

        if (pickupVFX != null)
        {
            GameObject vfx = Instantiate(pickupVFX, transform.position, Quaternion.identity);
            Destroy(vfx, vfxLifeTime);
        }

        Destroy(gameObject);
    }
}
