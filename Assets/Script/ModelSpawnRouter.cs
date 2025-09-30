using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ModelSpawnRouter : MonoBehaviour
{
    /* ---------- Singleton ---------- */
    public static ModelSpawnRouter Instance { get; private set; }

    [Header("Fallback")]
    [Tooltip("当没有传送门指定时，优先用本字段；留空则读取 LevelEntry；再不行就找场景里的第一个 Anchor。")]
    public string defaultAnchorId = "";

    [Tooltip("找不到任何 Anchor 时，是否使用本物体作为最终兜底的生成点")]
    public bool useSelfAsFinalFallback = true;

    Transform _currentAnchor;   // 解析到的当前锚点
    ModelSpawnAnchor _currentAnchorComp;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded; // 新场景成功加载后回调
        TryResolveAndApplyAnchor();                // 启用时先解一次
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode mode)
    {
        // 每次加载（包含 Additive）都尝试解析一次
        TryResolveAndApplyAnchor();
    }

    /* ---------- 外部 API ---------- */

    /// <summary>在“当前已加载的场景集合”中，按 ID 选择锚点并应用。</summary>
    public bool SelectAnchorById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            // 允许传空：等同于让 Router 自行按默认规则解析
            TryResolveAndApplyAnchor();
            return _currentAnchor != null;
        }

        var anchors = FindObjectsOfType<ModelSpawnAnchor>(true);
        var hit = anchors.FirstOrDefault(a =>
            string.Equals(a.anchorId, id, System.StringComparison.OrdinalIgnoreCase));

        if (hit)
        {
            SetCurrentAnchor(hit.transform, hit);
            return true;
        }

        Debug.LogWarning($"[ModelSpawnRouter] 未找到 AnchorId = {id} 的锚点。");
        return false;
    }

    /// <summary>给使用方（如 TripoInGameUI）读取当前生成位姿。</summary>
    public bool TryGetSpawnPose(out Vector3 pos, out Quaternion rot, out float scale)
    {
        if (_currentAnchor)
        {
            pos = _currentAnchor.position;
            rot = _currentAnchor.rotation;
            scale = _currentAnchorComp ? _currentAnchorComp.spawnScale : 1f;
            return true;
        }

        pos = Vector3.zero; rot = Quaternion.identity; scale = 1f;
        return false;
    }

    /* ---------- 内部：解析/应用 ---------- */

    void TryResolveAndApplyAnchor()
    {
        string targetId = SpawnTicket.Consume();
        if (string.IsNullOrWhiteSpace(targetId))
        {
            if (!string.IsNullOrWhiteSpace(defaultAnchorId)) targetId = defaultAnchorId;
            else
            {
                var entry = FindObjectOfType<LevelEntry>();
                if (entry && !string.IsNullOrWhiteSpace(entry.defaultAnchorId))
                    targetId = entry.defaultAnchorId;
            }
        }

        // ★ 先只在 Active Scene 找
        var active = SceneManager.GetActiveScene();
        var anchorsActive = FindObjectsOfType<ModelSpawnAnchor>(true)
            .Where(a => a.gameObject.scene == active)
            .ToArray();

        Transform anchorT = null;

        if (!string.IsNullOrWhiteSpace(targetId))
        {
            anchorT = anchorsActive
                .FirstOrDefault(a => string.Equals(a.anchorId, targetId, System.StringComparison.OrdinalIgnoreCase))
                ?.transform;
        }

        // Active Scene 里还没定锚？就取该场景第一个
        if (!anchorT && anchorsActive.Length > 0)
            anchorT = anchorsActive[0].transform;

        // 仍然没有，再扩到“所有已加载场景”
        if (!anchorT)
        {
            var anchorsAll = FindObjectsOfType<ModelSpawnAnchor>(true);
            if (!string.IsNullOrWhiteSpace(targetId))
                anchorT = anchorsAll
                    .FirstOrDefault(a => string.Equals(a.anchorId, targetId, System.StringComparison.OrdinalIgnoreCase))
                    ?.transform;
            if (!anchorT && anchorsAll.Length > 0)
                anchorT = anchorsAll[0].transform;
        }

        if (!anchorT && useSelfAsFinalFallback) anchorT = transform;

        if (anchorT)
        {
            _currentAnchor = anchorT;
            ApplyToConsumers(anchorT); // 把 spawnPoint 指给 TripoInGameUI 等消费者
        }
        else
        {
            Debug.LogWarning("[ModelSpawnRouter] 没有找到任何可用的 Anchor，spawnPoint 将保持为空。");
        }
    }

    void SetCurrentAnchor(Transform t, ModelSpawnAnchor comp)
    {
        _currentAnchor = t;
        _currentAnchorComp = comp;
        ApplyToConsumers(t);
    }

    void ApplyToConsumers(Transform anchor)
    {
        // 把本场景里的所有 TripoInGameUI 的 spawnPoint 都指向锚点
        var allTripo = FindObjectsOfType<TripoInGameUI>(true);
        foreach (var t in allTripo) t.spawnPoint = anchor;
    }

    /* ---------- 便利：从代码触发换关并指定目标锚点 ---------- */
    public void TeleportTo(string sceneName, string anchorIdInTargetScene)
    {
        SpawnTicket.NextAnchorId = anchorIdInTargetScene;   // 带上“下一次锚点”
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
