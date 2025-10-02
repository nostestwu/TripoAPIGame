using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
public class BossController : MonoBehaviour
{
    public enum BossState { Idle, Flying, Attacking }

    [Header("Refs")]
    public Transform player;                 // 指向玩家（必填）
    public Transform shootSocket;            // 子弹生成点（嘴/手/中心…）
    public GameObject bossBallPrefab;        // 你的 BossBall 预制体
    public GameObject portalToRevealOnDeath; // Boss 死后显示的通关传送门
    public MonsterHealth health;             // Boss 的血量脚本（用于监听死亡）

    [Header("Timings (seconds)")]
    public float idleRiseDuration1 = 2f;     // 1. 向上慢慢移动 2s（Idle）
    public float flyToPlayerFast1 = 1f;      // 2. 快速朝玩家飞 1s（Flying）
    public float idleStopDuration = 0.5f;    // 3. 停 0.5s（Idle）
    public float attackClipLength = 2.5f;    // 4. 攻击动画长度（每次）
    public float bossBallSpawnAt = 1.5f;     //    每段攻击的 1.5s 生成子弹
    public int attacksPerCycle = 3;          //    攻击 3 次
    public float flyToPlayerFast2 = 2f;      // 5. 快速朝玩家飞 2s（Flying）
    public float riseBetweenCycles = 2f;     //    再向上移动 2s，进入下一轮

    [Header("Movement")]
    public float idleRiseSpeed = 1.0f;       // Idle 上升速度（m/s）
    public float flySpeedFast = 12f;         // Flying 追击速度（m/s）
    public float turnLerp = 10f;             // 朝向插值，越大转向越快

    [Header("Anim Params")]
    public string idleStateName = "Idle";
    public string flyingStateName = "Flying";
    public string attackStateName = "Attack";
    public string attackTrigger = "Attack";  // 如果你用 Trigger 触发攻击

    Animator _anim;
    Rigidbody _rb;
    BossState _state;
    bool _running;

    void Reset()
    {
        var rb = GetComponent<Rigidbody>();
        rb.useGravity = false;                 // Boss 自己悬浮/飞行
        rb.isKinematic = true;                 // 用 MovePosition 控制即可
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    void Awake()
    {
        _anim = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        if (health)
        {
            // 用事件更优雅（见 MonsterHealth 补丁）
            health.onDeath += OnBossDeath;
        }
        if (!_running && player)
        {
            _running = true;
            StartCoroutine(AiLoop());
        }
    }

    void OnDisable()
    {
        if (health) health.onDeath -= OnBossDeath;
    }

    void OnBossDeath()
    {
        // Boss 死亡 → 显示 Portal
        if (portalToRevealOnDeath) portalToRevealOnDeath.SetActive(true);
        // 这里也可以停掉 AI、播放死亡动画等
        StopAllCoroutines();
        _running = false;
    }

    IEnumerator AiLoop()
    {
        while (true)
        {
            // 1) Idle：向上慢慢移动 2s
            SetAnim(idleStateName);
            yield return MoveFor(idleRiseDuration1, Vector3.up * idleRiseSpeed);

            // 2) Flying：快速朝玩家飞 1s
            SetAnim(flyingStateName);
            yield return FlyToPlayerFor(flyToPlayerFast1, flySpeedFast);

            // 3) Idle：停住 0.5s
            SetAnim(idleStateName);
            yield return new WaitForSeconds(idleStopDuration);  // 协程等待。:contentReference[oaicite:0]{index=0}

            // 4) Attack：播放攻击动画 attacksPerCycle 次；每次到 1.5s 生成子弹
            for (int i = 0; i < attacksPerCycle; i++)
            {
                // 切攻击动画
                SetAnim(attackStateName, useTrigger: true);

                // 在攻击剪辑 1.5s 时点发射 BossBall
                yield return new WaitForSeconds(bossBallSpawnAt);  // 协程等待。:contentReference[oaicite:1]{index=1}
                FireBossBall();

                // 等待到这次攻击动画结束
                float remain = Mathf.Max(0f, attackClipLength - bossBallSpawnAt);
                if (remain > 0f) yield return new WaitForSeconds(remain);
            }

            // 5) Flying：快速朝玩家飞 2s
            SetAnim(flyingStateName);
            yield return FlyToPlayerFor(flyToPlayerFast2, flySpeedFast);

            // 6) Idle：再向上移动 2s（进入下一轮）
            SetAnim(idleStateName);
            yield return MoveFor(riseBetweenCycles, Vector3.up * idleRiseSpeed);
        }
    }

    void SetAnim(string stateName, bool useTrigger = false)
    {
        if (!_anim) return;
        if (useTrigger && !string.IsNullOrEmpty(attackTrigger))
        {
            _anim.ResetTrigger(attackTrigger);
            _anim.SetTrigger(attackTrigger); // 触发器切到攻击层/状态。实际项目看你的 Animator 配置
        }
        else
        {
            _anim.CrossFadeInFixedTime(stateName, 0.1f); // 平滑过渡
        }
    }

    IEnumerator MoveFor(float seconds, Vector3 velocity)
    {
        float t = 0f;
        Vector3 v = velocity;
        while (t < seconds)
        {
            t += Time.deltaTime;
            FacePlayer(); // 始终朝向玩家
            // 用 Rigidbody.MovePosition 做插值移动（isKinematic=true 时可用）
            _rb.MovePosition(_rb.position + v * Time.deltaTime);
            yield return null;
        }
    }

    IEnumerator FlyToPlayerFor(float seconds, float speed)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            if (player)
            {
                Vector3 dir = (player.position - transform.position);
                dir.y = 0f; // 仅在水平面追击；若需要 3D 追击就去掉这行
                dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;

                FacePlayer();
                _rb.MovePosition(_rb.position + dir * speed * Time.deltaTime);
            }
            yield return null;
        }
    }

    void FacePlayer()
    {
        if (!player) return;
        Vector3 to = (player.position - transform.position);
        if (to.sqrMagnitude < 0.001f) return;

        // 只绕 Y 转向（看向玩家）
        to.y = 0f;
        if (to.sqrMagnitude < 0.001f) return;

        Quaternion target = Quaternion.LookRotation(to.normalized, Vector3.up); // 朝向。:contentReference[oaicite:2]{index=2}
        transform.rotation = Quaternion.Slerp(transform.rotation, target, turnLerp * Time.deltaTime);
    }

    void FireBossBall()
    {
        if (!bossBallPrefab || !shootSocket) return;

        // 实例化 BossBall，并给它初速度。:contentReference[oaicite:3]{index=3}
        var go = Instantiate(bossBallPrefab, shootSocket.position, shootSocket.rotation);
        var ball = go.GetComponent<BossBall>();
        if (ball)
        {
            Vector3 dir = player ? (player.position - shootSocket.position).normalized
                                 : shootSocket.forward;
            ball.Launch(dir);
        }
    }
}
