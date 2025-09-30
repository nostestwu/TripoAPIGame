using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SpikeTrap : MonoBehaviour
{
    [Header("Who & Where")]
    public string playerTag = "Player";                  // 玩家 Tag
    public string grabbableLayerName = "Grabbable";      // 可抓取物体所在层
    public Transform respawnPoint;                       // 复活点（必填）

    [Header("Behavior")]
    public bool useRagdollForPlayer = true;              // 玩家碰到是否 ragdoll
    public float ragdollSeconds = 2f;                    // ragdoll 持续时长
    public float reTriggerDelay = 0.05f;                 // 同帧/短时间多碰撞的极短防抖（对杂项用）

    // ① 极短节流：按 Collider 或其刚体ID
    private readonly Dictionary<int, float> _lastHitTime = new();

    // ② 关键：按“玩家根对象”做冷却，ragdoll窗口内只处理一次
    private readonly Dictionary<int, float> _playerCooldownUntil = new();

    // ① 顶部任意位置（类字段区）——防重复触发的小开关
    bool _launched = false;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true; // 地刺区域必须是 trigger
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other) return;

        _launched = true;
        // 玩家倒地或被传送回出生点那一刻
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.deadClip, transform.position, 1f);

        // --- 先做一个很短的全局节流，避免同帧抖动 ---
        int key = other.attachedRigidbody ? other.attachedRigidbody.GetInstanceID()
                                          : other.GetInstanceID();
        if (_lastHitTime.TryGetValue(key, out var t) && Time.time - t < reTriggerDelay)
            return;
        _lastHitTime[key] = Time.time;

        // 1) 玩家？
        if (other.CompareTag(playerTag))
        {
            HandlePlayer(other);
            return;
        }

        // 2) Grabbable 物体？删除
        int gLayer = LayerMask.NameToLayer(grabbableLayerName);
        if (gLayer != -1 && other.gameObject.layer == gLayer)
        {
            HandleGrabbable(other);
        }
    }

    void HandlePlayer(Collider hit)
    {
        if (!respawnPoint)
        {
            Debug.LogWarning("[SpikeTrap] 未设置 respawnPoint，无法复活玩家。");
            return;
        }

        // 抓到玩家根（优先 ragdoll 控制器所在 Transform）
        var rag = hit.GetComponentInParent<PlayerRagdollController>();
        Transform root = rag ? rag.transform
                             : (hit.attachedRigidbody ? hit.attachedRigidbody.transform : hit.transform);

        // ---- 玩家冷却：ragdoll窗口内只处理一次 ----
        int rootId = root.GetInstanceID();
        if (_playerCooldownUntil.TryGetValue(rootId, out float until) && Time.time < until)
        {
            // 冷却中，忽略本次
            return;
        }
        // 设置新的冷却窗口（ragdoll持续 + 少量冗余）
        _playerCooldownUntil[rootId] = Time.time + Mathf.Max(ragdollSeconds, 0.05f) + 0.1f;

        // 处理玩家
        if (useRagdollForPlayer && rag != null)
        {
            // 进入 ragdoll N 秒后复活
            rag.KnockoutAndRespawn(respawnPoint, ragdollSeconds);
        }
        else
        {
            // 直接瞬移复活
            TeleportNow(root, respawnPoint);
        }
        _launched = false;
    }

    void HandleGrabbable(Collider hit)
    {
        var go = hit.attachedRigidbody ? hit.attachedRigidbody.gameObject : hit.gameObject;
        Destroy(go); // 帧末销毁
    }

    static void TeleportNow(Transform target, Transform to)
    {
        if (!target || !to) return;
        var rb = target.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        target.SetPositionAndRotation(to.position, to.rotation);
    }
}
