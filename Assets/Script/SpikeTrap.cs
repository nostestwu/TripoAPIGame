using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SpikeTrap : MonoBehaviour
{
    [Header("Who & Where")]
    public string playerTag = "Player";                  // ��� Tag
    public string grabbableLayerName = "Grabbable";      // ��ץȡ�������ڲ�
    public Transform respawnPoint;                       // ����㣨���

    [Header("Behavior")]
    public bool useRagdollForPlayer = true;              // ��������Ƿ� ragdoll
    public float ragdollSeconds = 2f;                    // ragdoll ����ʱ��
    public float reTriggerDelay = 0.05f;                 // ͬ֡/��ʱ�����ײ�ļ��̷������������ã�

    // �� ���̽������� Collider �������ID
    private readonly Dictionary<int, float> _lastHitTime = new();

    // �� �ؼ���������Ҹ���������ȴ��ragdoll������ֻ����һ��
    private readonly Dictionary<int, float> _playerCooldownUntil = new();

    // �� ��������λ�ã����ֶ������������ظ�������С����
    bool _launched = false;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true; // �ش���������� trigger
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other) return;

        _launched = true;
        // ��ҵ��ػ򱻴��ͻس�������һ��
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.deadClip, transform.position, 1f);

        // --- ����һ���̵ܶ�ȫ�ֽ���������ͬ֡���� ---
        int key = other.attachedRigidbody ? other.attachedRigidbody.GetInstanceID()
                                          : other.GetInstanceID();
        if (_lastHitTime.TryGetValue(key, out var t) && Time.time - t < reTriggerDelay)
            return;
        _lastHitTime[key] = Time.time;

        // 1) ��ң�
        if (other.CompareTag(playerTag))
        {
            HandlePlayer(other);
            return;
        }

        // 2) Grabbable ���壿ɾ��
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
            Debug.LogWarning("[SpikeTrap] δ���� respawnPoint���޷�������ҡ�");
            return;
        }

        // ץ����Ҹ������� ragdoll ���������� Transform��
        var rag = hit.GetComponentInParent<PlayerRagdollController>();
        Transform root = rag ? rag.transform
                             : (hit.attachedRigidbody ? hit.attachedRigidbody.transform : hit.transform);

        // ---- �����ȴ��ragdoll������ֻ����һ�� ----
        int rootId = root.GetInstanceID();
        if (_playerCooldownUntil.TryGetValue(rootId, out float until) && Time.time < until)
        {
            // ��ȴ�У����Ա���
            return;
        }
        // �����µ���ȴ���ڣ�ragdoll���� + �������ࣩ
        _playerCooldownUntil[rootId] = Time.time + Mathf.Max(ragdollSeconds, 0.05f) + 0.1f;

        // �������
        if (useRagdollForPlayer && rag != null)
        {
            // ���� ragdoll N ��󸴻�
            rag.KnockoutAndRespawn(respawnPoint, ragdollSeconds);
        }
        else
        {
            // ֱ��˲�Ƹ���
            TeleportNow(root, respawnPoint);
        }
        _launched = false;
    }

    void HandleGrabbable(Collider hit)
    {
        var go = hit.attachedRigidbody ? hit.attachedRigidbody.gameObject : hit.gameObject;
        Destroy(go); // ֡ĩ����
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
