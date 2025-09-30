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
    public Text languageButtonLabel;  // ��ť����ʾ��English/���ġ�����ʾĿ�����ԣ�
    public Button quitButton;

    [Header("Stage panel")]
    public Button backButton;         // StagePanel ��ķ���

    void Start()
    {
        // Ĭ����ʾ�����
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

        // �����л�ʱˢ�°�ť��ǩ
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
        // ��ʾ����һ�����ԡ�
        languageButtonLabel.text =
            (LocalizationManager.Current == GameLanguage.English) ? "����" : "English";
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
        Application.Quit(); // ���������Ϸ�������˳�
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // �༭�����˳� Play ģʽ
#endif
    }
}
