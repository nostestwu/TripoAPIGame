// GameMode.cs�����������������еĹ����ࣩ
using UnityEngine;

public static class GameMode
{
    static bool _inMenu;
    public static bool InMenu => _inMenu;

    public static void SetInMenu(bool inMenu)
    {
        _inMenu = inMenu;

        // 1) PauseMenu��ǿ������ + ������Ϸ������ű�����
        foreach (var p in Object.FindObjectsOfType<PauseMenu>(true))
        {
            p.CloseImmediately();
            p.enabled = !inMenu;
        }

        // 2) ��� TripoInGameUI��������壬���������˵����ܵ��� G ���
        foreach (var t in Object.FindObjectsOfType<TripoInGameUI>(true))
        {
            t.SafeHideAllUI();
            t.enabled = !inMenu;
        }

        // 3) �������淨��ص������ʾ����
        foreach (var pick in Object.FindObjectsOfType<RayPickupController>(true))
            pick.enabled = !inMenu;

        // 4) ���/ʱ��
        Time.timeScale = 1f;
        Cursor.lockState = inMenu ? CursorLockMode.None : CursorLockMode.Locked; // ����=����&����
        Cursor.visible = inMenu;
    }
}
