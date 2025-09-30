using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;

public class EndScreenUI : MonoBehaviour
{
    [Header("UI")]
    public CanvasGroup root;         // 整体面板
    public Text thanksLabel;         // “感谢游玩 / Thanks for playing”
    public Button homeButton;        // “回到主页”
    public Text homeLabel;           // 可选：若不拖，则自动在 homeButton 子物体里找

    [Header("Text / Keys")]
    [TextArea] public string thanksText = "Thanks for playing!";
    [SerializeField] string thanksKey = "ui_thanks";
    [SerializeField] string homeKey = "ui_home";

    [Header("Behavior")]
    public bool pauseGameOnShow = true;
    public string bootstrapSceneName = "Bootstrap";
    public string mainMenuSceneName = "MainMenu";

    [Header("Fallback (no MainMenu)")]
    public string fallbackLevelName = "Level_01";
    public string fallbackAnchorId = "";

    [Header("Events (optional)")]
    public UnityEvent onShown;
    public UnityEvent onReturnHome;

    float _prevTimeScale = 1f;

    /* ---------------- Life ---------------- */

    void Awake()
    {
        HideImmediate();

        if (homeButton)
        {
            homeButton.onClick.RemoveAllListeners();
            homeButton.onClick.AddListener(HandleHomeClicked);
        }
    }

    void OnDestroy()
    {
        if (homeButton) homeButton.onClick.RemoveAllListeners();
        LocalizationManager.OnLanguageChanged -= RefreshTexts;
    }

    void OnEnable()
    {
        LocalizationManager.OnLanguageChanged -= RefreshTexts;
        LocalizationManager.OnLanguageChanged += RefreshTexts;
        RefreshTexts();
    }

    void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= RefreshTexts;
    }

    /* ---------------- Show / Hide ---------------- */

    public void Show()
    {
        if (!root) { Debug.LogWarning("[EndScreenUI] Missing CanvasGroup."); return; }

        RefreshTexts();

        if (pauseGameOnShow)
        {
            _prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        root.alpha = 1f;
        root.blocksRaycasts = true;
        root.interactable = true;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;   // Locked会隐藏并固定在屏幕中心。:contentReference[oaicite:0]{index=0}

        onShown?.Invoke();
    }

    public void Hide()
    {
        if (!root) return;
        root.alpha = 0f;
        root.blocksRaycasts = false;
        root.interactable = false;

        if (pauseGameOnShow)
            Time.timeScale = _prevTimeScale;
    }

    public void HideImmediate()
    {
        if (!root) return;
        root.alpha = 0f;
        root.blocksRaycasts = false;
        root.interactable = false;
    }

    /* ---------------- Localization ---------------- */

    void RefreshTexts()
    {
        // 感谢语
        if (thanksLabel)
        {
            string t = LocalizationManager.Get(thanksKey);
            thanksLabel.text = string.IsNullOrEmpty(t) ? thanksText : t;
        }

        // Home 按钮文字（优先用 Inspector 的 homeLabel；否则在按钮子物体中查找）
        if (!homeLabel && homeButton)
            homeLabel = homeButton.GetComponentInChildren<Text>(true);   // 查找子层级文字组件。:contentReference[oaicite:1]{index=1}

        if (homeLabel)
        {
            string h = LocalizationManager.Get(homeKey);
            homeLabel.text = string.IsNullOrEmpty(h) ? "Return to Menu" : h;
        }
    }

    /* ---------------- Button ---------------- */

    void HandleHomeClicked()
    {
        Hide();
        if (pauseGameOnShow) Time.timeScale = _prevTimeScale;

        if (onReturnHome != null && onReturnHome.GetPersistentEventCount() > 0)
        {
            onReturnHome.Invoke();
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            return;
        }

        // 优先让常驻的 PauseMenu 负责回主菜单（它已经处理过输入/时间/鼠标等）
        var pause = FindObjectOfType<PauseMenu>(true);
        var goHome = pause ? typeof(PauseMenu).GetMethod("ReturnToMenu",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public) : null;
        if (pause && goHome != null)
        {
            goHome.Invoke(pause, null);
            return;
        }

        // 兜底：没有 PauseMenu 就直接进主菜单或回默认关
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName, LoadSceneMode.Single);
        }
        else
        {
            LevelManager.Instance?.EnterLevel(fallbackLevelName, fallbackAnchorId);
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    /* ---------------- Utilities ---------------- */

    public void SetThanksText(string text)
    {
        thanksText = text;
        if (root && root.interactable && thanksLabel) thanksLabel.text = thanksText;
    }
}
