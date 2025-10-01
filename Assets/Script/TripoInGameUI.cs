using System.Collections;
using System.Collections.Generic;
using System.IO;
using GLTFast;
using TripoForUnity;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TripoInGameUI : MonoBehaviour
{
    [Header("Refs")]
    public TripoRuntimeCore core;          // 拖 TripoCore
    public Transform player;               // 生成时的位置参考
    public CanvasGroup promptPanel;        // PromptPanel 上的 CanvasGroup
    public InputField promptInput;         // 输入框
    public Button generateBtn;             // Generate 按钮
    public Text progressText;              // 顶部进度文字
    public Transform spawnPoint;   // Inspector 拖 SpawnPoint
    public float spawnScale = 1.5f;
    public string grabbableLayer = "Grabbable";   // 新模型自动设到这个层（用于F键拾取）

    bool waiting;      // 正在排队/下载
    float timer;       // 计时器

    string lastPrompt = "";
    public Button closePromptBtn;   // ← 在 Inspector 里把 X 按钮拖进来

    public WarehouseUI warehouseUI;                 // Inspector 里拖进来
    public bool IsPromptOpen => (promptPanel != null) && promptPanel.interactable;

    // 在字段区加：
    bool _addToDBThisTime = false;      // 只有“输入框生成”才会置 true
    string _pendingPromptForDB = null;  // 这次入库要用的 prompt（保证不为空）

    [Header("Explosive Auto-Decorate")]
    public GameObject defaultExplosionVfxPrefab;   // 默认爆炸特效（预制体）
    public AudioClip defaultExplosionSfx;         // 默认爆炸音
    [Range(0f, 1f)] public float defaultExplosionSfxVol = 1f;
    public float defaultExplodeRadius = 6f;
    public float defaultExplodeForce = 20f;
    public float defaultUpwards = 0.5f;
    public LayerMask defaultAffectLayers = ~0;

    // 英文关键词（用词边界，避免 bombastic 命中 bomb）
    static readonly string[] EnExplosive = new[]{
    "bomb","grenade","tnt","dynamite","c4","explosive","landmine","mine",
    "warhead","nuke","nuclear","molotov"
    };

    // 中文关键词（避免单独“雷”，只用明确/组合词）
    static readonly string[] ZhExplosive = new[]{
     "炸弹","手雷","原子弹","核弹","炸药","炸药包","雷管","爆破桶","爆炸桶","起爆器"
    };

    void Start()
    {
        generateBtn.onClick.AddListener(GenerateClicked);
        core.OnDownloadComplete.AddListener(ModelReady);

        // NEW: X 按钮关闭
        if (closePromptBtn) closePromptBtn.onClick.AddListener(() => TogglePrompt(false));


        TogglePrompt(false);               // 确保启动时隐藏
    }

    void Update()
    {
        // NEW: 在主菜单不处理任何输入
        if (GameMode.InMenu)
        {
            if (IsPromptOpen) TogglePrompt(false);
            if (warehouseUI && warehouseUI.IsOpen) warehouseUI.Hide();
            return;
        }

        // NEW: G 键切换。等待生成中就不让打开。
        if (Input.GetKeyDown(KeyCode.G))
        {
            if (!waiting)
            {
                if (warehouseUI && warehouseUI.IsOpen) warehouseUI.Hide();
                if (promptPanel != null && !IsPromptOpen)   // ✅
                {
                    TogglePrompt(true);
                    if (promptInput) { promptInput.text = ""; promptInput.ActivateInputField(); }
                }
            }
        }

        if (waiting)
        {
            timer += Time.deltaTime;
            float pct = Mathf.Clamp01(core.textToModelProgress);
            var prefix = BootstrapHUDLocalization.GeneratingPrefix; // ← 从新脚本取多语言前缀
            if (string.IsNullOrEmpty(prefix)) prefix = "Generating";
            progressText.text = $"{prefix}  {pct * 100f:0}%   |   {timer:0.0}s";

        }
    }

    public void SyncCursor()
    {
        bool anyUIOpen = (promptPanel && promptPanel.interactable)
                       || (warehouseUI && warehouseUI.IsOpen);
        Cursor.visible = anyUIOpen;
        Cursor.lockState = anyUIOpen ? CursorLockMode.None : CursorLockMode.Locked; // 光标锁定/解锁依据是否有面板打开
    }

    /* ---------- UI 切换 ---------- */
    public void TogglePrompt(bool show)
    {
        if (!promptPanel) { SyncCursor(); return; }   // ✅ 面板没了就直接返回
        promptPanel.alpha = show ? 1 : 0;
        promptPanel.interactable = show;
        promptPanel.blocksRaycasts = show;
        SyncCursor();
    }


    /* ---------- 点 Generate ---------- */
    void GenerateClicked()
    {
        string prompt = promptInput.text.Trim();
        if (string.IsNullOrEmpty(prompt)) return;

        core.textPrompt = prompt;
        core.Text_to_Model_func();
        waiting = true; timer = 0;

        TogglePrompt(false);
        progressText.gameObject.SetActive(true);

        lastPrompt = prompt;

        // ✨ 仅“输入框生成”允许把模型写入 DB
        _addToDBThisTime = true;
        _pendingPromptForDB = string.IsNullOrEmpty(lastPrompt)
            ? "Unnamed" : lastPrompt;
    }


    /* ---------- 下载完成 ---------- */
    void ModelReady(string urlOrDir)
    {
        if (string.IsNullOrWhiteSpace(urlOrDir))
        {
            Debug.LogError("Tripo output empty — 可能只返回了另一字段");
            waiting = false;
            progressText.gameObject.SetActive(false);
            return;
        }

        // 传入这次的 prompt
        StartCoroutine(LoadAndSpawn(urlOrDir, lastPrompt));   // <<< 修改
    }



    // 统一解析当前关卡的生成位姿（优先 Router → 再 spawnPoint → 最后玩家前方）
    bool ResolveSpawnPose(out Vector3 pos, out Quaternion rot, out float scale)
    {
        pos = Vector3.zero;
        rot = Quaternion.identity;
        scale = spawnScale;

        // 1) Router（来自 LevelEntry / Portal / 默认Anchor）
        var router = ModelSpawnRouter.Instance ?? FindObjectOfType<ModelSpawnRouter>();
        if (router != null && router.TryGetSpawnPose(out var p, out var r, out var s))
        {
            pos = p; rot = r; scale = s;
            // 可选：把 Inspector 的 spawnPoint 也同步一下，方便别处用
            if (spawnPoint) { spawnPoint.position = p; spawnPoint.rotation = r; }
            return true;
        }

        // 2) Inspector 的 spawnPoint 兜底
        if (spawnPoint)
        {
            pos = spawnPoint.position; rot = spawnPoint.rotation; scale = spawnScale;
            return true;
        }

        // 3) 最后用玩家前上方一个点
        if (player)
        {
            pos = player.position + player.forward * 1.5f + Vector3.up * 0.6f;
            rot = Quaternion.LookRotation(player.forward, Vector3.up);
            scale = spawnScale;
            return true;
        }

        return false;
    }
    IEnumerator LoadAndSpawn(string pathOrUrl)                    // <<< 新增薄包装
    {
        yield return LoadAndSpawn(pathOrUrl, lastPrompt);
    }
    IEnumerator LoadAndSpawn(string pathOrUrl, string decoratePromptMaybe)   // <<< 新签名
    {
        // === 0) 拿到本次要用的生成位姿（这一步修复了你漏赋值的问题） ===
        if (!ResolveSpawnPose(out var spawnPos, out var spawnRot, out var spawnScaleLocal))
        {
            Debug.LogWarning("[TripoInGameUI] No spawn pose resolved, using world origin.");
        }

        // === 1) 判空与缓存键 ===
        if (string.IsNullOrWhiteSpace(pathOrUrl))
        {
            Debug.LogError("Tripo returned empty model path/url");
            waiting = false;
            progressText.gameObject.SetActive(false);
            yield break;
        }
        string cacheKey = pathOrUrl;

        // === 2) 规范URI ===
        string uri;
        string finalLocalPathForDB = null;
        string originalUrlMaybeRemote = null;

        if (pathOrUrl.StartsWith("http", System.StringComparison.OrdinalIgnoreCase))
        {
            originalUrlMaybeRemote = pathOrUrl; // 保存原始远端URL，作为缓存别名
            string savedPath = null;
            yield return DownloadToCache(pathOrUrl, p2 => savedPath = p2);
            if (string.IsNullOrEmpty(savedPath))
            {
                Debug.LogError("Tripo signed URL likely expired. Please re-generate a fresh link.");
                waiting = false;
                progressText.gameObject.SetActive(false);
                yield break;
            }
            uri = "file://" + savedPath.Replace("\\", "/");
            finalLocalPathForDB = savedPath; // ✅ 入库用本地文件路径（非 file:// 前缀）
        }
        else
        {
            string full = Path.GetFullPath(pathOrUrl);
            if (Directory.Exists(full))
            {
                var gltfs = Directory.GetFiles(full, "*.glb");
                if (gltfs.Length == 0) gltfs = Directory.GetFiles(full, "*.gltf");
                if (gltfs.Length == 0)
                {
                    Debug.LogError("No glTF in " + full);
                    waiting = false;
                    progressText.gameObject.SetActive(false);
                    yield break;
                }
                full = gltfs[0];
            }
            uri = "file://" + full.Replace("\\", "/");
            finalLocalPathForDB = full; // ✅ 入库用绝对文件路径
        }

        // === 3) 先创建承载物体并放到“当前激活场景” ===
        var go = new GameObject("TripoModel");
        go.transform.SetPositionAndRotation(spawnPos, spawnRot);
        go.transform.localScale = Vector3.one * spawnScaleLocal;

        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (activeScene.IsValid())
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, activeScene); // 确保实例属于当前关卡（Additive流程必要）。 :contentReference[oaicite:0]{index=0}

        // === 4) glTFast 加载 ===
        var gltf = go.AddComponent<GLTFast.GltfAsset>();
        gltf.Url = uri;

        yield return new WaitUntil(() => gltf.IsDone);
        yield return new WaitUntil(() => go.GetComponentsInChildren<MeshFilter>(true).Length > 0);

        // === 5) 递归设层 ===
        int gLayer = LayerMask.NameToLayer(grabbableLayer);
        if (gLayer != -1) SetLayerRecursively(go, gLayer);

        // === 6) 物理 ===
        VHACDCompoundColliderBuilder.Build(go, maxHullsPerMesh: 32, rigidbodyMass: 300f);

        // === 6.5) ✨ 如 prompt 像“爆炸物” → 自动挂 Explosive + Sphere Trigger
        if (LooksLikeExplosive(decoratePromptMaybe))             
        {
            DecorateAsExplosive(go, decoratePromptMaybe);
        }

        // === 7) 注册缓存 ===
        if (!ModelCache.TryGet(finalLocalPathForDB, out _))
        {
            if (!string.IsNullOrEmpty(originalUrlMaybeRemote))
                ModelCache.RegisterFromInstance(finalLocalPathForDB, go, originalUrlMaybeRemote);
            else
                ModelCache.RegisterFromInstance(finalLocalPathForDB, go);
        }

        // === ✅ 只在“输入框触发”的这一次才入库；仓库点击/缓存补载一律不入库 ===
        if (WarehouseDB.Instance)
        {
            // 把远端URL替换成本地路径（若有）
            if (!string.IsNullOrEmpty(originalUrlMaybeRemote))
                WarehouseDB.Instance.ReplaceUrlEverywhere(originalUrlMaybeRemote, finalLocalPathForDB);

            if (_addToDBThisTime)
            {
                // 防止重复：用归一化后的本地路径判断
                if (!WarehouseDB.Instance.ContainsUrl(finalLocalPathForDB))
                    WarehouseDB.Instance.Add(_pendingPromptForDB ?? "Unnamed", finalLocalPathForDB);
            }
        }
        // 重置本次入库意图标记（无论成功失败都复位）
        _addToDBThisTime = false;
        _pendingPromptForDB = null;
        // === 8) UI 复位 ===
        waiting = false;
        progressText.gameObject.SetActive(false);
        timer = 0f;
        core.textToModelProgress = 0f;
    }



    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform t in obj.transform)
            SetLayerRecursively(t.gameObject, layer);
    }

    // 统一用解析后的位姿来克隆仓库里的模型（避免依赖 spawnPoint 是否被外部更新）
    public void SpawnExisting(string urlOrDir)
    {
        if (!ResolveSpawnPose(out var spawnPos, out var spawnRot, out var spawnScaleLocal))
        {
            spawnPos = Vector3.zero; spawnRot = Quaternion.identity; spawnScaleLocal = spawnScale;
        }

        if (ModelCache.TryGet(urlOrDir, out var prefab))
        {
            var clone = Instantiate(prefab, spawnPos, spawnRot);
            clone.transform.localScale = Vector3.one * spawnScaleLocal;
            clone.SetActive(true);
            StartCoroutine(WarmEnableColliders(clone));
            return;
        }

        Debug.LogWarning($"[CacheMiss] {urlOrDir} 未命中缓存，退回加载协程。");
        StartCoroutine(LoadAndSpawn(urlOrDir));
    }



    IEnumerator DownloadToCache(string remoteUrl, System.Action<string> onDone)
    {
        // 生成缓存目录和文件名
        string dir = Path.Combine(Application.persistentDataPath, "TripoCache");
        Directory.CreateDirectory(dir);

        // 用后缀猜测保存格式，默认 .glb
        string ext = remoteUrl.Contains(".gltf") ? ".gltf" : ".glb";
        string localPath = Path.Combine(dir, "tripo_" + System.Guid.NewGuid().ToString("N") + ext);

        using (var req = UnityWebRequest.Get(remoteUrl))
        {
            // 直接流式写盘，避免占内存
            req.downloadHandler = new DownloadHandlerFile(localPath) { removeFileOnAbort = true };
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                onDone?.Invoke(localPath);
            }
            else
            {
                Debug.LogError($"Tripo model download failed {req.responseCode}: {req.error}");
                onDone?.Invoke(null);
            }
        }
    }

    IEnumerator WarmEnableColliders(GameObject go)
    {
        // 先托管一下刚体，避免物体还没完全有碰撞体就掉落
        var rbs = go.GetComponentsInChildren<Rigidbody>(true);
        foreach (var rb in rbs) rb.isKinematic = true;

        var cols = new List<Collider>(go.GetComponentsInChildren<Collider>(true));
        foreach (var c in cols) c.enabled = false; // 全部先关
        yield return null; // 让一帧过去

        const int perFrame = 12; // 每帧启用 12 个，可按需要调小/调大
        for (int i = 0; i < cols.Count; i++)
        {
            cols[i].enabled = true;
            if ((i % perFrame) == perFrame - 1) yield return null;
        }

        // 全部启用后再恢复刚体
        foreach (var rb in rbs) rb.isKinematic = false;
    }

    // ④ 提供一个在“返回主菜单”时可安全调用的隐藏方法
    public void SafeHideAllUI()
    {
        if (promptPanel)
        {
            promptPanel.alpha = 0f;
            promptPanel.interactable = false;
            promptPanel.blocksRaycasts = false;
        }
        warehouseUI?.Hide();
        waiting = false;
        progressText?.gameObject.SetActive(false);
    }

    // ⑤ OnDestroy 里解绑事件（防外部再调）
    void OnDestroy()
    {
        if (core) core.OnDownloadComplete.RemoveListener(ModelReady);
    }

    static bool LooksLikeExplosive(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt)) return false;
        string p = prompt.ToLowerInvariant();

        // 英文：用“空格/标点边界”简化匹配（你也可以改用正则 \b 进一步严格）
        foreach (var w in EnExplosive)
        {
            if (p.Contains(w)) return true;
        }

        // 中文：直接子串包含（已剔除歧义“雷”单字）
        foreach (var w in ZhExplosive)
        {
            if (p.Contains(w)) return true;
        }

        return false;
    }

    // 计算模型的大致半径（以所有 Renderer 的包围盒为参考）
    static float ComputeApproxRadius(GameObject root, float pad = 1.0f)
    {
        var rends = root.GetComponentsInChildren<Renderer>(true);
        if (rends == null || rends.Length == 0) return 1.0f;

        Bounds b = new Bounds(rends[0].bounds.center, Vector3.zero);
        for (int i = 0; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        // 取最大半径（包围盒对角的一半），再乘一个 padding 系数
        float r = b.extents.magnitude;
        return Mathf.Max(0.2f, r * pad);
    }

    static T AddOrGet<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c ? c : go.AddComponent<T>();
    }

    // 给 root 挂上 Explosive + Sphere Trigger（父物体上）
    void DecorateAsExplosive(GameObject root, string promptUsed)
    {
        if (!root) return;

        // 1) 父节点 Rigidbody（如果 VHACD 已经加在父上就直接复用；否则补一个）
        var rb = root.GetComponent<Rigidbody>();
        if (!rb) rb = root.AddComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.mass = Mathf.Max(1f, rb.mass);

        // 2) 父节点触发器（用于 OnTriggerEnter 触发爆炸）
        var sc = root.GetComponent<SphereCollider>();
        if (!sc) sc = root.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = ComputeApproxRadius(root, 0.6f); // 半径可按需要调

        // 3) Explosive 组件 + 参数填充
        var ex = AddOrGet<Explosive>(root);
        ex.radius = defaultExplodeRadius;
        ex.force = defaultExplodeForce;
        ex.upwardsModifier = defaultUpwards;
        ex.affectLayers = defaultAffectLayers;
        ex.explosionVfxPrefab = defaultExplosionVfxPrefab;
        ex.explosionSfx = defaultExplosionSfx;
        ex.sfxVolume = defaultExplosionSfxVol;
        ex.explodeOnTriggerToo = true; // 大触发器命中即可
        ex.minImpactSpeed = 0.2f;
        // 可选：ex.autoExplodeAfter = 0f;

        // 你也可以在这里加一个小小的“标记组件/脚本化对象”记住这个 prompt
        // 但由于我们把装配过的实例丢进了 ModelCache，仓库再取就是“带爆炸能力”的版本。
    }
    public void SpawnExistingWithPrompt(string urlOrDir, string promptMaybeNull)
    {
        // 先用你原来的克隆方式
        if (!ResolveSpawnPose(out var spawnPos, out var spawnRot, out var spawnScaleLocal))
        {
            spawnPos = Vector3.zero; spawnRot = Quaternion.identity; spawnScaleLocal = spawnScale;
        }

        if (ModelCache.TryGet(urlOrDir, out var prefab))
        {
            var clone = Instantiate(prefab, spawnPos, spawnRot);
            clone.transform.localScale = Vector3.one * spawnScaleLocal;
            clone.SetActive(true);
            StartCoroutine(WarmEnableColliders(clone));

            // 保险：如果旧缓存里没有 Explosive，但 prompt 看起来是爆炸物，就补一次
            if (LooksLikeExplosive(promptMaybeNull) && clone.GetComponentInChildren<Explosive>(true) == null)
            {
                DecorateAsExplosive(clone, promptMaybeNull);
            }
            return;
        }

        // 没命中缓存就走原来的加载流程
        StartCoroutine(LoadAndSpawn(urlOrDir, promptMaybeNull));     // <<< 修改

    }

}
