using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RandomThrowTrigger : MonoBehaviour
{
    [Header("Throw Force Range")]
    public float minForce = 50f;
    public float maxForce = 500f;

    [Header("Direction Options")]
    [Tooltip("如果为 true，弹射方向会包含垂直分量（向上/向下）")]
    public bool allowVertical = false;

    [Header("Layers to Affect")]
    public LayerMask targetLayers;  // 被认为可以被“抛出”的层

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 检查层是否在 targetLayers 中
        if (!IsInLayerMask(other.gameObject.layer, targetLayers)) return;

        // 获取 Rigidbody（不一定在 this Collider 上，可能在父级）
        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) return;

        // 给一个随机方向
        Vector3 dir = GetRandomDirection();

        float force = Random.Range(minForce, maxForce);

        // 施加冲量
        rb.AddForce(dir * force, ForceMode.Impulse);
    }

    Vector3 GetRandomDirection()
    {
        Vector3 dir;
        if (allowVertical)
        {
            // 随机在球面方向（包括上下）
            dir = Random.onUnitSphere;  // 标准单位球方向
        }
        else
        {
            // 限制在水平平面（X,Z 方向），Y = 0
            Vector3 horiz = Random.insideUnitCircle.normalized;
            dir = new Vector3(horiz.x, 0f, horiz.y);
        }
        return dir.normalized;
    }

    static bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}
