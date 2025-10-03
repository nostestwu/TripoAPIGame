using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI; // ★ 新增：uGUI

public class RayPickupController : MonoBehaviour
{
    [Header("Refs")]
    public Transform rayOrigin;     // 射线起点与朝向 -> 设为 Player
    public Transform holdParent;    // 参考系 -> 设为 Player
    public Camera cam;              // 用相机坐标解释“屏幕左右/上下/前后" 

    [Header("Raycast")]
    public float maxGrabDistance = 3f;
    public LayerMask grabMask = ~0;    // 建议只勾 "Grabbable"
    public bool drawDebugRay = false;

    [Header("Aim Dot (point instead of line)")]
    public bool showAimDot = true;
    public float aimAssistRadius = 0.08f;    // SphereCast 半径 = 命中“容差”
    public float dotWorldSize = 0.15f;       // 点的世界尺度
    public Color rayColorNoHit = new Color(0.8f, 0.8f, 0.8f, 0.7f);
    public Color rayColorCanGrab = new Color(0.2f, 1f, 0.2f, 0.9f);
    public Color rayColorBlocked = new Color(1f, 0.3f, 0.3f, 0.9f);

    Transform _aimDot;
    Material _aimDotMat;
    Vector3 _aimEndWorld;   // ★ 新增：末端世界点缓存
    bool _aimHasHit, _aimCanGrab;

    [Header("Hold base")]
    public float baseDistance = 2f;
    public float baseHeight = 1f;
    public float smoothing = 18f;
    public float breakForce = 1e8f, breakTorque = 1e8f;
    public float maxGrabMass = 200f;

    [Header("Mouse control (offset around screen axes)")]
    public float mouseXSensitivity = 0.02f;
    public float mouseYSensitivity = 0.02f;
    public float scrollSensitivity = 1.0f;
    public Vector2 lateralLimits = new Vector2(-1.2f, 1.2f);
    public Vector2 verticalLimits = new Vector2(0.2f, 2.0f);
    public Vector2 distanceLimits = new Vector2(0.5f, 3.5f);
    public bool invertHorizontal = false;

    [Header("Aim (tilt the ray with mouse Y)")]
    public bool aimRayWithMouse = true;
    public float aimPitchSensitivity = 1.5f;
    public Vector2 aimPitchLimits = new Vector2(-60f, 60f);
    float aimPitchDeg = 0f;

    [Header("Rotate while holding")]
    public KeyCode rotateModifier = KeyCode.LeftShift;
    public float wheelRotDegrees = 30f;
    public float rotSmoothing = 18f;
    public float heldLinearDrag = 2f, heldAngularDrag = 5f;

    [Header("Throw (E to charge & release)")]
    public KeyCode throwKey = KeyCode.E;
    public float minImpulse = 4f;
    public float maxImpulse = 20f;
    public float maxChargeTime = 1.2f;
    public AnimationCurve chargeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float launchUpBias = 0.0f; // 0=纯前向；0.1~0.2略抬

    float _chargeT = 0f;
    bool _charging = false;
    bool _throwQueued = false;

    // 旋转累积
    float spinXDeg = 0f;
    public float buttonYawSpeed = 120f;
    float spinYDeg = 0f;

    float savedDrag, savedAngDrag;

    // 每帧目标位姿（Update 计算，FixedUpdate 使用）
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

    [Header("UI (Throw Panel)")]
    public GameObject throwPanel;      // 仅在抓起/蓄力时显示
    public Slider chargeSlider;        // 竖向蓄力条（0~1）
    public Image aimArrowImage;        // 屏幕中心箭头（RectTransform 朝向旋转）
    public float arrowMinAlpha = 0.25f;
    public float arrowMaxAlpha = 1.0f;
    public float arrowHideIfBehind = 0.0f; // 当目标在相机后方(<=0)时隐藏

    void Awake()
    {
        // 自动拾取摄像机
        if (cam == null)
        {
            cam = Camera.main;
        }
        if (cam == null)
        {
            Camera[] cams = FindObjectsOfType<Camera>();
            foreach (var c in cams)
            {
                if (c.enabled)
                {
                    cam = c;
                    break;
                }
            }
        }

        Debug.Log($"[RayPickup] Awake: cam = {cam}");

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
        if (cam == null)
        {
            cam = Camera.main;
        }
        Debug.Log($"[RayPickup] Start: cam = {cam}");

        // 锚点设置
        holdAnchor = new GameObject("HoldPoint").transform;
        holdAnchor.SetParent(holdParent ? holdParent : rayOrigin, false);

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

        EnsureAimDot();
        if (throwPanel) throwPanel.SetActive(false);

        if (chargeSlider)
        {
            chargeSlider.minValue = 0f;
            chargeSlider.maxValue = 1f;
            chargeSlider.value = 0f;
            chargeSlider.interactable = false;
        }
        if (aimArrowImage)
        {
            var c = aimArrowImage.color;
            c.a = 0f;
            aimArrowImage.color = c;
        }
    }

    void Update()
    {
        if (cam == null)
        {
            cam = Camera.main;
            Debug.Log($"[RayPickup] Update: cam was null, now set to Camera.main = {cam}");
        }

        // 你原来的 Update 逻辑继续…
        // 例如：Debug 输出一下 _frameTargetPos, ray origin, etc.
        Debug.DrawRay(rayOrigin.position, GetForwardDir() * maxGrabDistance, Color.green);
        // F：抓或放
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (held) Drop();
            else TryGrab();
        }

        // 俯仰：鼠标 Y 控制 ray 抬/压
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

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.0001f)
            {
                spinXDeg += scroll * wheelRotDegrees;
                if (spinXDeg > 10000f || spinXDeg < -10000f) spinXDeg %= 360f;
            }
            if (Input.GetMouseButton(0)) { spinYDeg += buttonYawSpeed * Time.deltaTime; }
            if (Input.GetMouseButton(1)) { spinYDeg -= buttonYawSpeed * Time.deltaTime; }
            if (spinYDeg > 10000f || spinYDeg < -10000f) spinYDeg %= 360f;
        }

        // 仅在“抓住物体”时才允许投掷
        if (held)
        {
            if (Input.GetKeyDown(throwKey))
            {
                _charging = true; _throwQueued = false; _chargeT = 0f;
            }
            if (_charging && Input.GetKey(throwKey))
            {
                _chargeT = Mathf.Clamp01(_chargeT + Time.deltaTime / maxChargeTime);
            }
            if (_charging && Input.GetKeyUp(throwKey) && !_throwQueued)
            {
                _charging = false;
                _throwQueued = true;

                float curve = (chargeCurve != null) ? chargeCurve.Evaluate(_chargeT) : _chargeT;
                float impulseMag = Mathf.Lerp(minImpulse, maxImpulse, curve);

                GetAimBasis(out var fwd, out _, out var up);
                Vector3 dir = (fwd + up * launchUpBias).normalized;

                StartCoroutine(ThrowRoutine(dir, impulseMag));
            }
        }
        else
        {
            _charging = false;
            _chargeT = 0f;
        }

        if (drawDebugRay)
        {
            GetAimBasis(out var fwd, out _, out _);
            Debug.DrawRay(rayOrigin.position, fwd * maxGrabDistance, Color.yellow, 0, false);
        }

        // 目标位姿（握持点）
        GetAimBasis(out var fwdNow, out var rgtNow, out var upNow);
        _frameTargetPos = rayOrigin.position + fwdNow * curDist + rgtNow * offX + upNow * offY;

        Quaternion baseRotNow = Quaternion.LookRotation(fwdNow, upNow);
        _frameTargetRot = baseRotNow
                        * Quaternion.AngleAxis(spinYDeg, Vector3.up)
                        * Quaternion.AngleAxis(spinXDeg, Vector3.right);
        _frameTargetRot = _frameTargetRot.normalized;

        if (holdAnchor)
        {
            holdAnchor.position = _frameTargetPos;
            holdAnchor.rotation = _frameTargetRot;
        }

        // 世界空间末端点（决定颜色 & 供箭头指向）
        DrawAimDot();

        // 刷新 UI（Panel 显示、蓄力条、屏幕箭头）
        UpdateThrowUI();
    }
    Vector3 GetForwardDir()
    {
        GetAimBasis(out var fwd, out _, out _);
        return fwd;
    }
    void FixedUpdate()
    {
        if (!holdRb) return;

        float kPos = held ? 1f : (1f - Mathf.Exp(-smoothing * Time.fixedDeltaTime));
        Vector3 newPos = Vector3.Lerp(holdRb.position, _frameTargetPos, kPos);
        holdRb.MovePosition(newPos);

        float kRot = 1f - Mathf.Exp(-rotSmoothing * Time.fixedDeltaTime);
        Quaternion smoothed = Quaternion.Slerp(holdRb.rotation, _frameTargetRot, kRot);
        smoothed = smoothed.normalized;

        holdRb.MoveRotation(smoothed);

        if (held) held.angularVelocity = Vector3.zero;
    }

    // —— UI 刷新 —— 
    void UpdateThrowUI()
    {
        // 避免命名冲突：不要再叫 showPanel
        bool shouldShowThrowUI = held || _charging;

        if (throwPanel && throwPanel.activeSelf != shouldShowThrowUI)
            throwPanel.SetActive(shouldShowThrowUI);

        if (chargeSlider)
            chargeSlider.value = (_charging ? _chargeT : 0f);

        // 箭头始终放屏幕中心；只根据“俯仰”在 X 轴上倾斜
        if (aimArrowImage && cam)
        {
            // 仍然用末端点判断是否在相机前方（z<=0 隐藏）
            Vector3 sp = cam.WorldToScreenPoint(_aimEndWorld);
            bool visible = sp.z > arrowHideIfBehind;

            if (!visible || !shouldShowThrowUI)
            {
                var c0 = aimArrowImage.color;
                c0.a = 0f;
                aimArrowImage.color = c0;

                // 重置为正向朝上（可选）
                aimArrowImage.rectTransform.localRotation = Quaternion.identity;
            }
            else
            {
                // 只绕 X 轴旋转：用当前瞄准的俯仰角（你的 aimPitchDeg）
                // 向上看箭头向上“翘”，向下看则下“压”
                float pitchDeg = aimPitchDeg;              // [-60,60] 取决于你的限制
                float baseX = 90f; // 你的箭头初始朝上的偏移
                aimArrowImage.rectTransform.localRotation = Quaternion.Euler(baseX + pitchDeg, 0f, 0f);
                // 透明度：可用“蓄力”增强反馈（也可按距离/固定常量）
                float a = Mathf.Lerp(arrowMinAlpha, arrowMaxAlpha, _charging ? _chargeT : 0f);
                var c = aimArrowImage.color;
                c.a = a;
                aimArrowImage.color = c;
            }
        }
    }


    // —— 创建一个“无碰撞”的小球作为瞄准点 —— 
    void EnsureAimDot()
    {
        if (!showAimDot || _aimDot) return;

        var go = new GameObject("AimDot");
        go.transform.SetParent(null, true); // 世界物体

        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        // 内置球网格（Unity 自带资源名在不同版本可能为 Sphere.fbx/Sphere)：
        mf.sharedMesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");

        Shader s = Shader.Find("Universal Render Pipeline/Unlit");
        if (!s) s = Shader.Find("HDRP/Unlit");
        if (!s) s = Shader.Find("Unlit/Color");
        if (!s) s = Shader.Find("Sprites/Default");
        _aimDotMat = new Material(s);
        mr.sharedMaterial = _aimDotMat;

        _aimDot = go.transform;
        _aimDot.localScale = Vector3.one * dotWorldSize;
        _aimDot.gameObject.SetActive(true);
    }

    void SetDotColor(Color c)
    {
        if (_aimDotMat == null) return;
        if (_aimDotMat.HasProperty("_BaseColor")) _aimDotMat.SetColor("_BaseColor", c);
        else if (_aimDotMat.HasProperty("_Color")) _aimDotMat.SetColor("_Color", c);
    }

    // —— 用 SphereCast 求“末端点”，把小球放到那里，并按命中状态换色 —— 
    void DrawAimDot()
    {
        if (!showAimDot) return;

        if (_aimDot == null)
        {
            // 尝试重建
            EnsureAimDot();
            if (_aimDot == null)
            {
                Debug.LogWarning("[RayPickup] DrawAimDot: _aimDot is null even after EnsureAimDot");
                return;
            }
        }

        // 原来逻辑…
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

        _aimEndWorld = hit ? info.point : origin + fwdNow * maxGrabDistance;
        _aimHasHit = hit;

        bool canGrab = false;
        if (hit)
        {
            var rb = info.rigidbody ? info.rigidbody : info.collider.attachedRigidbody;
            canGrab = rb && !rb.isKinematic && rb.mass <= maxGrabMass;
        }
        _aimCanGrab = canGrab;

        _aimDot.position = _aimEndWorld;
        _aimDot.localScale = Vector3.one * dotWorldSize;

        var c = canGrab ? rayColorCanGrab : (hit ? rayColorBlocked : rayColorNoHit);
        SetDotColor(c);
    }


    // ★ 在场景卸载后若锚点/刚体被销毁，自动重建
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
            ResetOffsets();
        }
    }

    void TryGrab()
    {
        Debug.Log("[RayPickup] TryGrab attempt");
        EnsureHoldAnchor();
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

        // 抓之前把锚点移到目标处，姿态对齐
        curDist = Mathf.Clamp(baseDistance, distanceLimits.x, distanceLimits.y);
        offX = 0f;
        offY = Mathf.Clamp(baseHeight, verticalLimits.x, verticalLimits.y);

        Vector3 want = rayOrigin.position + fwd * curDist + rgt * offX + up * offY;
        holdRb.position = want;
        holdRb.rotation = Quaternion.LookRotation(fwd, up);

        spinXDeg = 0f;
        spinYDeg = 0f;

        // 稳定一下
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // 记录/提高阻尼（稳）
        savedDrag = rb.drag;
        savedAngDrag = rb.angularDrag;
        rb.drag = heldLinearDrag;
        rb.angularDrag = heldAngularDrag;

        // 抓“质心”减少扭矩
        var fj = rb.gameObject.AddComponent<FixedJoint>();
        fj.autoConfigureConnectedAnchor = false;
        rb.ResetCenterOfMass();
        fj.anchor = rb.centerOfMass;
        fj.connectedBody = holdRb;
        fj.connectedAnchor = Vector3.zero;
        fj.breakForce = breakForce;
        fj.breakTorque = breakTorque;
        fj.enableCollision = false;

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
        yield return new WaitForFixedUpdate();

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

        TogglePlayerHeldCollision(false);
        held = null;
        heldCols = null;

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
            ? Quaternion.AngleAxis(aimPitchDeg, t.right)
            : Quaternion.identity;

        fwd = (pitchQ * t.forward).normalized;   // 俯仰后的前向
        up = (pitchQ * t.up).normalized;         // 相应的上向
        rgt = Vector3.Cross(fwd, up).normalized; // 正交化得到右向
    }

    // ✅ 把仍然连到 holdRb 的所有关节都立即断开（防并发残留）
    void DetachAnyJointsFromHold()
    {
        if (joint)
        {
            joint.connectedBody = null;
            Destroy(joint);
            joint = null;
        }
        if (!holdRb) return;

        var all = FindObjectsOfType<Joint>();
        foreach (var j in all)
        {
            if (j && j.connectedBody == holdRb)
            {
                j.connectedBody = null;
                Destroy(j);
            }
        }
    }

    IEnumerator ThrowRoutine(Vector3 dir, float impulseMag)
    {
        if (!held) yield break;

        Rigidbody target = held;

        if (joint)
        {
            joint.connectedBody = null;
            Destroy(joint);
            joint = null;
        }

        target.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        TogglePlayerHeldCollision(false);
        held = null;
        heldCols = null;

        // 在下一个物理步里施加冲量（更稳定）
        yield return new WaitForFixedUpdate();

        if (target)
        {
            target.AddForce(dir * impulseMag, ForceMode.Impulse);

            // ★ 新增：如果这个被扔出的刚体带 Explosive，则武装它
            var explosive = target.GetComponent<Explosive>();
            if (explosive)
            {
                explosive.OnThrown();
                Debug.Log($"[Explosive] Armed by throw: {target.name}");
            }
        }

        DetachAnyJointsFromHold();
        _throwQueued = false;

        // 投出后，UI 立即隐藏
        if (throwPanel) throwPanel.SetActive(false);
        if (chargeSlider) chargeSlider.value = 0f;
        if (aimArrowImage)
        {
            var c = aimArrowImage.color; c.a = 0f; aimArrowImage.color = c;
        }
    }
}
