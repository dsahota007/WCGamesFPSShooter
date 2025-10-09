using UnityEngine;

public class DropSpawner : MonoBehaviour
{
    [Range(0f, 1f)] public float dropChance = 0.005f;
    public static float GlobalDropChanceMult = 1f;
    public GameObject[] pickupPrefabs; // each has DropPickup + trigger collider

    public void TrySpawnDrop(Vector3 where)
    {
        if (pickupPrefabs == null || pickupPrefabs.Length == 0) return;

        // no clamp, just pure chaos
        float effectiveChance = dropChance * Mathf.Max(0f, GlobalDropChanceMult);

        if (Random.value > effectiveChance) return; // if effectiveChance > 1, this always passes (guaranteed drop)

        var prefab = pickupPrefabs[Random.Range(0, pickupPrefabs.Length)];
        Instantiate(prefab, where + Vector3.up * 0.5f, Quaternion.identity);
    }

}
