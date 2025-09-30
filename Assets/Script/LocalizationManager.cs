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
        ["ui_start"] = "��ʼ��Ϸ",
        ["ui_language"] = "����",
        ["ui_quit"] = "�˳�",
        ["ui_select_stage"] = "ѡ��ؿ�",
        ["ui_back"] = "����",
        ["ui_thanks"] = "��л���棡",
        ["ui_home"] = "�ص���ҳ",

        ["ui_controls_hint"] =
            "G - ����3Dģ��\n" +
            "F - �ƶ�/����3Dģ��\n" +
            "����/�������Ҽ� - ��ת3Dģ��\n" +
            "Tab - 3Dģ�Ͳֿ�",

        ["ui_generating"] = "��������",
        ["ui_spawn"] = "����",

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
