using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Player")]
    public GameObject player;
    public string playerTag = "Player";

    [Header("Load/Unload")]
    public bool unloadPrevious = true;
    public float postActivateDelay = 0.1f;

    string _currentLevelSceneName;

    public void ClearCurrentLevel() { _currentLevelSceneName = null; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    GameObject GetPlayer()
    {
        if (player) return player;
        var go = GameObject.FindGameObjectWithTag(playerTag);
        if (go) player = go;
        return player;
    }

    public void EnterLevel(string sceneName, string spawnAnchorId = "")
    {
        StartCoroutine(EnterLevelRoutine(sceneName, spawnAnchorId));
    }

    IEnumerator EnterLevelRoutine(string sceneName, string spawnAnchorId)
    {
        // 1) 异步 Additive 加载
        var load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (load == null) yield break;
        while (!load.isDone) yield return null;

        // 2) 设为 Active Scene（实例化/查找等以后都以它为上下文）
        var scene = SceneManager.GetSceneByName(sceneName);
        SceneManager.SetActiveScene(scene);

        // —— 新增：如果根物体全是关着的，强制激活（防上一次残留逻辑把根禁用了）——
        var roots = scene.GetRootGameObjects();      // 取根物体
        bool anyActive = false;
        for (int i = 0; i < roots.Length; i++) if (roots[i].activeInHierarchy) { anyActive = true; break; }
        if (!anyActive) for (int i = 0; i < roots.Length; i++) roots[i].SetActive(true);


        // 3)（可选）告知模型生成路由使用哪个 Anchor
        var router = FindObjectOfType<ModelSpawnRouter>(true);
        if (router && !string.IsNullOrWhiteSpace(spawnAnchorId))
            router.SelectAnchorById(spawnAnchorId);

        // 4) 给新场景里的对象一个初始化的机会
        if (postActivateDelay > 0f) yield return new WaitForSeconds(postActivateDelay);

        // 5) 把玩家移入新场景 + 只在该场景里找 PlayerSpawn
        var p = GetPlayer();
        if (p)
        {
            // 把玩家 GameObject 归属到新场景（官方API）
            // SceneManager.MoveGameObjectToScene(p, scene);  // :contentReference[oaicite:2]{index=2}

            // 只在【scene】里找 PlayerSpawn：遍历该场景的根物体
            PlayerSpawn spawn = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                spawn = root.GetComponentInChildren<PlayerSpawn>(true);
                if (spawn) break;
            }
            // （思路来自 Unity 官方/社区：先拿到 Scene，再用 GetRootGameObjects 再往下找组件） :contentReference[oaicite:3]{index=3}

            // 传送角色（建议暂时禁用 CharacterController 再改位姿）
            var cc = p.GetComponent<CharacterController>();
            if (cc) cc.enabled = false;

            if (spawn)
                p.transform.SetPositionAndRotation(spawn.transform.position, spawn.transform.rotation);
            else
                Debug.LogWarning($"[LevelManager] 没在场景 {sceneName} 里找到 PlayerSpawn，保留当前位置。");

            if (cc) cc.enabled = true;
        }

        // 6)（可选）卸载上一关
        // LevelManager.cs 里，卸载旧关卡前做稳妥判断（只贴这段）
        var old = UnityEngine.SceneManagement.SceneManager.GetSceneByName(_currentLevelSceneName);
        if (old.IsValid() && old.isLoaded)                          // ✅ 只有已加载的场景才卸
        {
            var unload = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(old);
            while (unload != null && !unload.isDone) yield return null;
        }

        _currentLevelSceneName = sceneName; // 放到最后记录
    }
}
