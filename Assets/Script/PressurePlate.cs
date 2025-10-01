using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class PressurePlate : MonoBehaviour
{
    [Header("Detect")]
    public LayerMask detectLayers = ~0;
    public bool ignoreTriggerColliders = true;
    public bool requireNonKinematicRB = false;

    [Header("Target Door(s)")]
    public List<SlidingDoor> doors = new();
    public UnityEvent<bool> onPressedChanged;

    // ========= ������һ���Դ���ѡ�� =========
    [Header("One-shot Options")]
    [Tooltip("��ѡ���״δ���������Ϊ���Ѵ����������ֿ��ţ������뿪Ҳ���ٹرա�")]
    public bool oneShotLatch = false;

    [Tooltip("һ���ԡ����塯ģʽ���״δ�������һС��ʱ�䣬Ȼ���Զ��رա���λ���룻<=0 �����á�")]
    public float oneShotPulseDuration = 0f;

    [Tooltip("һ���Դ������Ƿ����������������ֹ�ظ�����/��������")]
    public bool disableColliderAfterOneShot = true;

    // =====================================

    // ��ǰѹס����ײ�弯�ϣ�����һ��������Collider�ظ�������
    private readonly HashSet<Collider> _pressing = new();
    bool _isPressed = false;     // ��ס��һ��״̬
    bool _latched = false;       // ��ס�Ƿ���һ���Դ���
    Coroutine _pulseCo;          // ����Э�̾��
    Collider _selfCol;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;    // ��ť��������Ǵ�����
    }

    void Awake()
    {
        _selfCol = GetComponent<Collider>();
        if (_selfCol) _selfCol.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsValid(other)) return;

        // ���� һ���Դ��������棩���� ���� 
        if (!_latched && (oneShotLatch || oneShotPulseDuration > 0f))
        {
            TriggerOnce();
            return; // ����/����ģʽ�£����������������߼�
        }

        _pressing.Add(other);
        UpdateState();
    }

    void OnTriggerExit(Collider other)
    {
        if (_latched) return; // һ���Դ������˳�����Ӱ��״̬
        if (!_pressing.Remove(other)) return;
        UpdateState();
    }

    // �رջ�ж��ʱ��λ
    void OnDisable()
    {
        _pressing.Clear();
        _isPressed = false;
        _latched = false;
        if (_pulseCo != null) { StopCoroutine(_pulseCo); _pulseCo = null; }
        foreach (var d in doors) if (d) d.SetOpen(false);
        onPressedChanged?.Invoke(false);
        if (_selfCol) _selfCol.enabled = true;   // �������ô������������´ν�����������
    }

    bool IsValid(Collider other)
    {
        if (!other || other == _selfCol) return false;
        if (ignoreTriggerColliders && other.isTrigger) return false;
        if ((detectLayers.value & (1 << other.gameObject.layer)) == 0) return false;

        if (requireNonKinematicRB)
        {
            var rb = other.attachedRigidbody;
            if (!rb || rb.isKinematic) return false;
        }
        return true;
    }

    // ���� ���ģ���ͨ��������ѹ���߼����� one-shot ʱ��Ч��
    void UpdateState()
    {
        bool pressed = _pressing.Count > 0;
        if (pressed == _isPressed) return;

        _isPressed = pressed;
        foreach (var d in doors) if (d) d.SetOpen(pressed);
        onPressedChanged?.Invoke(pressed);

        if (pressed)
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.doorOpenClip, transform.position, 1f);
    }

    // ���� ������һ���Դ�����ڣ���������壩
    void TriggerOnce()
    {
        _latched = true;

        // ͳһ�ȿ��� & ֪ͨ
        foreach (var d in doors) if (d) d.SetOpen(true);
        onPressedChanged?.Invoke(true);
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.doorOpenClip, transform.position, 1f);

        // �����ͣ����ֿ�����ֱ�Ӻ��Ժ�������/�뿪
        if (oneShotLatch && oneShotPulseDuration <= 0f)
        {
            if (disableColliderAfterOneShot && _selfCol) _selfCol.enabled = false;
            return;
        }

        // �����ͣ�N ����Զ��رղ���ѡ��ָ�������
        if (oneShotPulseDuration > 0f)
        {
            if (_pulseCo != null) StopCoroutine(_pulseCo);
            _pulseCo = StartCoroutine(PulseCloseAfter(oneShotPulseDuration));
        }
    }

    IEnumerator PulseCloseAfter(float seconds)
    {
        if (disableColliderAfterOneShot && _selfCol) _selfCol.enabled = false;

        yield return new WaitForSeconds(seconds);

        foreach (var d in doors) if (d) d.SetOpen(false);
        onPressedChanged?.Invoke(false);

        // �������������Ϊ��δ���桱�������ٴδ���������һ���Խ�����һ�Σ��ɰ���������ɾ����
        _latched = false;
        if (_selfCol) _selfCol.enabled = true;

        _pulseCo = null;
    }
}
