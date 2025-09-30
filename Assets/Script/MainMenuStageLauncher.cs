// MainMenuStageLauncher.cs
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MainMenuStageLauncher : MonoBehaviour
{
    [Header("Target Level")]
    public string levelSceneName;      // 目标关卡
    public string targetAnchorId;      // 进入后选用的 Anchor（可空）

    [Header("Bootstrap")]
    public string bootstrapSceneName = "Bootstrap"; // 你常驻的场景名
    public bool unloadMenuSceneAfterLaunch = true;  // 进关后卸载 MainMenu

    Button _btn;

    void Awake()
    {
        _btn = GetComponent<Button>();
        _btn.onClick.AddListener(OnClick);
    }
    void OnDestroy()
    {
        if (_btn) _btn.onClick.RemoveListener(OnClick);
    }

    void OnClick()
    {
        StartCoroutine(LaunchRoutine());
    }

    IEnumerator LaunchRoutine()
    {
        // 1) 确保 Bootstrap 已加载（里面才有 LevelManager / Router / 玩家/相机 等常驻）
        var boot = SceneManager.GetSceneByName(bootstrapSceneName);
        if (!boot.isLoaded)
        {
            var op = SceneManager.LoadSceneAsync(bootstrapSceneName, LoadSceneMode.Additive);
            while (!op.isDone) yield return null;
            yield return null; // 多等一帧，保证 Bootstrap 里的单例 Awake/OnEnable 完成
        }

        // 2) 通过 LevelManager 进关（会 Additive 加载目标关卡并 SetActiveScene、移动玩家到 PlayerSpawn）
        if (LevelManager.Instance == null)
        {
            Debug.LogError("[MainMenuStageLauncher] LevelManager not found after loading Bootstrap.");
            yield break;
        }
        // ✅ 进关前恢复“游戏模式”（启用拾取/暂停等脚本、锁鼠标）
        GameMode.SetInMenu(false);

        LevelManager.Instance.EnterLevel(levelSceneName, targetAnchorId);

        // ★ 新增：发车后立刻复位一遍全局输入/光标/时间缩放
        GameplayFocusReset.Apply();

        // 3) 可选：卸载菜单场景，避免占内存
        if (unloadMenuSceneAfterLaunch)
        {
            var menuScene = gameObject.scene;
            if (menuScene.IsValid())
                SceneManager.UnloadSceneAsync(menuScene.name);
        }

        var hud = FindObjectOfType<BootstrapHUDLocalization>(true);
        if (hud) hud.SetHintVisible(hud.showOnStart);

    }
}
