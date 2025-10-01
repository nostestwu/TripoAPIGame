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

    // ========= 新增：一次性触发选项 =========
    [Header("One-shot Options")]
    [Tooltip("勾选后：首次触碰即锁存为‘已触发’，保持开门；后续离开也不再关闭。")]
    public bool oneShotLatch = false;

    [Tooltip("一次性‘脉冲’模式：首次触碰仅打开一小段时间，然后自动关闭。单位：秒；<=0 则不启用。")]
    public float oneShotPulseDuration = 0f;

    [Tooltip("一次性触发后是否禁用自身触发器（防止重复触发/计数）。")]
    public bool disableColliderAfterOneShot = true;

    // =====================================

    // 当前压住的碰撞体集合（避免一个物体多个Collider重复计数）
    private readonly HashSet<Collider> _pressing = new();
    bool _isPressed = false;     // 记住上一次状态
    bool _latched = false;       // 记住是否已一次性触发
    Coroutine _pulseCo;          // 脉冲协程句柄
    Collider _selfCol;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;    // 按钮区域必须是触发器
    }

    void Awake()
    {
        _selfCol = GetComponent<Collider>();
        if (_selfCol) _selfCol.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsValid(other)) return;

        // —— 一次性触发（锁存）优先 —— 
        if (!_latched && (oneShotLatch || oneShotPulseDuration > 0f))
        {
            TriggerOnce();
            return; // 锁存/脉冲模式下，无需进入持续计数逻辑
        }

        _pressing.Add(other);
        UpdateState();
    }

    void OnTriggerExit(Collider other)
    {
        if (_latched) return; // 一次性触发后，退出不再影响状态
        if (!_pressing.Remove(other)) return;
        UpdateState();
    }

    // 关闭或卸载时复位
    void OnDisable()
    {
        _pressing.Clear();
        _isPressed = false;
        _latched = false;
        if (_pulseCo != null) { StopCoroutine(_pulseCo); _pulseCo = null; }
        foreach (var d in doors) if (d) d.SetOpen(false);
        onPressedChanged?.Invoke(false);
        if (_selfCol) _selfCol.enabled = true;   // 重新启用触发器，方便下次进场景可再用
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

    // —— 核心：普通“持续按压”逻辑（非 one-shot 时生效）
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

    // —— 新增：一次性触发入口（锁存或脉冲）
    void TriggerOnce()
    {
        _latched = true;

        // 统一先开门 & 通知
        foreach (var d in doors) if (d) d.SetOpen(true);
        onPressedChanged?.Invoke(true);
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.doorOpenClip, transform.position, 1f);

        // 锁存型：保持开启，直接忽略后续触发/离开
        if (oneShotLatch && oneShotPulseDuration <= 0f)
        {
            if (disableColliderAfterOneShot && _selfCol) _selfCol.enabled = false;
            return;
        }

        // 脉冲型：N 秒后自动关闭并可选择恢复触发器
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

        // 脉冲结束后：重置为“未锁存”，允许再次触发（如需一次性仅触发一次，可把下面两行删掉）
        _latched = false;
        if (_selfCol) _selfCol.enabled = true;

        _pulseCo = null;
    }
}
