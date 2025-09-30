using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EndGameDoor : MonoBehaviour
{
    public string playerTag = "Player";     // 只有玩家触发
    public EndScreenUI endScreen;           // 指向常驻（Bootstrap）里的 EndScreenUI
    bool _launched = false;
    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        // 让触发器和 CharacterController 确保能触发：至少一方带刚体
        var rb = GetComponent<Rigidbody>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void OnTriggerEnter(Collider other)
    {

        _launched = true;
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.victoryClip, transform.position, 1f);

        if (!other || !other.CompareTag(playerTag)) return;

        // 找不到引用就现场找一次（容错）
        if (!endScreen) endScreen = FindObjectOfType<EndScreenUI>(true);

        if (endScreen)
        {
            endScreen.Show();
        }
        else
        {
            Debug.LogWarning("[EndGameDoor] EndScreenUI not found. Please place it in Bootstrap scene.");
        }
    }
}
