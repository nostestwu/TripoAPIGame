using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class WarehouseItem
{
    public string id;
    public string prompt;
    public string modelUrl;       // Tripo 返回的 glb/gltf URL 或本地目录
    public long addedUnix;
}

[Serializable]
class WarehouseSave { public List<WarehouseItem> items = new List<WarehouseItem>(); }

public class WarehouseDB : MonoBehaviour
{
    public static WarehouseDB Instance { get; private set; }
    public List<WarehouseItem> Items => data.items;

    WarehouseSave data = new WarehouseSave();
    string savePath;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        savePath = Path.Combine(Application.persistentDataPath, "warehouse.json"); // 持久目录
        Load();
        Debug.Log($"[WarehouseDB] Loaded {data.items.Count} items from {savePath}");

    }

    public void Add(string prompt, string modelUrl)
    {
        var it = new WarehouseItem
        {
            id = Guid.NewGuid().ToString("N"),
            prompt = prompt,
            modelUrl = modelUrl,
            addedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        data.items.Add(it);
        Save();
    }

    public void Save()
    {
        var json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
    }

    public void Load()
    {
        if (!File.Exists(savePath)) return;
        var json = File.ReadAllText(savePath);
        var loaded = JsonUtility.FromJson<WarehouseSave>(json);
        if (loaded != null && loaded.items != null) data = loaded;

        // ✨ 去重：同一路径只保留最早的一条记录
        var seen = new HashSet<string>();
        var dedup = new List<WarehouseItem>();
        foreach (var it in data.items)
        {
            if (it == null || string.IsNullOrEmpty(it.modelUrl)) continue;
            string k = NormalizePathKey(it.modelUrl);
            if (seen.Add(k))
            {
                it.modelUrl = k;         // 归一化写回
                dedup.Add(it);
            }
        }
        data.items = dedup;
        Save();
    }


    // WarehouseDB.cs 里加上：
    public bool Remove(string id)
    {
        int idx = data.items.FindIndex(it => it.id == id);
        if (idx >= 0)
        {
            data.items.RemoveAt(idx);
            Save();                 // 立刻写回到 persistentDataPath
            return true;
        }
        return false;
    }
    // 在 WarehouseDB 类里加：

    public bool ContainsUrl(string url)
    {
        string k = NormalizePathKey(url);
        return data.items.Exists(it => it != null && NormalizePathKey(it.modelUrl) == k);
    }

    public void ReplaceUrlEverywhere(string oldUrl, string newUrl)
    {
        string oldK = NormalizePathKey(oldUrl);
        string newK = NormalizePathKey(newUrl);
        if (string.IsNullOrEmpty(oldK) || string.IsNullOrEmpty(newK)) return;

        bool changed = false;
        foreach (var it in data.items)
        {
            if (it != null && NormalizePathKey(it.modelUrl) == oldK)
            {
                it.modelUrl = newK;
                changed = true;
            }
        }
        if (changed) Save();
    }


    static string NormalizePathKey(string p)
    {
        if (string.IsNullOrEmpty(p)) return "";
        // 绝对路径 & 统一斜杠
        try { p = Path.GetFullPath(p); } catch { }
        p = p.Replace('\\', '/');

        // Windows 下转小写避免 C: 与 c: 重复
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        p = p.ToLowerInvariant();
#endif
        // 清掉误存的 "file://"
        if (p.StartsWith("file://")) p = p.Substring("file://".Length);
        return p;
    }

}
