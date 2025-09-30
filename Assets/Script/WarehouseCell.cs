using UnityEngine;
using UnityEngine.UI;

public class WarehouseCell : MonoBehaviour
{
    [Header("Refs in prefab")]
    public Button mainButton;     // 整个格子的按钮
    public Button deleteButton;   // 右上角 X
    public Text label;            // 中间文字

    WarehouseUI owner;
    string itemId;
    string modelUrl;

    public void Init(WarehouseUI owner, WarehouseItem item)
    {
        this.owner = owner;
        itemId = item.id;
        modelUrl = item.modelUrl;

        if (label) label.text = Trunc(item.prompt, 28);

        // WarehouseCell.cs -> Init 里 mainButton 的回调，只改这段
        if (mainButton)
        {
            mainButton.onClick.RemoveAllListeners();
            mainButton.onClick.AddListener(() => {
                owner.Hide();

                // ⚠️ 不要用 owner.tripo 的老引用。点击时现场找一个可用的 TripoInGameUI
                var tripo = Object.FindObjectOfType<TripoInGameUI>();
                if (tripo)
                    tripo.SpawnExisting(modelUrl);
                else
                    Debug.LogWarning("[WarehouseCell] No TripoInGameUI in this scene; spawn ignored.");
            });
        }


        if (deleteButton)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(() => {
                // 1) 删数据库
                WarehouseDB.Instance.Remove(itemId);
                // 2) 立刻移除这个格子
                Destroy(gameObject);
                // 3) 让 UI 重新排布/翻页
                owner.RebuildAfterDelete();
            });
        }
    }

    static string Trunc(string s, int n) =>
        string.IsNullOrEmpty(s) ? "" : (s.Length <= n ? s : s.Substring(0, n) + "…");
}
