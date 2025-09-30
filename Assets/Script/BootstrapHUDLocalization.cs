using UnityEngine;
using UnityEngine.UI;

public class BootstrapHUDLocalization : MonoBehaviour
{
    [Header("Controls Hint UI")]
    public CanvasGroup hintGroup;     // 右下角提示面板（带 CanvasGroup）
    public Text hintText;             // 键位提示（多行）
    public Text spawnText;            // “生成”按钮上的文字
    public bool showOnStart = true;

    // 提供给别的脚本（如 TripoInGameUI）使用的“Generating”前缀
    public static string GeneratingPrefix { get; private set; } = "Generating";

    [Header("Localization Keys")]
    [SerializeField] string hintKey = "ui_controls_hint";
    [SerializeField] string generatingKey = "ui_generating";
    [SerializeField] string spawnKey = "ui_spawn";  // ← 你说的 key

    public void SetHintVisible(bool show)
    {
        if (!hintGroup) return;
        hintGroup.alpha = show ? 1f : 0f;
        hintGroup.blocksRaycasts = show;
        hintGroup.interactable = show;   // 控制交互/可见同一处
        // CanvasGroup 的 alpha / interactable / blocksRaycasts 就是官方推荐的显隐/可交互控制方式。
    }

    void OnEnable()
    {
        LocalizationManager.OnLanguageChanged -= RefreshTexts;
        LocalizationManager.OnLanguageChanged += RefreshTexts;
        RefreshTexts();

        // 进主菜单时默认隐藏，进关卡再由发车按钮/LevelManager打开
        SetHintVisible(!GameMode.InMenu && showOnStart);
    }

    void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= RefreshTexts;
    }

    void RefreshTexts()
    {
        // 1) 键位提示（多行）
        if (hintText)
        {
            string localized = LocalizationManager.Get(hintKey);
            if (string.IsNullOrEmpty(localized))
            {
                // 中文默认（你给的文案）
                localized =
                    "G - 生成3D模型\n" +
                    "F - 移动前方3D模型\n" +
                    "滑轮/长按左右键 - 旋转3D模型\n" +
                    "Tab - 3D模型仓库";
            }
            hintText.text = localized;
        }

        // 2) Generating 前缀（给 TripoInGameUI 用）
        string gen = LocalizationManager.Get(generatingKey);
        GeneratingPrefix = string.IsNullOrEmpty(gen) ? "Generating" : gen;

        // 3) “生成”按钮文字
        if (spawnText)
        {
            string spawnLabel = LocalizationManager.Get(spawnKey);
            if (string.IsNullOrEmpty(spawnLabel))
                spawnLabel = (LocalizationManager.Current == GameLanguage.Chinese) ? "生成" : "Generate";
            spawnText.text = spawnLabel;   // ✅ 正确地设置 Text 组件的 .text
        }
    }
}
