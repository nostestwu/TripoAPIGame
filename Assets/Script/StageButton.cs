// StageButton.cs
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class StageButton : MonoBehaviour
{
    [Tooltip("Build Settings ��Ĺؿ�������")]
    public string sceneName;

    [Tooltip("����ùؿ�ʱѡ�õ� ModelSpawnAnchor Id����������Ĭ�ϣ�")]
    public string targetAnchorId;

    Button _btn;

    void Awake()
    {
        _btn = GetComponent<Button>();
        _btn.onClick.AddListener(OnClicked);
    }

    void OnClicked()
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[StageButton] sceneName δ����");
            return;
        }
        LevelManager.Instance?.EnterLevel(sceneName, targetAnchorId);
    }
}
