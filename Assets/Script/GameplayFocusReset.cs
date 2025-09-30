// GameplayFocusReset.cs
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 每次进入“游戏态”（加载 Bootstrap 或关卡）时，统一把全局状态复位：
/// - 取消暂停（timeScale=1）
/// - 锁定并隐藏光标（鼠标可继续操控相机/按键）
/// - 若使用新输入系统，切回 "Player" Action Map
/// </summary>
public static class GameplayFocusReset
{
    // 在任意场景加载完成后（包括 Additive）尝试复位一次
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Hook()
    {
        SceneManager.sceneLoaded += (_, __) => Apply();
    }

    public static void Apply()
    {
        // 1) 避免残留暂停
        Time.timeScale = 1f;                                   // 文档：更改全局时间缩放（常用于暂停/恢复）。:contentReference[oaicite:0]{index=0}

        // 2) 锁定/隐藏光标，进入“游戏操控”模式
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;              // 文档：Cursor.lockState 控制鼠标锁定。:contentReference[oaicite:1]{index=1}

#if ENABLE_INPUT_SYSTEM
        // 3) 若用了 PlayerInput，确保是 Player 映射而不是 UI
        var pi = Object.FindObjectOfType<PlayerInput>();
        if (pi != null && pi.currentActionMap != null && pi.currentActionMap.name != "Player")
            pi.SwitchCurrentActionMap("Player");               // 文档：SwitchCurrentActionMap 可切换当前 Action Map。:contentReference[oaicite:2]{index=2}
#endif
    }
}
