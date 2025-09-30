// PauseMenu.cs  —— 放在 Bootstrap 场景的一个空物体上
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    public CanvasGroup panel;       // 指向 PausePanel（带 CanvasGroup）
    public Button homeButton;       // “Return to Menu / 返回主页”
    public Button closeButton;      // 你的 X 按钮
    public Text homeLabel;          // 按钮上的 Text（或改为 TMP_Text）


    [Header("Localization")]
    public string homeLabelEN = "Return to Menu";
    public string homeLabelZH = "返回主页";

    [Header("Scenes")]
    public string bootstrapSceneName = "Bootstrap";
    public string mainMenuSceneName = "MainMenu";

    [Header("Input")]
    public KeyCode toggleKey = KeyCode.Escape;

    bool _open;

    void Start()
    {
        // 初始隐藏
        Toggle(false);

        // 文案
        UpdateHomeLabel();

        // 事件
        if (homeButton) homeButton.onClick.AddListener(() => StartCoroutine(ReturnToMenuRoutine()));
        if (closeButton) closeButton.onClick.AddListener(() => Toggle(false));
    }

    void OnDestroy()
    {
        if (homeButton) homeButton.onClick.RemoveAllListeners();
        if (closeButton) closeButton.onClick.RemoveAllListeners();
    }

    void Update()
    {
        if (GameMode.InMenu) return;   // ← 在主页不响应 ESC
        if (Input.GetKeyDown(toggleKey))
            Toggle(!_open);
    }


    public void Toggle(bool show)
    {
        _open = show;
        if (panel)
        {
            if (show && !panel.gameObject.activeSelf)      // ← 新增：显示前确保激活
                panel.gameObject.SetActive(true);

            panel.alpha = show ? 1f : 0f;
            panel.blocksRaycasts = show;
            panel.interactable = show;
        }

        Time.timeScale = show ? 0f : 1f;
        Cursor.visible = show;
        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
    }


    void UpdateHomeLabel()
    {
        if (!homeLabel) return;
        homeLabel.text = (LocalizationManager.Current == GameLanguage.Chinese)
            ? homeLabelZH
            : homeLabelEN;
    }


    // 若你的语言切换按钮想主动刷新这里的文案，可在切换后调用此方法
    public void OnLanguageChangedExternally() => UpdateHomeLabel();

    public void CloseImmediately()
    {
        _open = false;
        if (panel)
        {
            panel.alpha = 0f;
            panel.blocksRaycasts = false;
            panel.interactable = false;
        }
        Time.timeScale = 1f;
    }
    public void ReturnToMenu()
    {
        StartCoroutine(ReturnToMenuRoutine());
    }


    // PauseMenu.cs -> 仅替换 ReturnToMenuRoutine()
    IEnumerator ReturnToMenuRoutine()
    {
        // 保险：先把面板彻底关掉，避免叠在主页上
        if (_open) Toggle(false);
        // if (panel) panel.gameObject.SetActive(false);
        CloseImmediately();

        // NEW: 回菜单前关掉 HUD hint + 所有玩法面板
        var hud = FindObjectOfType<BootstrapHUDLocalization>(true);
        if (hud) hud.SetHintVisible(false);

        foreach (var ui in FindObjectsOfType<TripoInGameUI>(true))
            ui.SafeHideAllUI();

        Time.timeScale = 1f;
        GameMode.SetInMenu(true);
        FindObjectOfType<TripoInGameUI>(true)?.SafeHideAllUI();

        // 先把要卸的场景名收集出来（不要一边 GetSceneAt 一边卸）
        var toUnload = new List<string>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (s.isLoaded && s.name != bootstrapSceneName)
                toUnload.Add(s.name);
        }
        // 再逐个卸
        foreach (var name in toUnload)
        {
            var u = SceneManager.UnloadSceneAsync(name);
            while (u != null && !u.isDone) yield return null;
        }

        // 告知关卡管理器“当前没有关卡了”
        if (LevelManager.Instance) LevelManager.Instance.ClearCurrentLevel();

        // 加载主页（Additive）并设为激活场景
        var op = SceneManager.LoadSceneAsync(mainMenuSceneName, LoadSceneMode.Additive);
        while (!op.isDone) yield return null;

        var menu = SceneManager.GetSceneByName(mainMenuSceneName);
        if (menu.IsValid()) SceneManager.SetActiveScene(menu);   // 设置激活场景

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }


    void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += UpdateHomeLabel;
        UpdateHomeLabel(); // 立刻按当前语言刷新
    }

    void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= UpdateHomeLabel;
    }


}
