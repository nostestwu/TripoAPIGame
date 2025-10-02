using UnityEngine;

[DisallowMultipleComponent]
public class HoopLamp : MonoBehaviour
{
    [Header("Lamp Renderer")]
    [SerializeField] Renderer lampRenderer;

    [Header("Emission Colors")]
    [ColorUsage(true, true)]
    public Color redEmission = new Color(1, 0, 0, 1);
    [ColorUsage(true, true)]
    public Color greenEmission = new Color(0, 1, 0, 1);

    Material _matInstance;

    void Awake()
    {
        if (!lampRenderer)
            lampRenderer = GetComponentInChildren<Renderer>(true);

        if (lampRenderer != null)
        {
            // 实例化材质，避免改 sharedMaterial
            _matInstance = lampRenderer.material;
            // 如果材质支持 Emission，需要打开关键字
            _matInstance.EnableKeyword("_EMISSION");
        }
    }

    public void SetRed()
    {
        if (_matInstance == null) return;
        ApplyEmission(redEmission);
    }

    public void SetGreen()
    {
        if (_matInstance == null) return;
        ApplyEmission(greenEmission);
    }

    void ApplyEmission(Color emissive)
    {
        // 设基础颜色（Albedo / 主色）
        if (_matInstance.HasProperty("_Color"))
            _matInstance.SetColor("_Color", emissive);
        else if (_matInstance.HasProperty("_BaseColor"))
            _matInstance.SetColor("_BaseColor", emissive);

        // 设发光颜色
        if (_matInstance.HasProperty("_EmissionColor"))
            _matInstance.SetColor("_EmissionColor", emissive);
        else if (_matInstance.HasProperty("_EmissiveColor"))
            _matInstance.SetColor("_EmissiveColor", emissive);
    }
}
