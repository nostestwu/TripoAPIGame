using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FanBlower : MonoBehaviour
{
    public enum PowerMode { AlwaysOn, ExternalGate }

    [Header("Power")]
    public PowerMode powerMode = PowerMode.AlwaysOn;
    [Tooltip("�� PowerMode=ExternalGate ʱ�����ⲿ���� SetGate(true/false) ���ƿ���")]
    [SerializeField] bool gateState = false;

    [Header("Wind")]
    [Tooltip("������С��Use Acceleration ����������Force ������������")]
    public float windStrength = 25f;
    public ForceMode windMode = ForceMode.Acceleration;
    [Tooltip("�ɱ������Ĳ㣨Ĭ�ϳ���ֻ�� Grabbable��")]
    public LayerMask affectLayers = ~0;
    public bool ignoreTriggerColliders = true;

    [Header("Visual (optional)")]
    [Tooltip("ҶƬ/���������壻��������ת")]
    public Transform blades;
    [Tooltip("������ת�٣�RPM��")]
    public float bladesRpmOn = 600f;
    [Tooltip("�ϵ��Ķ���ת�٣�RPM��")]
    public float bladesRpmOff = 0f;
    [Tooltip("ת�ٱ仯ƽ��")]
    public float spinLerp = 8f;

    float currentRpm;
    readonly HashSet<Rigidbody> _appliedThisStep = new HashSet<Rigidbody>();
    Collider _trigger;

    void Reset()
    {
        _trigger = GetComponent<Collider>();
        if (_trigger) _trigger.isTrigger = true;

        // Ĭ��ֻ�� Grabbable �㣨���������򱣳� ~0��
        int g = LayerMask.NameToLayer("Grabbable");
        if (g != -1) affectLayers = 1 << g;
    }

    void Awake()
    {
        _trigger = GetComponent<Collider>();
        if (_trigger && !_trigger.isTrigger)
            _trigger.isTrigger = true; // ���������Ǵ�����
    }

    // �� PressurePlate.onPressedChanged(bool) ֱ�Ӱ����
    public void SetGate(bool pressed)
    {
        gateState = pressed;
    }

    bool Powered =>
        powerMode == PowerMode.AlwaysOn ? true : gateState;

    void Update()
    {
        // ��ת����ҶƬ���Ʊ��� Z �ᣩ
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
        // ÿ������ÿ������ֻʩ��һ���������⸴����ײ���ظ����ӣ�
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

        // ��һ����ֻ��ͬһ����ʩ��һ����
        if (!_appliedThisStep.Add(rb)) return;

        // �ط���ǰ�򣨱��� Z+��ʩ����
        rb.AddForce(transform.forward * windStrength, windMode);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // �򵥻���ǰ��
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1.0f);
        Gizmos.DrawSphere(transform.position + transform.forward * 1.0f, 0.05f);
    }
#endif
}
