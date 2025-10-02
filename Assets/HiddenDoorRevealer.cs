using UnityEngine;

public class HiddenDoorRevealer : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("场景里所有篮球框的触发器")]
    public HoopTrigger[] hoops;
    [Tooltip("隐藏门（把你的传送门做其子物体），开始时需要隐藏")]
    public GameObject hiddenDoorRoot;

    int _total, _green;

    void Awake()
    {
        if (hiddenDoorRoot) hiddenDoorRoot.SetActive(false); // 先隐藏:contentReference[oaicite:5]{index=5}

        _total = (hoops != null) ? hoops.Length : 0;
        _green = 0;

        if (hoops != null)
        {
            foreach (var h in hoops)
            {
                if (!h) continue;
                // 订阅一次
                h.onScored.AddListener(OnHoopScored);
            }
        }
    }

    void OnHoopScored()
    {
        _green++;
        if (_green >= _total && hiddenDoorRoot)
        {
            hiddenDoorRoot.SetActive(true);   // 全部点亮 → 显示门（内含你的传送门）:contentReference[oaicite:6]{index=6}
        }
    }
}
