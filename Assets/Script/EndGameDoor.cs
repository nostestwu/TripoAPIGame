using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EndGameDoor : MonoBehaviour
{
    public string playerTag = "Player";     // ֻ����Ҵ���
    public EndScreenUI endScreen;           // ָ��פ��Bootstrap����� EndScreenUI
    bool _launched = false;
    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        // �ô������� CharacterController ȷ���ܴ���������һ��������
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

        // �Ҳ������þ��ֳ���һ�Σ��ݴ�
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
