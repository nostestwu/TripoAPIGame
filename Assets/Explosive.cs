using UnityEngine;

[DisallowMultipleComponent]
public class Explosive : MonoBehaviour
{
    [Header("Explosion Physics")]
    [Tooltip("��ը�뾶")]
    public float radius = 6f;
    [Tooltip("��ը���������� AddExplosionForce �� force��")]
    public float force = 20f;
    [Tooltip("����ƫ�ƣ�����̧���У�")]
    public float upwardsModifier = 0.5f;
    [Tooltip("����ըӰ��Ĳ�")]
    public LayerMask affectLayers = ~0;

    [Header("FX (Prefab GameObject)")]
    [Tooltip("��ը��ЧԤ���壨������һ������ϵͳ�ĸ����壩")]
    public GameObject explosionVfxPrefab;
    [Tooltip("��ը��Ч��һ���� 3D ���ţ�")]
    public AudioClip explosionSfx;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("Arming / Conditions")]
    [Tooltip("Ͷ������ײ�� Trigger Ҳ�㴥��")]
    public bool explodeOnTriggerToo = true;
    [Tooltip("С�ڸ�����ٶȲ��������������󱬣�")]
    public float minImpactSpeed = 0.2f;
    [Tooltip("Ͷ���� N �뻹ûײҲ����0=���ã����Զ��ף�")]
    public float autoExplodeAfter = 0f;

    // �ڲ�״̬
    bool _armedByThrow = false;   // ���� E Ͷ��ʱ�� true
    bool _done = false;           // ���ظ�
    float _armedTime = 0f;        // ���ڳ�ʱ����

    /// <summary>
    /// ���ڡ��� E Ͷ������·������á�F ����/��Ȼ���䲻Ҫ����
    /// </summary>
    public void OnThrown()
    {
        if (_done) return;
        _armedByThrow = true;
        _armedTime = Time.time;
        // �����̱������״�ʵ�ʽӴ����򴥷�������/��ʱ���ף�
    }

    void Update()
    {
        // ��ʱ���ף����ڵ��Գ��������� Inspector �� autoExplodeAfter ��Ϊ 3~6s��
        if (_armedByThrow && !_done && autoExplodeAfter > 0f &&
            Time.time - _armedTime > autoExplodeAfter)
        {
            ExplodeAt(transform.position);
        }
    }

    // ���� ʵ����ײ����������һ���� Trigger ������һ���� Rigidbody������
    void OnCollisionEnter(Collision c)
    {
        if (_done || !_armedByThrow) return;

        // �ٶ��ż�������ٶȹ�С����ԣ�
        if (c.relativeVelocity.magnitude < minImpactSpeed) return;

        // ȡ��һ���Ӵ�����Ϊ���ģ�����ò�����������λ��
        Vector3 impact = (c.contactCount > 0) ? c.GetContact(0).point : transform.position; // �ٷ� API: GetContact / contactCount
        ExplodeAt(impact);                                                                 // :contentReference[oaicite:1]{index=1}
    }

    // ���� ��������������ѡ������
    void OnTriggerEnter(Collider other)
    {
        if (!_armedByThrow || _done || !explodeOnTriggerToo) return;

        // �Ӵ�����ȡ����㣻�ò�����������λ��
        Vector3 impact = other.ClosestPoint(transform.position);
        if ((impact - transform.position).sqrMagnitude < 1e-6f) impact = transform.position;
        ExplodeAt(impact);
        // OnTriggerEnter ��ʱ���� & isTrigger ��Ϊ���ٷ�˵����:contentReference[oaicite:2]{index=2}
    }

    // ���� ���ı�ը�߼����� center ��ʵ���� VFX��������Ч��ʩ�ӱ�ը�������ٱ��� ���� 
    void ExplodeAt(Vector3 center)
    {
        if (_done) return;
        _done = true;

        // 1) VFX���뱾����ֱ���������������ɣ�
        if (explosionVfxPrefab)
        {
            Instantiate(explosionVfxPrefab, center, Quaternion.identity); // ����ʱʵ���� Prefab
        }

        // 2) SFX��һ���� 3D ��Ч��
        if (explosionSfx)
        {
            AudioSource.PlayClipAtPoint(explosionSfx, center, sfxVolume); // �Զ�������������Դ
            // �ٷ�˵�����ú����ᴴ��һ����ʱ AudioSource ���ڲ������Զ�����:contentReference[oaicite:3]{index=3}
        }

        // 3) ���������ռ��뾶�ڵ� Collider �� ���� attachedRigidbody ʩ�ӱ�ը��
        var cols = Physics.OverlapSphere(center, radius, affectLayers, QueryTriggerInteraction.Ignore); // ���ش���/�ڲ������� Collider
        for (int i = 0; i < cols.Length; i++)
        {
            var rb = cols[i].attachedRigidbody;
            if (!rb) continue;

            // ����˥��/���α��ĵı�׼��ը��ģ�ͣ�Unity ���ã�
            rb.AddExplosionForce(force, center, radius, upwardsModifier, ForceMode.Impulse);
            // �ĵ���AddExplosionForce ����Ϊ�����˵����:contentReference[oaicite:4]{index=4}
        }

        // 4) �������ٱ��壨VFX/SFX ���뱾����
        Destroy(gameObject);
    }

    // ������ѡ��ʱ���ӻ���ը�뾶
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.35f);
        Gizmos.DrawSphere(transform.position, radius);
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
