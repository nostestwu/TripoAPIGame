using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class BossController : MonoBehaviour
{
    [Header("Refs")]
    public Transform player;
    public Transform shootSocket;
    public GameObject bossBallPrefab;
    public GameObject portalToRevealOnDeath;
    public MonsterHealth health;

    [Header("Timings (s)")]
    public float idleRiseDuration = 2f;
    public float flyDuration1 = 1f;
    public float idlePauseDuration = 0.5f;
    public float attackClipLength = 2.5f;
    public float bossBallSpawnAt = 1.5f;
    public int attacksPerCycle = 3;
    public float flyDuration2 = 2f;
    public float idleRiseBetweenCycles = 2f;

    [Header("Speeds")]
    public float riseSpeed = 1f;
    public float approachSpeed = 8f;    // �����ٶȣ�ԭ���� 12f�����ڵ���
    public float heightLerpSpeed = 2f;  // �߶ȿ����Ĳ�ֵ�ٶȣ�Y ����һ�㣩
    public float turnLerp = 10f;

    [Header("Anim Params")]
    public string idleStateName = "Idle";
    public string flyingStateName = "Flying";
    public string attackStateName = "Attack";
    public string attackTrigger = "Attack";

    Animator _anim;
    bool _running = false;

    void Awake()
    {
        _anim = GetComponent<Animator>();
    }

    void Start()
    {
        if (player == null)
        {
            var go = GameObject.FindWithTag("Player");
            if (go != null) player = go.transform;
        }
        if (player == null)
        {
            Debug.LogError("[BossController] �Ҳ��� Player", this);
        }

        if (!_running)
        {
            _running = true;
            StartCoroutine(AiLoop());
        }
    }

    void OnEnable()
    {
        if (health != null)
            health.onDeath += OnBossDeath;
    }

    void OnDisable()
    {
        if (health != null)
            health.onDeath -= OnBossDeath;
    }

    void OnBossDeath()
    {
        if (portalToRevealOnDeath != null)
            portalToRevealOnDeath.SetActive(true);
        StopAllCoroutines();
    }

    IEnumerator AiLoop()
    {
        while (true)
        {
            // 1. Idle rise upward
            _anim.CrossFade(idleStateName, 0.1f);
            float t0 = 0f;
            while (t0 < idleRiseDuration)
            {
                t0 += Time.deltaTime;
                transform.position += Vector3.up * riseSpeed * Time.deltaTime;
                FacePlayer();
                yield return null;
            }

            // 2. Fly toward player (phase 1) �� ��ά���� + Y �Ỻ��
            _anim.CrossFade(flyingStateName, 0.1f);
            float tf = 0f;
            while (tf < flyDuration1)
            {
                tf += Time.deltaTime;
                if (player != null)
                {
                    Vector3 current = transform.position;
                    Vector3 target = player.position;

                    // XZ ֱ�ӿ���
                    Vector3 newPosXZ = Vector3.MoveTowards(current, target, approachSpeed * Time.deltaTime);

                    // Y ���ֵ��������һ�㣩
                    float newY = Mathf.Lerp(current.y, target.y, heightLerpSpeed * Time.deltaTime);

                    transform.position = new Vector3(newPosXZ.x, newY, newPosXZ.z);
                }
                // ��ӷ�����б��x �� rotate ~50 �ȣ���˵ flying �����ǰ� x ����ת�� 50 �ȣ�
                ApplyFlyingTilt();
                FacePlayer();
                yield return null;
            }

            // 3. Idle pause
            _anim.CrossFade(idleStateName, 0.1f);
            float tp = 0f;
            while (tp < idlePauseDuration)
            {
                tp += Time.deltaTime;
                FacePlayer();
                yield return null;
            }

            // 4. Attack cycles
            for (int i = 0; i < attacksPerCycle; i++)
            {
                _anim.ResetTrigger(attackTrigger);
                _anim.SetTrigger(attackTrigger);

                float t = 0f;
                while (t < bossBallSpawnAt)
                {
                    t += Time.deltaTime;
                    FacePlayer();
                    yield return null;
                }

                FireBossBall();

                float remain = Mathf.Max(0f, attackClipLength - bossBallSpawnAt);
                float t2 = 0f;
                while (t2 < remain)
                {
                    t2 += Time.deltaTime;
                    FacePlayer();
                    yield return null;
                }
            }

            // 5. Fly toward player (phase 2) �� ��ά���� + Y �Ỻ��
            _anim.CrossFade(flyingStateName, 0.1f);
            float tf2 = 0f;
            while (tf2 < flyDuration2)
            {
                tf2 += Time.deltaTime;
                if (player != null)
                {
                    Vector3 current = transform.position;
                    Vector3 target = player.position;

                    Vector3 newPosXZ = Vector3.MoveTowards(current, target, approachSpeed * Time.deltaTime);
                    float newY = Mathf.Lerp(current.y, target.y, heightLerpSpeed * Time.deltaTime);

                    transform.position = new Vector3(newPosXZ.x, newY, newPosXZ.z);
                }
                ApplyFlyingTilt();
                FacePlayer();
                yield return null;
            }

            // 6. Idle rise between cycles
            _anim.CrossFade(idleStateName, 0.1f);
            float tbr = 0f;
            while (tbr < idleRiseBetweenCycles)
            {
                tbr += Time.deltaTime;
                transform.position += Vector3.up * riseSpeed * Time.deltaTime;
                FacePlayer();
                yield return null;
            }
        }
    }

    void ApplyFlyingTilt()
    {
        // �� Boss �ڷ���ʱ��һ�� X ����ת��б������ -50 �Ȼ� +50 �ȵĸ���/��б
        // ������Ҫ�����ر��� X ����б 50 ����ǰ��������:
        Quaternion tilt = Quaternion.Euler(50f, 0f, 0f);
        // Ҳ���Բ�ֵ��ת�뵱ǰ�ںϣ�
        transform.rotation = Quaternion.Slerp(transform.rotation, transform.rotation * tilt, 5f * Time.deltaTime);
    }

    void FacePlayer()
    {
        if (player == null) return;
        Vector3 to = player.position - transform.position;
        to.y = 0f;  // ˮƽ���򣬲�Ӱ������
        if (to.sqrMagnitude < 0.0001f) return;
        Quaternion target = Quaternion.LookRotation(to.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, turnLerp * Time.deltaTime);
    }

    void FireBossBall()
    {
        if (bossBallPrefab == null || shootSocket == null) return;
        if (player == null)
        {
            var go = GameObject.FindWithTag("Player");
            if (go != null) player = go.transform;
        }
        GameObject goBall = Instantiate(bossBallPrefab, shootSocket.position, shootSocket.rotation);
        var ball = goBall.GetComponent<BossBall>();
        if (ball != null)
        {
            Vector3 dir = (player != null)
                ? (player.position - shootSocket.position).normalized
                : shootSocket.forward;
            ball.Launch(dir);
        }
    }
}
