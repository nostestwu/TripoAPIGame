// MainMenuUI.cs
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("Panels (CanvasGroup)")]
    public CanvasGroup mainPanel;     // MainPanel
    public CanvasGroup stagePanel;    // StagePanel

    [Header("Main buttons")]
    public Button startButton;
    public Button languageButton;
    public Text languageButtonLabel;  // 按钮上显示“English/中文”（显示目标语言）
    public Button quitButton;

    [Header("Stage panel")]
    public Button backButton;         // StagePanel 里的返回

    void Start()
    {
        // 默认显示主面板
        ShowPanel(mainPanel, true);
        ShowPanel(stagePanel, false);

        if (startButton) startButton.onClick.AddListener(() =>
        {
            ShowPanel(mainPanel, false);
            ShowPanel(stagePanel, true);
        });

        if (languageButton) languageButton.onClick.AddListener(() =>
        {
            LocalizationManager.ToggleLanguage();
            UpdateLanguageButtonLabel();
        });

        if (quitButton) quitButton.onClick.AddListener(QuitGame);

        if (backButton) backButton.onClick.AddListener(() =>
        {
            ShowPanel(stagePanel, false);
            ShowPanel(mainPanel, true);
        });

        // 语言切换时刷新按钮标签
        LocalizationManager.OnLanguageChanged += UpdateLanguageButtonLabel;
        UpdateLanguageButtonLabel();
    }

    void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= UpdateLanguageButtonLabel;
    }

    void UpdateLanguageButtonLabel()
    {
        if (!languageButtonLabel) return;
        // 显示“下一种语言”
        languageButtonLabel.text =
            (LocalizationManager.Current == GameLanguage.English) ? "中文" : "English";
    }

    static void ShowPanel(CanvasGroup g, bool show)
    {
        if (!g) return;
        g.alpha = show ? 1 : 0;
        g.interactable = show;
        g.blocksRaycasts = show;
    }

    static void QuitGame()
    {
        Application.Quit(); // 构建后的游戏会正常退出
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // 编辑器里退出 Play 模式
#endif
    }
}
