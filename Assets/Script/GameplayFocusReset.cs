// GameplayFocusReset.cs
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// ÿ�ν��롰��Ϸ̬�������� Bootstrap ��ؿ���ʱ��ͳһ��ȫ��״̬��λ��
/// - ȡ����ͣ��timeScale=1��
/// - ���������ع�꣨���ɼ����ٿ����/������
/// - ��ʹ��������ϵͳ���л� "Player" Action Map
/// </summary>
public static class GameplayFocusReset
{
    // �����ⳡ��������ɺ󣨰��� Additive�����Ը�λһ��
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Hook()
    {
        SceneManager.sceneLoaded += (_, __) => Apply();
    }

    public static void Apply()
    {
        // 1) ���������ͣ
        Time.timeScale = 1f;                                   // �ĵ�������ȫ��ʱ�����ţ���������ͣ/�ָ�����:contentReference[oaicite:0]{index=0}

        // 2) ����/���ع�꣬���롰��Ϸ�ٿء�ģʽ
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;              // �ĵ���Cursor.lockState �������������:contentReference[oaicite:1]{index=1}

#if ENABLE_INPUT_SYSTEM
        // 3) ������ PlayerInput��ȷ���� Player ӳ������� UI
        var pi = Object.FindObjectOfType<PlayerInput>();
        if (pi != null && pi.currentActionMap != null && pi.currentActionMap.name != "Player")
            pi.SwitchCurrentActionMap("Player");               // �ĵ���SwitchCurrentActionMap ���л���ǰ Action Map��:contentReference[oaicite:2]{index=2}
#endif
    }
}
