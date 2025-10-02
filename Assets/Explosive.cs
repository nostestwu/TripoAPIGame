using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class Explosive : MonoBehaviour
{
    [Header("Explosion Physics")]
    public float radius = 6f;
    public float force = 20f;
    public float upwardsModifier = 0.5f;
    [Tooltip("ֻӰ�����������Ĳ㣻ը��/�����˺���������")]
    public LayerMask affectLayers = ~0;

    [Header("FX")]
    public GameObject explosionVfxPrefab;
    public AudioClip explosionSfx;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("Arming / Conditions")]
    public bool explodeOnTriggerToo = true;
    public float minImpactSpeed = 0.2f;
    public float autoExplodeAfter = 0f;
    [Tooltip("�����ã�һ���ɾ����ţ�����Ͷ��Ҳ�ܴ�����")]
    public bool armOnStart = false;

    [Header("Damage / Interactions")]
    public float maxMonsterDamage = 100f;
    public float minMonsterDamage = 10f;
    [Tooltip("��Щ�㱻��������ը�����š����������߼�������")]
    public LayerMask destructibleMask = ~0;
    [Tooltip("��Щ�㱻������������������߼�������")]
    public LayerMask monsterMask = ~0;

    [Header("Debug")]
    public bool verbose = false;
    [Tooltip("�Ҳ�����ȦTriggerʱ����ģ�Ͱ뾶�����ϵ������Sphere Trigger")]
    public float triggerRadiusScale = 0.6f;

    // ������
    Rigidbody _rb;
    SphereCollider _trigger; // ը����Ȧ������
    bool _armedByThrow = false;
    bool _done = false;
    float _armedTime = 0f;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.useGravity = true;

        // ȷ������һ��Sphere Trigger�����Ѵ�������
        _trigger = GetComponent<SphereCollider>();
        if (_trigger == null || !_trigger.isTrigger)
        {
            // ���ƻ������е� MeshCollider/BoxCollider����Щ��ʵ����ײ����������һ������Ȧ
            _trigger = gameObject.AddComponent<SphereCollider>();
            _trigger.isTrigger = true;
            _trigger.radius = Mathf.Max(0.2f, ComputeApproxRadius() * triggerRadiusScale);
            if (verbose) Debug.Log($"[Explosive] Added Sphere Trigger (r={_trigger.radius:0.00})", this);
        }
        else if (verbose)
        {
            Debug.Log($"[Explosive] Reusing existing trigger (r={_trigger.radius:0.00})", this);
        }
    }

    void Start()
    {
        if (armOnStart) Arm("armOnStart");
    }

    void Arm(string reason)
    {
        if (_done) return;
        _armedByThrow = true;
        _armedTime = Time.time;
        if (verbose) Debug.Log($"[Explosive] ARMED by {reason}", this);
    }

    /// Ͷ���߼���ǵõ���
    public void OnThrown()
    {
        Arm("OnThrown");
    }

    void Update()
    {
        if (_armedByThrow && !_done && autoExplodeAfter > 0f &&
            Time.time - _armedTime > autoExplodeAfter)
        {
            if (verbose) Debug.Log($"[Explosive] Auto explode after {autoExplodeAfter}s", this);
            ExplodeAt(transform.position);
        }
    }

    void OnCollisionEnter(Collision c)
    {
        if (_done || !_armedByThrow) return;
        if (c.relativeVelocity.magnitude < minImpactSpeed) return;

        Vector3 impact = (c.contactCount > 0) ? c.GetContact(0).point : transform.position;
        if (verbose) Debug.Log($"[Explosive] OnCollisionEnter hit {c.collider.name} @ {impact}", c.collider);
        ExplodeAt(impact);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!_armedByThrow || _done || !explodeOnTriggerToo) return;
        Vector3 impact = other.ClosestPoint(transform.position);
        if (verbose) Debug.Log($"[Explosive] OnTriggerEnter with {other.name} (layer={LayerMask.LayerToName(other.gameObject.layer)})", other);
        ExplodeAt(impact);
    }

    void ExplodeAt(Vector3 center)
    {
        if (_done) return;
        _done = true;

        // 1) VFX / SFX
        if (explosionVfxPrefab)
        {
            var vfx = Instantiate(explosionVfxPrefab, center, Quaternion.identity);
            float life = 3f;
            var ps = vfx.GetComponentInChildren<ParticleSystem>();
            if (ps)
            {
                var main = ps.main;
                life = Mathf.Max(life, main.duration + main.startLifetime.constantMax + 0.25f);
            }
            Destroy(vfx, life);
        }
        if (explosionSfx) AudioSource.PlayClipAtPoint(explosionSfx, center, sfxVolume);

        // 2) �������������Դ�������
        var physCols = Physics.OverlapSphere(center, radius, affectLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < physCols.Length; i++)
        {
            var rb = physCols[i].attachedRigidbody;
            if (!rb) continue;
            rb.AddExplosionForce(force, center, radius, upwardsModifier, ForceMode.Impulse);
        }

        // 3) �߼��������������������������������
        var logicCols = Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Collide);
        var visitedDoors = new HashSet<DestructibleDoor>();
        var visitedMonsters = new HashSet<MonsterHealth>();

        for (int i = 0; i < logicCols.Length; i++)
        {
            var tr = logicCols[i].transform;

            // ���� �� ���� 
            var door = tr.GetComponentInParent<DestructibleDoor>(true);
            if (door && visitedDoors.Add(door) && IsMaskAllowed(door.gameObject.layer, destructibleMask))
            {
                if (verbose) Debug.Log($"[Explosive] Explode door: {door.name}", door);
                door.ExplodeOpen(center, force, radius);
            }

            // ���� ���� ���� 
            var mh = tr.GetComponentInParent<MonsterHealth>(true);
            if (mh && visitedMonsters.Add(mh) && IsMaskAllowed(mh.gameObject.layer, monsterMask))
            {
                const int FIXED_BOMB_DAMAGE = 100;
                mh.ApplyDamage(FIXED_BOMB_DAMAGE);
            }
        }

        Destroy(gameObject);
    }

    static bool IsMaskAllowed(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;

    float ComputeApproxRadius()
    {
        var rends = GetComponentsInChildren<Renderer>(true);
        if (rends == null || rends.Length == 0) return 0.5f;
        Bounds b = new Bounds(rends[0].bounds.center, Vector3.zero);
        for (int i = 0; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        return b.extents.magnitude;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.35f);
        Gizmos.DrawSphere(transform.position, radius);
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
