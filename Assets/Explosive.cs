using UnityEngine;

[DisallowMultipleComponent]
public class Explosive : MonoBehaviour
{
    [Header("Explosion Physics")]
    [Tooltip("爆炸半径")]
    public float radius = 6f;
    [Tooltip("爆炸冲量（用于 AddExplosionForce 的 force）")]
    public float force = 20f;
    [Tooltip("向上偏移（更有抬升感）")]
    public float upwardsModifier = 0.5f;
    [Tooltip("被爆炸影响的层")]
    public LayerMask affectLayers = ~0;

    [Header("FX (Prefab GameObject)")]
    [Tooltip("爆炸特效预制体（可以是一组粒子系统的父物体）")]
    public GameObject explosionVfxPrefab;
    [Tooltip("爆炸音效（一次性 3D 播放）")]
    public AudioClip explosionSfx;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("Arming / Conditions")]
    [Tooltip("投掷后若撞到 Trigger 也算触发")]
    public bool explodeOnTriggerToo = true;
    [Tooltip("小于该相对速度不引爆（防擦碰误爆）")]
    public float minImpactSpeed = 0.2f;
    [Tooltip("投出后 N 秒还没撞也爆（0=禁用；调试兜底）")]
    public float autoExplodeAfter = 0f;

    // 内部状态
    bool _armedByThrow = false;   // 仅按 E 投掷时置 true
    bool _done = false;           // 防重复
    float _armedTime = 0f;        // 用于超时兜底

    /// <summary>
    /// 仅在“按 E 投掷”的路径里调用。F 放下/自然下落不要调。
    /// </summary>
    public void OnThrown()
    {
        if (_done) return;
        _armedByThrow = true;
        _armedTime = Time.time;
        // 不立刻爆；等首次实际接触（或触发器进入/超时兜底）
    }

    void Update()
    {
        // 超时兜底：便于调试场景（可在 Inspector 把 autoExplodeAfter 设为 3~6s）
        if (_armedByThrow && !_done && autoExplodeAfter > 0f &&
            Time.time - _armedTime > autoExplodeAfter)
        {
            ExplodeAt(transform.position);
        }
    }

    // —— 实体碰撞触发（至少一方非 Trigger 且至少一方带 Rigidbody）——
    void OnCollisionEnter(Collision c)
    {
        if (_done || !_armedByThrow) return;

        // 速度门槛（相对速度过小则忽略）
        if (c.relativeVelocity.magnitude < minImpactSpeed) return;

        // 取第一个接触点作为爆心；如果拿不到就用自身位置
        Vector3 impact = (c.contactCount > 0) ? c.GetContact(0).point : transform.position; // 官方 API: GetContact / contactCount
        ExplodeAt(impact);                                                                 // :contentReference[oaicite:1]{index=1}
    }

    // —— 触发器触发（可选）——
    void OnTriggerEnter(Collider other)
    {
        if (!_armedByThrow || _done || !explodeOnTriggerToo) return;

        // 从触发器取最近点；拿不到就用自身位置
        Vector3 impact = other.ClosestPoint(transform.position);
        if ((impact - transform.position).sqrMagnitude < 1e-6f) impact = transform.position;
        ExplodeAt(impact);
        // OnTriggerEnter 何时触发 & isTrigger 行为见官方说明。:contentReference[oaicite:2]{index=2}
    }

    // —— 核心爆炸逻辑：在 center 处实例化 VFX、播放音效、施加爆炸力、销毁本体 —— 
    void ExplodeAt(Vector3 center)
    {
        if (_done) return;
        _done = true;

        // 1) VFX（与本体解耦，直接在世界坐标生成）
        if (explosionVfxPrefab)
        {
            Instantiate(explosionVfxPrefab, center, Quaternion.identity); // 运行时实例化 Prefab
        }

        // 2) SFX（一次性 3D 音效）
        if (explosionSfx)
        {
            AudioSource.PlayClipAtPoint(explosionSfx, center, sfxVolume); // 自动创建与清理音源
            // 官方说明：该函数会创建一个临时 AudioSource 并在播放完自动清理。:contentReference[oaicite:3]{index=3}
        }

        // 3) 物理冲击：收集半径内的 Collider → 对其 attachedRigidbody 施加爆炸力
        var cols = Physics.OverlapSphere(center, radius, affectLayers, QueryTriggerInteraction.Ignore); // 返回触及/内部的所有 Collider
        for (int i = 0; i < cols.Length; i++)
        {
            var rb = cols[i].attachedRigidbody;
            if (!rb) continue;

            // 距离衰减/球形爆心的标准爆炸力模型（Unity 内置）
            rb.AddExplosionForce(force, center, radius, upwardsModifier, ForceMode.Impulse);
            // 文档：AddExplosionForce 的行为与参数说明。:contentReference[oaicite:4]{index=4}
        }

        // 4) 立即销毁本体（VFX/SFX 已与本体解耦）
        Destroy(gameObject);
    }

    // 场景中选中时可视化爆炸半径
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.35f);
        Gizmos.DrawSphere(transform.position, radius);
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
