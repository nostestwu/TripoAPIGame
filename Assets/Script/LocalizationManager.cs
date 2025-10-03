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
        ["hint_lvl1"] =
        "Welcome to Imagi Nation!\n\n" +
        "Press G to generate any object\n" +
        "Press F to pick up / drop objects\n\n" +
        "Use your imagination to get through each level!\n\n" +
        "(Press X to close this tip)",
        ["hint_lvl2"] =
        "The button seems a bit high!\n\n" +
        "You can hold down E after picking up an object to throw it!\n\n" +
        "You can also press Tab to open your inventory. It saves all the objects you've previously generated. That way you don't have to wait again for them to generate!\n\n" +
        "(Press X to close this tip)",
        ["hint_lvl5"] =
        "We’ve encountered an immovable door!\n\n" +
        "Try generating “grenades” or throwable explosive objects. Maybe they will explode!\n\n" +
        "(Press X to close this tip)",


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
        ["hint_lvl1"] =
        "欢迎来到 Imagi Nation！\n\n" +
        "按 G 可以生成任何物体\n" +
        "按 F 可以拾取物体 / 扔下物体\n\n" +
        "请发挥你的想象来通过每一关吧！\n\n" +
        "(按 x 关闭提示)",
        ["hint_lvl2"] =
        "按钮看样子有一点高啊！\n\n" +
        "我们可以拾取物体后长 E 键来把物品扔出去！\n\n" +
        "当然你也可以按 Tab 来打开背包，它会保存你之前生成的所有物品。这样你就不用再去花时间等待物品生成啦！\n\n" +
        "(按 x 关闭提示)",
        ["hint_lvl5"] =
        "我们遇到了不可移动的门！\n\n" +
        "尝试生成 “手雷” 或者可以投掷爆炸的物体。说不定它们会爆炸！\n\n" +
        "(按 x 关闭提示)",

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
