// PauseMenu.cs  ���� ���� Bootstrap ������һ����������
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    public CanvasGroup panel;       // ָ�� PausePanel���� CanvasGroup��
    public Button homeButton;       // ��Return to Menu / ������ҳ��
    public Button closeButton;      // ��� X ��ť
    public Text homeLabel;          // ��ť�ϵ� Text�����Ϊ TMP_Text��


    [Header("Localization")]
    public string homeLabelEN = "Return to Menu";
    public string homeLabelZH = "������ҳ";

    [Header("Scenes")]
    public string bootstrapSceneName = "Bootstrap";
    public string mainMenuSceneName = "MainMenu";

    [Header("Input")]
    public KeyCode toggleKey = KeyCode.Escape;

    bool _open;

    void Start()
    {
        // ��ʼ����
        Toggle(false);

        // �İ�
        UpdateHomeLabel();

        // �¼�
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
        if (GameMode.InMenu) return;   // �� ����ҳ����Ӧ ESC
        if (Input.GetKeyDown(toggleKey))
            Toggle(!_open);
    }


    public void Toggle(bool show)
    {
        _open = show;
        if (panel)
        {
            if (show && !panel.gameObject.activeSelf)      // �� ��������ʾǰȷ������
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


    // ����������л���ť������ˢ��������İ��������л�����ô˷���
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


    // PauseMenu.cs -> ���滻 ReturnToMenuRoutine()
    IEnumerator ReturnToMenuRoutine()
    {
        // ���գ��Ȱ���峹�׹ص������������ҳ��
        if (_open) Toggle(false);
        // if (panel) panel.gameObject.SetActive(false);
        CloseImmediately();

        // NEW: �ز˵�ǰ�ص� HUD hint + �����淨���
        var hud = FindObjectOfType<BootstrapHUDLocalization>(true);
        if (hud) hud.SetHintVisible(false);

        foreach (var ui in FindObjectsOfType<TripoInGameUI>(true))
            ui.SafeHideAllUI();

        Time.timeScale = 1f;
        GameMode.SetInMenu(true);
        FindObjectOfType<TripoInGameUI>(true)?.SafeHideAllUI();

        // �Ȱ�Ҫж�ĳ������ռ���������Ҫһ�� GetSceneAt һ��ж��
        var toUnload = new List<string>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (s.isLoaded && s.name != bootstrapSceneName)
                toUnload.Add(s.name);
        }
        // �����ж
        foreach (var name in toUnload)
        {
            var u = SceneManager.UnloadSceneAsync(name);
            while (u != null && !u.isDone) yield return null;
        }

        // ��֪�ؿ�����������ǰû�йؿ��ˡ�
        if (LevelManager.Instance) LevelManager.Instance.ClearCurrentLevel();

        // ������ҳ��Additive������Ϊ�����
        var op = SceneManager.LoadSceneAsync(mainMenuSceneName, LoadSceneMode.Additive);
        while (!op.isDone) yield return null;

        var menu = SceneManager.GetSceneByName(mainMenuSceneName);
        if (menu.IsValid()) SceneManager.SetActiveScene(menu);   // ���ü����

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }


    void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += UpdateHomeLabel;
        UpdateHomeLabel(); // ���̰���ǰ����ˢ��
    }

    void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= UpdateHomeLabel;
    }


}
