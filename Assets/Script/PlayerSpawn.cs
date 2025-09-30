using UnityEngine;

public class PlayerSpawn : MonoBehaviour
{
    [Tooltip("���ؿ�ʱ�������볯��")]
    public bool drawGizmo = true;
    // �� ��������λ�ã����ֶ������������ظ�������С����
    bool _launched = false;
    private void Start()
    {
        _launched = true;                                // ��ֹ��ν������ظ� SFX
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
