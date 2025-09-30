using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RayPickupController : MonoBehaviour
{
    [Header("Refs")]
    public Transform rayOrigin;     // 射线起点与朝向 -> 设为 Player
    public Transform holdParent;    // 参考系 -> 设为 Player
    public Camera cam;            // 用相机坐标解释“屏幕左右/上下/前后”

    [Header("Raycast")]
    public float maxGrabDistance = 3f;
    public LayerMask grabMask = ~0;    // 建议只勾 "Grabbable"
    public bool drawDebugRay = false;

    [Header("Ray Visual")]
    public bool showRayInGame = true;
    public float rayWidth = 0.02f;
    public Color rayColorNoHit = new Color(0.8f, 0.8f, 0.8f, 0.7f);
    public Color rayColorCanGrab = new Color(0.2f, 1f, 0.2f, 0.9f);
    public Color rayColorBlocked = new Color(1f, 0.3f, 0.3f, 0.9f);
    public float aimAssistRadius = 0.08f;   // ← 命中“容差”，越大越好碰到

    LineRenderer _rayLR;


    [Header("Hold base")]
    public float baseDistance = 2f;  // 初始前向距离（贴近 rayOrigin）
    public float baseHeight = 1f;  // 初始抬高
    public float smoothing = 18f;   // 锚点平滑
    public float breakForce = 1e8f, breakTorque = 1e8f;
    public float maxGrabMass = 200f;

    [Header("Mouse control (offset around screen axes)")]
    public float mouseXSensitivity = 0.02f;   // 左右
    public float mouseYSensitivity = 0.02f;   // 上下
    public float scrollSensitivity = 1.0f;   // 远近
    public Vector2 lateralLimits = new Vector2(-1.2f, 1.2f);
    public Vector2 verticalLimits = new Vector2(0.2f, 2.0f);
    public Vector2 distanceLimits = new Vector2(0.5f, 3.5f);
    public bool invertHorizontal = false;

    [Header("Aim (tilt the ray with mouse Y)")]
    public bool aimRayWithMouse = true;       // 鼠标上下可抬/压射线
    public float aimPitchSensitivity = 1.5f;     // 度/鼠标增量
    public Vector2 aimPitchLimits = new Vector2(-60f, 60f);
    float aimPitchDeg = 0f;                       // 当前俯仰角（度）

    [Header("Rotate while holding")]
    public KeyCode rotateModifier = KeyCode.LeftShift;  // 按住再滚轮 = 旋转；设为 KeyCode.None 则滚轮永远旋转
    public float wheelRotDegrees = 30f;                 // 每个滚轮单位转多少度
    public float rotSmoothing = 18f;                    // 旋转跟随平滑
    public float heldLinearDrag = 2f, heldAngularDrag = 5f; // 抓取时提高阻尼更稳

    [Header("Throw (E to charge & release)")]
    public KeyCode throwKey = KeyCode.E;
    public float minImpulse = 4f;        // 最小冲量（米/秒*质量）
    public float maxImpulse = 20f;       // 最大冲量
    public float maxChargeTime = 1.2f;   // 蓄力上限（秒）
    public AnimationCurve chargeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    // 可选：投出瞬间给一点上挑
    public float launchUpBias = 0.0f;    // 0 = 纯前向；0.1~0.2 = 略微抬起
    float _chargeT = 0f;
    bool _charging = false;
    bool _throwQueued = false; // 防止同帧重复触发

    // 累积的绕“屏幕X轴”的角度（度），可无限增减
    float spinXDeg = 0f;

    // 新增：绕Y轴的左右键旋转（度/秒）
    public float buttonYawSpeed = 120f;   // ← 你想旋转更快/更慢可改这个
    float spinYDeg = 0f;                  // ← 新增：累计“水平(yaw)”角

    // 本帧/本次滚轮的增量角度
    float savedDrag, savedAngDrag; // 放下时恢复

    // 本帧目标位姿（由 Update 计算，FixedUpdate 复用）
    Vector3 _frameTargetPos;
    Quaternion _frameTargetRot;

    // 运行态
    Rigidbody held;
    FixedJoint joint;
    Rigidbody holdRb;
    Transform holdAnchor;

    float curDist, offX, offY;

    // 抓取时忽略 玩家↔物体 的碰撞；放下再恢复
    List<(Collider a, Collider b)> ignoredPairs = new List<(Collider, Collider)>();
    Collider[] playerCols;
    Collider[] heldCols;

    [Header("Aim source")]
    public Transform aimFrom; // 建议拖 ThirdPersonController 的 CinemachineCameraTarget


    void Awake()
    {
        if (!cam) cam = Camera.main;                                        // :contentReference[oaicite:1]{index=1}
        if (!rayOrigin) rayOrigin = transform;
        if (!holdParent) holdParent = rayOrigin;

        if (grabMask.value == 0)
        {
            int g = LayerMask.NameToLayer("Grabbable");
            if (g != -1) grabMask = 1 << g;
        }
    }

    void Start()
    {
        // 锚点贴在 rayOrigin（让 HoldPoint 跟随玩家与场景切换）
        holdAnchor = new GameObject("HoldPoint").transform;
        holdAnchor.SetParent(holdParent ? holdParent : rayOrigin, false);  // ← 新增

        holdRb = holdAnchor.gameObject.AddComponent<Rigidbody>();
        holdRb.isKinematic = true;
        holdRb.useGravity = false;
        holdRb.interpolation = RigidbodyInterpolation.Interpolate;
        holdRb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        ResetOffsets();
        playerCols = holdParent.GetComponentsInChildren<Collider>(true);

        if (aimFrom == null)
        {
            var tpc = FindObjectOfType<StarterAssets.ThirdPersonController>();
            if (tpc && tpc.CinemachineCameraTarget != null)
                aimFrom = tpc.CinemachineCameraTarget.transform;
        }
        EnsureRayRenderer();

    }


    void Update()
    {
        // F：抓或放
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (held) Drop();
            else TryGrab();
        }

        // 俯仰：鼠标 Y 控制 ray 抬/压（不必转相机）
        if (aimRayWithMouse)
        {
            float dy = Input.GetAxis("Mouse Y");
            aimPitchDeg = Mathf.Clamp(aimPitchDeg - dy * aimPitchSensitivity,
                                      aimPitchLimits.x, aimPitchLimits.y);
        }

        // 抓住时：偏移 + 滚轮旋转
        if (held)
        {
            float dx = Input.GetAxis("Mouse X");
            float dy = Input.GetAxis("Mouse Y");
            float signedDx = invertHorizontal ? -dx : dx;

            offX = Mathf.Clamp(offX + signedDx * mouseXSensitivity, lateralLimits.x, lateralLimits.y);
            offY = Mathf.Clamp(offY + dy * mouseYSensitivity, verticalLimits.x, verticalLimits.y);

            // 🔁 滚轮 => 围绕“屏幕/握持坐标系”的 X 轴无限累积旋转（只改角度，不改距离）
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.0001f)
            {
                spinXDeg += scroll * wheelRotDegrees;   // 不做夹角限制，可无限转
                                                        // 可选：防止数值过大，做个温和取模，完全不影响“无限旋”的观感
                if (spinXDeg > 10000f || spinXDeg < -10000f) spinXDeg %= 360f;
            }

            // 🆕 鼠标左右键：绕“水平(Y)轴”旋转（按住持续转）
            if (Input.GetMouseButton(0))
            {            // 左键：顺时针（可按你习惯换正负）
                spinYDeg += buttonYawSpeed * Time.deltaTime;
            }
            if (Input.GetMouseButton(1))
            {            // 右键：逆时针
                spinYDeg -= buttonYawSpeed * Time.deltaTime;
            }
            // 温和取模防数值膨胀（不影响“无限旋”观感）
            if (spinYDeg > 10000f || spinYDeg < -10000f) spinYDeg %= 360f;
        }

        // 仅在“抓住物体”时才允许投掷
        if (held)
        {
            // 开始蓄力
            if (Input.GetKeyDown(throwKey))
            {
                _charging = true;
                _throwQueued = false;
                _chargeT = 0f;
            }
            // 按住累积（0..1）
            if (_charging && Input.GetKey(throwKey))
            {
                _chargeT = Mathf.Clamp01(_chargeT + Time.deltaTime / maxChargeTime);
                // 你可以在这里驱动UI：例如根据 _chargeT 填充条
            }
            // 松开 → 计算冲量并投掷
            if (_charging && Input.GetKeyUp(throwKey) && !_throwQueued)
            {
                _charging = false;
                _throwQueued = true;

                float curve = (chargeCurve != null) ? chargeCurve.Evaluate(_chargeT) : _chargeT;
                float impulseMag = Mathf.Lerp(minImpulse, maxImpulse, curve);

                // 用当前的瞄准前向（含你的鼠标俯仰）
                GetAimBasis(out var fwd, out _, out var up);
                Vector3 dir = (fwd + up * launchUpBias).normalized;

                StartCoroutine(ThrowRoutine(dir, impulseMag));
            }
        }
        else
        {
            // 手里没东西就重置
            _charging = false;
            _chargeT = 0f;
        }

        if (drawDebugRay)
        {
            GetAimBasis(out var fwd, out _, out _);
            Debug.DrawRay(rayOrigin.position, fwd * maxGrabDistance, Color.yellow, 0, false);
        }

        // --- 每帧算“本帧目标位姿”并直接把锚点Transform放过去（画面无延迟） ---
        GetAimBasis(out var fwdNow, out var rgtNow, out var upNow);
        _frameTargetPos = rayOrigin.position + fwdNow * curDist + rgtNow * offX + upNow * offY;

        Quaternion baseRotNow = Quaternion.LookRotation(fwdNow, upNow);
        _frameTargetRot = baseRotNow
                        * Quaternion.AngleAxis(spinYDeg, Vector3.up)
                        * Quaternion.AngleAxis(spinXDeg, Vector3.right);

        // 直接设 Transform：确保跟随角色在“渲染帧”就到位
        if (holdAnchor)
        {
            holdAnchor.position = _frameTargetPos;
            holdAnchor.rotation = _frameTargetRot;
        }

        DrawGrabRay();

    }


    void FixedUpdate()
    {
        if (!holdRb) return; // 防止场景切换后还没重建

        // 用 Update 缓存的目标位姿（避免二次计算和重名变量）
        float kPos = held ? 1f : (1f - Mathf.Exp(-smoothing * Time.fixedDeltaTime));
        Vector3 newPos = Vector3.Lerp(holdRb.position, _frameTargetPos, kPos);
        holdRb.MovePosition(newPos);

        float kRot = 1f - Mathf.Exp(-rotSmoothing * Time.fixedDeltaTime);
        Quaternion smoothed = Quaternion.Slerp(holdRb.rotation, _frameTargetRot, kRot);
        holdRb.MoveRotation(smoothed);

        if (held) held.angularVelocity = Vector3.zero;
    }

    // 1) 创建/配置 LineRenderer（兼容内置/URP/HDRP，并父到 rayOrigin）
    void EnsureRayRenderer()
    {
        if (!showRayInGame) return;
        if (_rayLR) return;

        var go = new GameObject("GrabRay_Line");
        go.transform.SetParent(rayOrigin ? rayOrigin : transform, false);
        // 把层设置成和玩家/射线起点一致，避免被相机 CullingMask 裁掉
        go.layer = (rayOrigin ? rayOrigin.gameObject.layer : gameObject.layer);

        _rayLR = go.AddComponent<LineRenderer>();
        _rayLR.positionCount = 2;
        _rayLR.useWorldSpace = true;
        _rayLR.alignment = LineAlignment.View;   // 让线总是对着相机，避免“看不见侧边”
        _rayLR.widthMultiplier = rayWidth;
        _rayLR.numCapVertices = 8;
        _rayLR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _rayLR.receiveShadows = false;

        _rayLR.material = CreateLineMaterial();  // 见下
        SetLineColor(rayColorNoHit);             // 初始颜色
    }
    Material CreateLineMaterial()
    {
        // 依次尝试 URP / HDRP / 内置 的无光材质
        Shader[] candidates = new Shader[] {
        Shader.Find("Universal Render Pipeline/Unlit"),
        Shader.Find("HDRP/Unlit"),
        Shader.Find("Unlit/Color"),
        Shader.Find("Sprites/Default"),
    };
        Shader s = null;
        foreach (var sh in candidates) if (sh) { s = sh; break; }
        return new Material(s);
    }

    void SetLineColor(Color c)
    {
        if (!_rayLR) return;
        _rayLR.startColor = _rayLR.endColor = c;
        // 兼容不同 shader 的主色属性
        if (_rayLR.material)
        {
            if (_rayLR.material.HasProperty("_BaseColor")) _rayLR.material.SetColor("_BaseColor", c);
            else if (_rayLR.material.HasProperty("_Color")) _rayLR.material.SetColor("_Color", c);
        }
    }
    // ★ 新增：在场景卸载后若锚点/刚体被销毁，自动重建
    void EnsureHoldAnchor()
    {
        if (holdAnchor == null)
        {
            holdAnchor = new GameObject("HoldPoint").transform;
            holdAnchor.SetParent(holdParent ? holdParent : rayOrigin, false);
        }

        if (holdRb == null)
        {
            holdRb = holdAnchor.GetComponent<Rigidbody>();
            if (!holdRb) holdRb = holdAnchor.gameObject.AddComponent<Rigidbody>();
            holdRb.isKinematic = true;
            holdRb.useGravity = false;
            holdRb.interpolation = RigidbodyInterpolation.Interpolate;
            holdRb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            ResetOffsets(); // 重建后把它放回正确位置/朝向
        }
    }



    void TryGrab()
    {
        EnsureHoldAnchor();   // ★ 新增

        if (held) return;

        DetachAnyJointsFromHold();

        GetAimBasis(out var fwd, out var rgt, out var up);
        Vector3 origin = rayOrigin.position;

        if (!Physics.SphereCast(
        origin,
        aimAssistRadius,
        fwd,
        out RaycastHit hit,
        maxGrabDistance,
        grabMask,
        QueryTriggerInteraction.Ignore))
            return;

        Rigidbody rb = hit.rigidbody ? hit.rigidbody : hit.collider.attachedRigidbody;
        if (!rb || rb.isKinematic || rb.mass > maxGrabMass) return;

        // 抓之前就把锚点移到目标处，姿态也先对齐
        curDist = Mathf.Clamp(baseDistance, distanceLimits.x, distanceLimits.y);
        offX = 0f;
        offY = Mathf.Clamp(baseHeight, verticalLimits.x, verticalLimits.y);

        Vector3 want = rayOrigin.position + fwd * curDist + rgt * offX + up * offY;
        // 抓前先把锚点刚体放到“本帧目标位姿”
        holdRb.position = want;                             // 原来用的是 _frameTargetPos（旧值）
        holdRb.rotation = Quaternion.LookRotation(fwd, up); // 原来用的是 _frameTargetRot（旧值）

        spinXDeg = 0f;            // 已有
        spinYDeg = 0f;            // 🆕 新增：水平角也从0开始


        // 稳定一下
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // 抓“质心”能显著减少扭矩
        var fj = rb.gameObject.AddComponent<FixedJoint>();
        fj.autoConfigureConnectedAnchor = false;

        rb.ResetCenterOfMass();          // centerOfMass 是刚体本地坐标
        fj.anchor = rb.centerOfMass;     // 物体端锚点 = 本地质心（不是世界坐标）
        fj.connectedBody = holdRb;       // 连接到 kinematic 的握持刚体
        fj.connectedAnchor = Vector3.zero;
        fj.breakForce = breakForce;
        fj.breakTorque = breakTorque;
        fj.enableCollision = false;
        // 让预处理保持默认开启（更稳）

        joint = fj;
        held = rb;

        heldCols = held.GetComponentsInChildren<Collider>(true);
        TogglePlayerHeldCollision(true);
    }



    void Drop()
    {
        if (!held) return;
        StartCoroutine(DropRoutine());
    }

    IEnumerator DropRoutine()
    {
        yield return new WaitForFixedUpdate();   // 等物理步更稳

        // 先断开连接（立刻生效），再销毁组件（延迟销毁）
        if (joint)
        {
            joint.connectedBody = null;
            Destroy(joint);
            joint = null;
        }

        if (held)
        {
            held.velocity = Vector3.zero;
            held.angularVelocity = Vector3.zero;
            held.collisionDetectionMode = CollisionDetectionMode.Discrete;
            held.drag = savedDrag;
            held.angularDrag = savedAngDrag;
        }

        TogglePlayerHeldCollision(false);        // 恢复玩家↔物体的碰撞（成对）:contentReference[oaicite:7]{index=7}
        held = null;
        heldCols = null;

        // 保险：再扫一遍是否还有残留的“连在 holdRb 的关节”
        DetachAnyJointsFromHold();
    }


    void TogglePlayerHeldCollision(bool ignore)
    {
        if (playerCols == null || heldCols == null) return;

        if (ignore)
        {
            ignoredPairs.Clear();
            foreach (var pc in playerCols)
                foreach (var hc in heldCols)
                    if (pc && hc) { Physics.IgnoreCollision(pc, hc, true); ignoredPairs.Add((pc, hc)); }
        }
        else
        {
            foreach (var (a, b) in ignoredPairs) if (a && b) Physics.IgnoreCollision(a, b, false);
            ignoredPairs.Clear();
        }
    }

    void ResetOffsets()
    {
        curDist = Mathf.Clamp(baseDistance, distanceLimits.x, distanceLimits.y);
        offX = 0f;
        offY = Mathf.Clamp(baseHeight, verticalLimits.x, verticalLimits.y);

        GetAimBasis(out var fwd, out var rgt, out var up);
        holdAnchor.position = rayOrigin.position + fwd * curDist + rgt * offX + up * offY;
        holdAnchor.rotation = Quaternion.LookRotation(fwd, up);
    }

    // === 计算带俯仰的屏幕坐标系（forward/right/up） ===
    void GetAimBasis(out Vector3 fwd, out Vector3 rgt, out Vector3 up)
    {
        Transform t = aimFrom ? aimFrom : (cam ? cam.transform : holdParent);
        Quaternion pitchQ = aimRayWithMouse
            ? Quaternion.AngleAxis(aimPitchDeg, t.right)   // 绕屏幕“右向”俯仰 :contentReference[oaicite:11]{index=11}
            : Quaternion.identity;

        fwd = (pitchQ * t.forward).normalized;             // 俯仰后的前向
        up = (pitchQ * t.up).normalized;                  // 相应的上向
        rgt = Vector3.Cross(fwd, up).normalized;           // 正交化得到右向
    }

    // ✅ 把仍然连到 holdRb 的所有关节都立即断开（防并发残留）
    void DetachAnyJointsFromHold()
    {
        // 先断开我们记录的 joint
        if (joint)
        {
            joint.connectedBody = null;   // 立即断开连接（无需等销毁）:contentReference[oaicite:2]{index=2}
            Destroy(joint);               // 标记销毁（延迟，但已不再联动）:contentReference[oaicite:3]{index=3}
            joint = null;
        }

        if (!holdRb) return;  // ★ 新增：holdRb 被销毁就不再扫描


        // 保险：扫一遍场景里所有 Joint，把还连着 holdRb 的也断开
        var all = FindObjectsOfType<Joint>();
        foreach (var j in all)
        {
            if (j && j.connectedBody == holdRb)
            {
                j.connectedBody = null;   // 立刻断开
                Destroy(j);               // 标记销毁
            }
        }
    }

    // 2) 始终画线（不要再按 InMenu 关掉），并用 SphereCast 提高命中
    void DrawGrabRay()
    {
        if (!_rayLR || !showRayInGame) return;
        _rayLR.enabled = true; // ← 总是开启（就像 Debug.DrawRay）

        GetAimBasis(out var fwdNow, out _, out _);
        Vector3 origin = rayOrigin ? rayOrigin.position : transform.position;

        bool hit = Physics.SphereCast(
            origin,
            aimAssistRadius,
            fwdNow,
            out RaycastHit info,
            maxGrabDistance,
            grabMask,
            QueryTriggerInteraction.Ignore
        );

        Vector3 end = hit ? info.point : origin + fwdNow * maxGrabDistance;

        bool canGrab = false;
        if (hit)
        {
            var rb = info.rigidbody ? info.rigidbody : info.collider.attachedRigidbody;
            canGrab = rb && !rb.isKinematic && rb.mass <= maxGrabMass;
        }

        _rayLR.SetPosition(0, origin);
        _rayLR.SetPosition(1, end);

        var c = canGrab ? rayColorCanGrab : (hit ? rayColorBlocked : rayColorNoHit);
        SetLineColor(c);
    }

    IEnumerator ThrowRoutine(Vector3 dir, float impulseMag)
    {
        if (!held) yield break;

        // 缓存当前被抓刚体
        Rigidbody target = held;

        // —— 断开抓取（不要把速度清零）
        // 改造一下：做一个“投掷版 Drop”，不清零速度只解开关节 & 恢复碰撞
        if (joint)
        {
            joint.connectedBody = null;
            Destroy(joint);
            joint = null;
        }

        // 恢复物体参数（不要清零速度）
        target.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        TogglePlayerHeldCollision(false);
        held = null;
        heldCols = null;

        // 等到下一次 FixedUpdate 再施加冲量，确保上面的解关节已被物理步接收
        yield return new WaitForFixedUpdate(); // :contentReference[oaicite:2]{index=2}

        if (target) // 物体可能在这一帧被销毁
        {
            // 一次性冲量（与质量相关的瞬时速度变化）:contentReference[oaicite:3]{index=3}
            target.AddForce(dir * impulseMag, ForceMode.Impulse);
            // 可选：给一点角动量，让飞行更自然
            // target.AddTorque(Random.insideUnitSphere * 0.5f * impulseMag, ForceMode.Impulse);
        }

        // 防残留：保证握持侧没有遗留 Joint
        DetachAnyJointsFromHold();

        _throwQueued = false;
    }

}
