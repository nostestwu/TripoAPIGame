using UnityEngine;

public class ModelSpawnAnchor : MonoBehaviour
{
    [Tooltip("关卡内唯一标识（不填则用GameObject名字）")]
    public string anchorId;

    [Tooltip("可选：生成时的统一缩放倍数")]
    public float spawnScale = 1f;

    void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(anchorId))
            anchorId = gameObject.name;
    }

    public Transform GetTransform() => transform;
}
