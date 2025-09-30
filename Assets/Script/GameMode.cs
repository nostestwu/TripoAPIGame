// GameMode.cs（新增或扩充你已有的工具类）
using UnityEngine;

public static class GameMode
{
    static bool _inMenu;
    public static bool InMenu => _inMenu;

    public static void SetInMenu(bool inMenu)
    {
        _inMenu = inMenu;

        // 1) PauseMenu：强制隐藏 + 仅在游戏里允许脚本工作
        foreach (var p in Object.FindObjectsOfType<PauseMenu>(true))
        {
            p.CloseImmediately();
            p.enabled = !inMenu;
        }

        // 2) 你的 TripoInGameUI：收起面板，避免在主菜单还能弹出 G 面板
        foreach (var t in Object.FindObjectsOfType<TripoInGameUI>(true))
        {
            t.SafeHideAllUI();
            t.enabled = !inMenu;
        }

        // 3) 其他与玩法相关的组件（示例）
        foreach (var pick in Object.FindObjectsOfType<RayPickupController>(true))
            pick.enabled = !inMenu;

        // 4) 鼠标/时间
        Time.timeScale = 1f;
        Cursor.lockState = inMenu ? CursorLockMode.None : CursorLockMode.Locked; // 锁定=隐藏&束缚
        Cursor.visible = inMenu;
    }
}
