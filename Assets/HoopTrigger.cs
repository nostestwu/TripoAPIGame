using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class HoopTrigger : MonoBehaviour
{
    [Header("Scoring")]
    [Tooltip("哪些层能触发得分（默认包含 Default + Grabbable）")]
    public LayerMask scoreLayers;
    [Tooltip("同一框只记一次分")]
    public bool oneShot = true;

    [Header("Lamp")]
    public HoopLamp lamp;               // 拖到篮球框顶部的灯
    public UnityEvent onScored;         // 通知外部（HiddenDoorRevealer 会订阅）

    bool _scored = false;

    [Header("Audio")]
    public AudioClip scoreSfx;
    [Range(0f, 1f)] public float scoreVolume = 1f;
    public bool useLocalAudioSource = true;   // 若本物体有AudioSource，优先走它

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true; // 篮筐是触发器体积
    }

    void Start()
    {
        // 缺省层：Default + Grabbable（如果有）
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

        // 要触发 OnTriggerEnter，至少一方带 Rigidbody（通常是投掷物/球）
        _scored = true;
        PlayScoreSfx();
        if (lamp) lamp.SetGreen();
        onScored?.Invoke();
    }

    static bool IsInLayerMask(int layer, LayerMask mask)
        => (mask.value & (1 << layer)) != 0; // LayerMask 基本用法:contentReference[oaicite:4]{index=4}


    void PlayScoreSfx()
    {
        if (!scoreSfx) return;

        if (useLocalAudioSource && TryGetComponent<AudioSource>(out var src))
        {
            src.PlayOneShot(scoreSfx, scoreVolume);
        }
        else
        {
            // 没有本地音源就用一次性的3D播放
            AudioSource.PlayClipAtPoint(scoreSfx, transform.position, scoreVolume);
        }
    }
}
