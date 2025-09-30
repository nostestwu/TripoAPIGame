// LocalizationManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameLanguage { English, Chinese }

public static class LocalizationManager
{
    public static event Action OnLanguageChanged;

    const string PrefKey = "game.lang";
    static GameLanguage _current = GameLanguage.English;
    public static GameLanguage Current => _current;

    static readonly Dictionary<string, string> EN = new Dictionary<string, string>
    {
        ["ui_start"] = "Start",
        ["ui_language"] = "Language",
        ["ui_quit"] = "Quit",
        ["ui_select_stage"] = "Select Stage",
        ["ui_back"] = "Back",
        ["ui_thanks"] = "Thanks for playing!",
        ["ui_home"] = "Main Menu",
        ["ui_controls_hint"] =
            "G - Generate 3D Model\n" +
            "F - Move/Drop 3D Model\n" +
            "Mouse Wheel/Hold L/R Mouse - \n " +
        "           Rotate 3D model\n" +
            "Tab - 3D Model Warehouse",
        ["ui_generating"] = "Generating",
        ["ui_spawn"] = "Generate",


    };

    static readonly Dictionary<string, string> ZH = new Dictionary<string, string>
    {
        ["ui_start"] = "开始游戏",
        ["ui_language"] = "语言",
        ["ui_quit"] = "退出",
        ["ui_select_stage"] = "选择关卡",
        ["ui_back"] = "返回",
        ["ui_thanks"] = "感谢游玩！",
        ["ui_home"] = "回到主页",

        ["ui_controls_hint"] =
            "G - 生成3D模型\n" +
            "F - 移动/放下3D模型\n" +
            "滑轮/长按左右键 - 旋转3D模型\n" +
            "Tab - 3D模型仓库",

        ["ui_generating"] = "正在生成",
        ["ui_spawn"] = "生成",

    };

    static LocalizationManager()
    {
        var saved = PlayerPrefs.GetString(PrefKey, "en");
        _current = saved == "zh" ? GameLanguage.Chinese : GameLanguage.English;
    }

    public static void ToggleLanguage()
    {
        SetLanguage(_current == GameLanguage.English ? GameLanguage.Chinese : GameLanguage.English);
    }

    public static void SetLanguage(GameLanguage lang)
    {
        if (_current == lang) return;
        _current = lang;
        PlayerPrefs.SetString(PrefKey, _current == GameLanguage.Chinese ? "zh" : "en");
        PlayerPrefs.Save();
        OnLanguageChanged?.Invoke();
    }

    public static string Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        var dict = _current == GameLanguage.Chinese ? ZH : EN;
        return dict.TryGetValue(key, out var s) ? s : key;
    }
}
