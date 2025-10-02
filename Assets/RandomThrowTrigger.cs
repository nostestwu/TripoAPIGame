using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RandomThrowTrigger : MonoBehaviour
{
    [Header("Throw Force Range")]
    public float minForce = 50f;
    public float maxForce = 500f;

    [Header("Direction Options")]
    [Tooltip("���Ϊ true�����䷽��������ֱ����������/���£�")]
    public bool allowVertical = false;

    [Header("Layers to Affect")]
    public LayerMask targetLayers;  // ����Ϊ���Ա����׳����Ĳ�

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // �����Ƿ��� targetLayers ��
        if (!IsInLayerMask(other.gameObject.layer, targetLayers)) return;

        // ��ȡ Rigidbody����һ���� this Collider �ϣ������ڸ�����
        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) return;

        // ��һ���������
        Vector3 dir = GetRandomDirection();

        float force = Random.Range(minForce, maxForce);

        // ʩ�ӳ���
        rb.AddForce(dir * force, ForceMode.Impulse);
    }

    Vector3 GetRandomDirection()
    {
        Vector3 dir;
        if (allowVertical)
        {
            // ��������淽�򣨰������£�
            dir = Random.onUnitSphere;  // ��׼��λ����
        }
        else
        {
            // ������ˮƽƽ�棨X,Z ���򣩣�Y = 0
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
