using UnityEngine;

public class PlayerSpawn : MonoBehaviour
{
    [Tooltip("进关卡时玩家落点与朝向")]
    public bool drawGizmo = true;
    // ① 顶部任意位置（类字段区）——防重复触发的小开关
    bool _launched = false;
    private void Start()
    {
        _launched = true;                                // 防止多次进门与重复 SFX
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.spawnClip, transform.position, 1f);
    }
    void OnDrawGizmos()
    {
        if (!drawGizmo) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + Vector3.up * 1f, new Vector3(0.6f, 2f, 0.6f));
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, transform.forward * 1.2f);
    }
}
