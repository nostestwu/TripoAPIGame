using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class PressurePlate : MonoBehaviour
{
    [Header("Detect")]
    public LayerMask detectLayers = ~0;          // 允许触发的层（玩家/可抓取等）
    public bool ignoreTriggerColliders = true;   // 忽略对方的触发器
    public bool requireNonKinematicRB = false;   // 需要对方带非运动学刚体才算“压住”

    [Header("Target Door(s)")]
    public List<SlidingDoor> doors = new();      // 一个按钮可以驱动多个门
    public UnityEvent<bool> onPressedChanged;    // 可选：给UI/音效用

    // 记住“当前压在上面的”Collider，避免一个物体多个 Collider 造成重复计数
    private readonly HashSet<Collider> _pressing = new();

    // 放在类的字段区
    bool _isPressed = false;   // 记住上一次状态

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;                    // 按钮区域必须是触发器
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

    // 替换 OnDisable() —— 关闭时静默复位（不播音）
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

    // 替换原来的 UpdateState()
    void UpdateState()
    {
        bool pressed = _pressing.Count > 0;

        // 只有状态变化时才执行（避免同一次开门多次播放）
        if (pressed == _isPressed) return;
        _isPressed = pressed;

        foreach (var d in doors) if (d) d.SetOpen(pressed);
        onPressedChanged?.Invoke(pressed);

        // 只在状态翻转时播一次
        if (pressed)
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.doorOpenClip, transform.position, 1f);

    }

}
