using UnityEngine;

public class ModelSpawnAnchor : MonoBehaviour
{
    [Tooltip("�ؿ���Ψһ��ʶ����������GameObject���֣�")]
    public string anchorId;

    [Tooltip("��ѡ������ʱ��ͳһ���ű���")]
    public float spawnScale = 1f;

    void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(anchorId))
            anchorId = gameObject.name;
    }

    public Transform GetTransform() => transform;
}
