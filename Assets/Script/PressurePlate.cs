using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class PressurePlate : MonoBehaviour
{
    [Header("Detect")]
    public LayerMask detectLayers = ~0;          // �������Ĳ㣨���/��ץȡ�ȣ�
    public bool ignoreTriggerColliders = true;   // ���ԶԷ��Ĵ�����
    public bool requireNonKinematicRB = false;   // ��Ҫ�Է������˶�ѧ������㡰ѹס��

    [Header("Target Door(s)")]
    public List<SlidingDoor> doors = new();      // һ����ť�������������
    public UnityEvent<bool> onPressedChanged;    // ��ѡ����UI/��Ч��

    // ��ס����ǰѹ������ġ�Collider������һ�������� Collider ����ظ�����
    private readonly HashSet<Collider> _pressing = new();

    // ��������ֶ���
    bool _isPressed = false;   // ��ס��һ��״̬

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;                    // ��ť��������Ǵ�����
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsValid(other)) return;
        _pressing.Add(other);
        UpdateState();
    }

    void OnTriggerExit(Collider other)
    {
        if (!_pressing.Remove(other)) return;
        UpdateState();
    }

    // �滻 OnDisable() ���� �ر�ʱ��Ĭ��λ����������
    void OnDisable()
    {
        if (_pressing.Count > 0) _pressing.Clear();
        _isPressed = false;
        foreach (var d in doors) if (d) d.SetOpen(false);
        onPressedChanged?.Invoke(false);
    }


    bool IsValid(Collider other)
    {
        if (!other || other == GetComponent<Collider>()) return false;
        if (ignoreTriggerColliders && other.isTrigger) return false;
        if ((detectLayers.value & (1 << other.gameObject.layer)) == 0) return false;

        if (requireNonKinematicRB)
        {
            var rb = other.attachedRigidbody;
            if (!rb || rb.isKinematic) return false;
        }
        return true;
    }

    // �滻ԭ���� UpdateState()
    void UpdateState()
    {
        bool pressed = _pressing.Count > 0;

        // ֻ��״̬�仯ʱ��ִ�У�����ͬһ�ο��Ŷ�β��ţ�
        if (pressed == _isPressed) return;
        _isPressed = pressed;

        foreach (var d in doors) if (d) d.SetOpen(pressed);
        onPressedChanged?.Invoke(pressed);

        // ֻ��״̬��תʱ��һ��
        if (pressed)
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.doorOpenClip, transform.position, 1f);

    }

}
