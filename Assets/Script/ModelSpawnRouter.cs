using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ModelSpawnRouter : MonoBehaviour
{
    /* ---------- Singleton ---------- */
    public static ModelSpawnRouter Instance { get; private set; }

    [Header("Fallback")]
    [Tooltip("��û�д�����ָ��ʱ�������ñ��ֶΣ��������ȡ LevelEntry���ٲ��о��ҳ�����ĵ�һ�� Anchor��")]
    public string defaultAnchorId = "";

    [Tooltip("�Ҳ����κ� Anchor ʱ���Ƿ�ʹ�ñ�������Ϊ���ն��׵����ɵ�")]
    public bool useSelfAsFinalFallback = true;

    Transform _currentAnchor;   // �������ĵ�ǰê��
    ModelSpawnAnchor _currentAnchorComp;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded; // �³����ɹ����غ�ص�
        TryResolveAndApplyAnchor();                // ����ʱ�Ƚ�һ��
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode mode)
    {
        // ÿ�μ��أ����� Additive�������Խ���һ��
        TryResolveAndApplyAnchor();
    }

    /* ---------- �ⲿ API ---------- */

    /// <summary>�ڡ���ǰ�Ѽ��صĳ������ϡ��У��� ID ѡ��ê�㲢Ӧ�á�</summary>
    public bool SelectAnchorById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            // �����գ���ͬ���� Router ���а�Ĭ�Ϲ������
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

        Debug.LogWarning($"[ModelSpawnRouter] δ�ҵ� AnchorId = {id} ��ê�㡣");
        return false;
    }

    /// <summary>��ʹ�÷����� TripoInGameUI����ȡ��ǰ����λ�ˡ�</summary>
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

    /* ---------- �ڲ�������/Ӧ�� ---------- */

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

        // �� ��ֻ�� Active Scene ��
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

        // Active Scene �ﻹû��ê����ȡ�ó�����һ��
        if (!anchorT && anchorsActive.Length > 0)
            anchorT = anchorsActive[0].transform;

        // ��Ȼû�У��������������Ѽ��س�����
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
            ApplyToConsumers(anchorT); // �� spawnPoint ָ�� TripoInGameUI ��������
        }
        else
        {
            Debug.LogWarning("[ModelSpawnRouter] û���ҵ��κο��õ� Anchor��spawnPoint ������Ϊ�ա�");
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
        // �ѱ������������ TripoInGameUI �� spawnPoint ��ָ��ê��
        var allTripo = FindObjectsOfType<TripoInGameUI>(true);
        foreach (var t in allTripo) t.spawnPoint = anchor;
    }

    /* ---------- �������Ӵ��봥�����ز�ָ��Ŀ��ê�� ---------- */
    public void TeleportTo(string sceneName, string anchorIdInTargetScene)
    {
        SpawnTicket.NextAnchorId = anchorIdInTargetScene;   // ���ϡ���һ��ê�㡱
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
