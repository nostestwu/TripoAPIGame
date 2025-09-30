using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FanBlower : MonoBehaviour
{
    public enum PowerMode { AlwaysOn, ExternalGate }

    [Header("Power")]
    public PowerMode powerMode = PowerMode.AlwaysOn;
    [Tooltip("当 PowerMode=ExternalGate 时，由外部调用 SetGate(true/false) 控制开关")]
    [SerializeField] bool gateState = false;

    [Header("Wind")]
    [Tooltip("风力大小。Use Acceleration 忽略质量，Force 按质量受力。")]
    public float windStrength = 25f;
    public ForceMode windMode = ForceMode.Acceleration;
    [Tooltip("可被吹动的层（默认尝试只勾 Grabbable）")]
    public LayerMask affectLayers = ~0;
    public bool ignoreTriggerColliders = true;

    [Header("Visual (optional)")]
    [Tooltip("叶片/可视子物体；留空则不旋转")]
    public Transform blades;
    [Tooltip("满功率转速（RPM）")]
    public float bladesRpmOn = 600f;
    [Tooltip("断电后的惰性转速（RPM）")]
    public float bladesRpmOff = 0f;
    [Tooltip("转速变化平滑")]
    public float spinLerp = 8f;

    float currentRpm;
    readonly HashSet<Rigidbody> _appliedThisStep = new HashSet<Rigidbody>();
    Collider _trigger;

    void Reset()
    {
        _trigger = GetComponent<Collider>();
        if (_trigger) _trigger.isTrigger = true;

        // 默认只吹 Grabbable 层（若不存在则保持 ~0）
        int g = LayerMask.NameToLayer("Grabbable");
        if (g != -1) affectLayers = 1 << g;
    }

    void Awake()
    {
        _trigger = GetComponent<Collider>();
        if (_trigger && !_trigger.isTrigger)
            _trigger.isTrigger = true; // 风区必须是触发体
    }

    // 给 PressurePlate.onPressedChanged(bool) 直接绑定这个
    public void SetGate(bool pressed)
    {
        gateState = pressed;
    }

    bool Powered =>
        powerMode == PowerMode.AlwaysOn ? true : gateState;

    void Update()
    {
        // 旋转可视叶片（绕本地 Z 轴）
        float target = Powered ? bladesRpmOn : bladesRpmOff;
        currentRpm = Mathf.Lerp(currentRpm, target, 1f - Mathf.Exp(-spinLerp * Time.deltaTime));
        if (blades)
        {
            float degPerSec = currentRpm * 6f; // RPM -> deg/s
            blades.Rotate(0f, 0f, degPerSec * Time.deltaTime, Space.Self);
        }
    }

    void FixedUpdate()
    {
        // 每个物体每个物理步只施加一次力（避免复合碰撞体重复叠加）
        _appliedThisStep.Clear();
    }

    void OnTriggerStay(Collider other)
    {
        if (!Powered) return;
        if (!other) return;
        if (ignoreTriggerColliders && other.isTrigger) return;

        if ((affectLayers.value & (1 << other.gameObject.layer)) == 0)
            return;

        var rb = other.attachedRigidbody;
        if (!rb || rb.isKinematic) return;

        // 这一物理步只对同一刚体施加一次力
        if (!_appliedThisStep.Add(rb)) return;

        // 沿风扇前向（本地 Z+）施加力
        rb.AddForce(transform.forward * windStrength, windMode);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // 简单画出前向
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1.0f);
        Gizmos.DrawSphere(transform.position + transform.forward * 1.0f, 0.05f);
    }
#endif
}
