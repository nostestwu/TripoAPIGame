// StageButton.cs
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class StageButton : MonoBehaviour
{
    [Tooltip("Build Settings 里的关卡场景名")]
    public string sceneName;

    [Tooltip("进入该关卡时选用的 ModelSpawnAnchor Id（可留空用默认）")]
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
            Debug.LogWarning("[StageButton] sceneName 未设置");
            return;
        }
        LevelManager.Instance?.EnterLevel(sceneName, targetAnchorId);
    }
}
