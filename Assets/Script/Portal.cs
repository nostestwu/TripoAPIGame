using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class Portal : MonoBehaviour
{
    [Header("Target")]
    public string targetSceneName;   // 目标关卡 Scene 名（Build Settings 里要勾上）
    public string targetAnchorId;    // 进入后要用的锚点 Id（可空）

    [Header("Who is player")]
    public string playerTag = "Player";     // 你玩家根一般就这个
    public bool requireCharacterController = true; // 更稳：必须能在父链上找到 CC

    // ① 顶部任意位置（类字段区）——防重复触发的小开关
    bool _launched = false;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        // ★ 关键：给触发器加一个 kinematic Rigidbody，这样 CharacterController 进入也会触发
        var rb = GetComponent<Rigidbody>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;

        _launched = true;                                // 防止多次进门与重复 SFX

        // 进入传送门时播传送音
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.teleportClip, transform.position, 1f);

        Debug.Log($"[Portal] OnTriggerEnter by {other.name} => load {targetSceneName} @ {targetAnchorId}");
        LevelManager.Instance?.EnterLevel(targetSceneName, targetAnchorId);
    }

    // 也许你想看离开事件是否也能打印出来，方便验证触发器设置是否正常
    void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;
        Debug.Log($"[Portal] OnTriggerExit by {other.name}");
    }

    bool IsPlayer(Collider col)
    {
        // 1) 直接是 Player Tag？
        if (col.CompareTag(playerTag)) return true;

        // 2) 刚体在父级，且父级打了 Tag？
        if (col.attachedRigidbody && col.attachedRigidbody.CompareTag(playerTag)) return true;

        // 3) 往父链找 CharacterController（更稳，第三人称多半在根）
        if (requireCharacterController)
        {
            var cc = col.GetComponentInParent<CharacterController>();
            if (cc && cc.gameObject.CompareTag(playerTag)) return true;
        }
        else
        {
            // 或者只要父链里任何物体带 Player tag 也算
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
