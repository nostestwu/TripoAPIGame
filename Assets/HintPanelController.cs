using UnityEngine;
using UnityEngine.UI;

public class HintPanelController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private Text hintText;  // 使用 UnityEngine.UI.Text

    [Header("Localization Key")]
    [Tooltip("LocalizationManager 中的 key，例如 hint_lvl1, hint_lvl2 等")]
    [SerializeField] private string hintKey;

    void Start()
    {
        UpdateHintText();
    }

    void Update()
    {
        if (hintPanel != null && hintPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.X))
            {
                CloseHintPanel();
            }
        }
    }

    public void CloseHintPanel()
    {
        if (hintPanel != null)
        {
            hintPanel.SetActive(false);
        }
    }

    private void UpdateHintText()
    {
        if (hintText == null) return;
        if (string.IsNullOrEmpty(hintKey)) return;

        string localized = LocalizationManager.Get(hintKey);
        hintText.text = localized;
    }

    void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += OnLanguageChanged;
    }

    void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        UpdateHintText();
    }
}
