using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class Portal : MonoBehaviour
{
    [Header("Target")]
    public string targetSceneName;   // Ŀ��ؿ� Scene ����Build Settings ��Ҫ���ϣ�
    public string targetAnchorId;    // �����Ҫ�õ�ê�� Id���ɿգ�

    [Header("Who is player")]
    public string playerTag = "Player";     // ����Ҹ�һ������
    public bool requireCharacterController = true; // ���ȣ��������ڸ������ҵ� CC

    // �� ��������λ�ã����ֶ������������ظ�������С����
    bool _launched = false;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        // �� �ؼ�������������һ�� kinematic Rigidbody������ CharacterController ����Ҳ�ᴥ��
        var rb = GetComponent<Rigidbody>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;

        _launched = true;                                // ��ֹ��ν������ظ� SFX

        // ���봫����ʱ��������
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.teleportClip, transform.position, 1f);

        Debug.Log($"[Portal] OnTriggerEnter by {other.name} => load {targetSceneName} @ {targetAnchorId}");
        LevelManager.Instance?.EnterLevel(targetSceneName, targetAnchorId);
    }

    // Ҳ�����뿴�뿪�¼��Ƿ�Ҳ�ܴ�ӡ������������֤�����������Ƿ�����
    void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;
        Debug.Log($"[Portal] OnTriggerExit by {other.name}");
    }

    bool IsPlayer(Collider col)
    {
        // 1) ֱ���� Player Tag��
        if (col.CompareTag(playerTag)) return true;

        // 2) �����ڸ������Ҹ������� Tag��
        if (col.attachedRigidbody && col.attachedRigidbody.CompareTag(playerTag)) return true;

        // 3) �������� CharacterController�����ȣ������˳ƶ���ڸ���
        if (requireCharacterController)
        {
            var cc = col.GetComponentInParent<CharacterController>();
            if (cc && cc.gameObject.CompareTag(playerTag)) return true;
        }
        else
        {
            // ����ֻҪ�������κ������ Player tag Ҳ��
            var t = col.transform;
            while (t)
            {
                if (t.CompareTag(playerTag)) return true;
                t = t.parent;
            }
        }
        return false;
    }
}
