using UnityEngine;

public class LODBiasController : MonoBehaviour
{
    [Header("LOD Bias (try 2, 3, 4)")]
    [Range(0.1f, 5f)]
    public float lodBias = 2f;

    private float lastBias;

    void Start()
    {
        ApplyBias();
    }

    void Update()
    {
        if (Mathf.Abs(lastBias - lodBias) > 0.01f)
        {
            ApplyBias();
        }
    }

    void ApplyBias()
    {
        QualitySettings.lodBias = lodBias;
        lastBias = lodBias;
        Debug.Log($"LOD Bias set to {lodBias}");
    }
}
