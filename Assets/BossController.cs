using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
public class BossController : MonoBehaviour
{
    public enum BossState { Idle, Flying, Attacking }

    [Header("Refs")]
    public Transform player;                 // ָ����ң����
    public Transform shootSocket;            // �ӵ����ɵ㣨��/��/���ġ���
    public GameObject bossBallPrefab;        // ��� BossBall Ԥ����
    public GameObject portalToRevealOnDeath; // Boss ������ʾ��ͨ�ش�����
    public MonsterHealth health;             // Boss ��Ѫ���ű������ڼ���������

    [Header("Timings (seconds)")]
    public float idleRiseDuration1 = 2f;     // 1. ���������ƶ� 2s��Idle��
    public float flyToPlayerFast1 = 1f;      // 2. ���ٳ���ҷ� 1s��Flying��
    public float idleStopDuration = 0.5f;    // 3. ͣ 0.5s��Idle��
    public float attackClipLength = 2.5f;    // 4. �����������ȣ�ÿ�Σ�
    public float bossBallSpawnAt = 1.5f;     //    ÿ�ι����� 1.5s �����ӵ�
    public int attacksPerCycle = 3;          //    ���� 3 ��
    public float flyToPlayerFast2 = 2f;      // 5. ���ٳ���ҷ� 2s��Flying��
    public float riseBetweenCycles = 2f;     //    �������ƶ� 2s��������һ��

    [Header("Movement")]
    public float idleRiseSpeed = 1.0f;       // Idle �����ٶȣ�m/s��
    public float flySpeedFast = 12f;         // Flying ׷���ٶȣ�m/s��
    public float turnLerp = 10f;             // �����ֵ��Խ��ת��Խ��

    [Header("Anim Params")]
    public string idleStateName = "Idle";
    public string flyingStateName = "Flying";
    public string attackStateName = "Attack";
    public string attackTrigger = "Attack";  // ������� Trigger ��������

    Animator _anim;
    Rigidbody _rb;
    BossState _state;
    bool _running;

    void Reset()
    {
        var rb = GetComponent<Rigidbody>();
        rb.useGravity = false;                 // Boss �Լ�����/����
        rb.isKinematic = true;                 // �� MovePosition ���Ƽ���
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
            // ���¼������ţ��� MonsterHealth ������
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
        // Boss ���� �� ��ʾ Portal
        if (portalToRevealOnDeath) portalToRevealOnDeath.SetActive(true);
        // ����Ҳ����ͣ�� AI����������������
        StopAllCoroutines();
        _running = false;
    }

    IEnumerator AiLoop()
    {
        while (true)
        {
            // 1) Idle�����������ƶ� 2s
            SetAnim(idleStateName);
            yield return MoveFor(idleRiseDuration1, Vector3.up * idleRiseSpeed);

            // 2) Flying�����ٳ���ҷ� 1s
            SetAnim(flyingStateName);
            yield return FlyToPlayerFor(flyToPlayerFast1, flySpeedFast);

            // 3) Idle��ͣס 0.5s
            SetAnim(idleStateName);
            yield return new WaitForSeconds(idleStopDuration);  // Э�̵ȴ���:contentReference[oaicite:0]{index=0}

            // 4) Attack�����Ź������� attacksPerCycle �Σ�ÿ�ε� 1.5s �����ӵ�
            for (int i = 0; i < attacksPerCycle; i++)
            {
                // �й�������
                SetAnim(attackStateName, useTrigger: true);

                // �ڹ������� 1.5s ʱ�㷢�� BossBall
                yield return new WaitForSeconds(bossBallSpawnAt);  // Э�̵ȴ���:contentReference[oaicite:1]{index=1}
                FireBossBall();

                // �ȴ�����ι�����������
                float remain = Mathf.Max(0f, attackClipLength - bossBallSpawnAt);
                if (remain > 0f) yield return new WaitForSeconds(remain);
            }

            // 5) Flying�����ٳ���ҷ� 2s
            SetAnim(flyingStateName);
            yield return FlyToPlayerFor(flyToPlayerFast2, flySpeedFast);

            // 6) Idle���������ƶ� 2s��������һ�֣�
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
            _anim.SetTrigger(attackTrigger); // �������е�������/״̬��ʵ����Ŀ����� Animator ����
        }
        else
        {
            _anim.CrossFadeInFixedTime(stateName, 0.1f); // ƽ������
        }
    }

    IEnumerator MoveFor(float seconds, Vector3 velocity)
    {
        float t = 0f;
        Vector3 v = velocity;
        while (t < seconds)
        {
            t += Time.deltaTime;
            FacePlayer(); // ʼ�ճ������
            // �� Rigidbody.MovePosition ����ֵ�ƶ���isKinematic=true ʱ���ã�
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
                dir.y = 0f; // ����ˮƽ��׷��������Ҫ 3D ׷����ȥ������
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

        // ֻ�� Y ת�򣨿�����ң�
        to.y = 0f;
        if (to.sqrMagnitude < 0.001f) return;

        Quaternion target = Quaternion.LookRotation(to.normalized, Vector3.up); // ����:contentReference[oaicite:2]{index=2}
        transform.rotation = Quaternion.Slerp(transform.rotation, target, turnLerp * Time.deltaTime);
    }

    void FireBossBall()
    {
        if (!bossBallPrefab || !shootSocket) return;

        // ʵ���� BossBall�����������ٶȡ�:contentReference[oaicite:3]{index=3}
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
