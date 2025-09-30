using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class WarehouseUI : MonoBehaviour
{
    [Header("Refs")]
    public CanvasGroup panel;             // �ֿ�����壨�� CanvasGroup��
    public Transform gridRoot;            // �� Cell �ĸ����壨�� GridLayoutGroup��
    public WarehouseCell cellPrefab;      // Cell Ԥ�ƣ��� WarehouseCell �ű���
    public Button prevBtn, nextBtn;
    public Text pageText;
    public TripoInGameUI tripo;           // ����� TripoInGameUI
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

    // WarehouseUI.cs ���� ����������滻�����ڵ� Update()
    void Update()
    {
        // �����˵��Ͳ������κ����룻����ǰ���ţ�˳�ֹص�
        if (GameMode.InMenu)
        {
            if (IsOpen) Hide();
            return;
        }

        // ֻ���ڡ��ǲ˵���ʱ����Ӧ Tab
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (IsOpen) Hide();
            else { Show(); Rebuild(); }   // ��ʱ�����ؽ��б�
        }
    }


    public void Toggle()
    {
        if (IsOpen) Hide();
        else { Show(); Rebuild(); }
    }

    public void Show()
    {
        // �� Prompt �򿪣��ȹص�
        if (tripo && tripo.IsPromptOpen) tripo.TogglePrompt(false);

        panel.alpha = 1; panel.blocksRaycasts = true; panel.interactable = true;
        tripo?.SyncCursor(); // ͳһ���������ʾ/����
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

        // ��ֹɾ����ҳ���ѵ�ǰҳ�������Ч��Χ��
        int maxPage = Mathf.Max(0, Mathf.CeilToInt(total / (float)PerPage) - 1);
        page = Mathf.Clamp(page, 0, maxPage);

        int start = page * PerPage;
        int end = Mathf.Min(start + PerPage, total);

        for (int i = start; i < end; i++)
        {
            var item = list[i];
            var cell = Instantiate(cellPrefab, gridRoot);
            cell.Init(this, item); // WarehouseCell �ڲ��᣺��������/���=����/X=ɾ��+�ص�ˢ��
            cells.Add(cell);
        }

        int pageCount = Mathf.Max(1, Mathf.CeilToInt(total / (float)PerPage));
        if (pageText) pageText.text = $"{page + 1}/{pageCount}";
        if (prevBtn) prevBtn.interactable = page > 0;
        if (nextBtn) nextBtn.interactable = (page + 1) < pageCount;
    }

    // �� WarehouseCell ���ã�ɾ����ˢ�£����ڱ�Ҫʱ����һҳ��
    public void RebuildAfterDelete()
    {
        var totalAfter = WarehouseDB.Instance.Items.Count;
        int maxPage = Mathf.Max(0, Mathf.CeilToInt(totalAfter / (float)PerPage) - 1);
        page = Mathf.Clamp(page, 0, maxPage);
        Rebuild();
    }

    // �� WarehouseCell ���ã���� cell ���ɲ��رղֿ�
    public void SpawnAndClose(string modelUrl)
    {
        Hide();
        tripo?.SpawnExisting(modelUrl);
    }
}
