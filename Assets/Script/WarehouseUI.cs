using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class WarehouseUI : MonoBehaviour
{
    [Header("Refs")]
    public CanvasGroup panel;             // 仓库主面板（挂 CanvasGroup）
    public Transform gridRoot;            // 放 Cell 的父物体（有 GridLayoutGroup）
    public WarehouseCell cellPrefab;      // Cell 预制（挂 WarehouseCell 脚本）
    public Button prevBtn, nextBtn;
    public Text pageText;
    public TripoInGameUI tripo;           // 拖你的 TripoInGameUI
    public bool IsOpen => panel && panel.interactable;

    const int PerPage = 12;
    int page = 0;
    readonly List<WarehouseCell> cells = new();

    void Start()
    {
        Hide();
        if (prevBtn) prevBtn.onClick.AddListener(() => { if (page > 0) { page--; Rebuild(); } });
        if (nextBtn) nextBtn.onClick.AddListener(() =>
        {
            int total = WarehouseDB.Instance.Items.Count;
            if ((page + 1) * PerPage < total) { page++; Rebuild(); }
        });
    }

    // WarehouseUI.cs —— 用这段完整替换你现在的 Update()
    void Update()
    {
        // 在主菜单就不接受任何输入；若当前开着，顺手关掉
        if (GameMode.InMenu)
        {
            if (IsOpen) Hide();
            return;
        }

        // 只有在“非菜单”时才响应 Tab
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (IsOpen) Hide();
            else { Show(); Rebuild(); }   // 打开时立刻重建列表
        }
    }


    public void Toggle()
    {
        if (IsOpen) Hide();
        else { Show(); Rebuild(); }
    }

    public void Show()
    {
        // 若 Prompt 打开，先关掉
        if (tripo && tripo.IsPromptOpen) tripo.TogglePrompt(false);

        panel.alpha = 1; panel.blocksRaycasts = true; panel.interactable = true;
        tripo?.SyncCursor(); // 统一处理鼠标显示/锁定
    }

    public void Hide()
    {
        panel.alpha = 0; panel.blocksRaycasts = false; panel.interactable = false;
        tripo?.SyncCursor();
    }

    void Clear()
    {
        foreach (var c in cells) if (c) Destroy(c.gameObject);
        cells.Clear();
    }

    public void Rebuild()
    {
        Clear();

        var list = WarehouseDB.Instance.Items;
        int total = list.Count;

        // 防止删到空页：把当前页码夹在有效范围内
        int maxPage = Mathf.Max(0, Mathf.CeilToInt(total / (float)PerPage) - 1);
        page = Mathf.Clamp(page, 0, maxPage);

        int start = page * PerPage;
        int end = Mathf.Min(start + PerPage, total);

        for (int i = start; i < end; i++)
        {
            var item = list[i];
            var cell = Instantiate(cellPrefab, gridRoot);
            cell.Init(this, item); // WarehouseCell 内部会：设置文字/点击=生成/X=删除+回调刷新
            cells.Add(cell);
        }

        int pageCount = Mathf.Max(1, Mathf.CeilToInt(total / (float)PerPage));
        if (pageText) pageText.text = $"{page + 1}/{pageCount}";
        if (prevBtn) prevBtn.interactable = page > 0;
        if (nextBtn) nextBtn.interactable = (page + 1) < pageCount;
    }

    // 给 WarehouseCell 调用：删除后刷新（并在必要时回退一页）
    public void RebuildAfterDelete()
    {
        var totalAfter = WarehouseDB.Instance.Items.Count;
        int maxPage = Mathf.Max(0, Mathf.CeilToInt(totalAfter / (float)PerPage) - 1);
        page = Mathf.Clamp(page, 0, maxPage);
        Rebuild();
    }

    // 给 WarehouseCell 调用：点击 cell 生成并关闭仓库
    public void SpawnAndClose(string modelUrl)
    {
        Hide();
        tripo?.SpawnExisting(modelUrl);
    }
}
