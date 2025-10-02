using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class HoopTrigger : MonoBehaviour
{
    [Header("Scoring")]
    [Tooltip("��Щ���ܴ����÷֣�Ĭ�ϰ��� Default + Grabbable��")]
    public LayerMask scoreLayers;
    [Tooltip("ͬһ��ֻ��һ�η�")]
    public bool oneShot = true;

    [Header("Lamp")]
    public HoopLamp lamp;               // �ϵ�����򶥲��ĵ�
    public UnityEvent onScored;         // ֪ͨ�ⲿ��HiddenDoorRevealer �ᶩ�ģ�

    bool _scored = false;

    [Header("Audio")]
    public AudioClip scoreSfx;
    [Range(0f, 1f)] public float scoreVolume = 1f;
    public bool useLocalAudioSource = true;   // ����������AudioSource����������

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true; // �����Ǵ��������
    }

    void Start()
    {
        // ȱʡ�㣺Default + Grabbable������У�
        if (scoreLayers.value == 0)
        {
            int def = LayerMask.NameToLayer("Default");
            int grab = LayerMask.NameToLayer("Grabbable");
            scoreLayers = 0;
            if (def >= 0) scoreLayers |= (1 << def);
            if (grab >= 0) scoreLayers |= (1 << grab);
        }

        if (lamp) lamp.SetRed();
    }

    void OnTriggerEnter(Collider other)
    {
        if (oneShot && _scored) return;
        if (!IsInLayerMask(other.gameObject.layer, scoreLayers)) return;

        // Ҫ���� OnTriggerEnter������һ���� Rigidbody��ͨ����Ͷ����/��
        _scored = true;
        PlayScoreSfx();
        if (lamp) lamp.SetGreen();
        onScored?.Invoke();
    }

    static bool IsInLayerMask(int layer, LayerMask mask)
        => (mask.value & (1 << layer)) != 0; // LayerMask �����÷�:contentReference[oaicite:4]{index=4}


    void PlayScoreSfx()
    {
        if (!scoreSfx) return;

        if (useLocalAudioSource && TryGetComponent<AudioSource>(out var src))
        {
            src.PlayOneShot(scoreSfx, scoreVolume);
        }
        else
        {
            // û�б�����Դ����һ���Ե�3D����
            AudioSource.PlayClipAtPoint(scoreSfx, transform.position, scoreVolume);
        }
    }
}
