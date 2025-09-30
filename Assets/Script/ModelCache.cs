// ModelCache.cs
using System.Collections.Generic;
using UnityEngine;

public static class ModelCache
{
    static readonly Dictionary<string, GameObject> _prefabLike = new();
    static GameObject _cacheRoot;

    public static bool TryGet(string key, out GameObject prefab)
        => _prefabLike.TryGetValue(key, out prefab);

    /// <summary>
    /// 把“现场已加载好的实例”复制一份，做成可反复 Instantiate 的缓存副本。
    /// - 会深拷贝 Mesh/Material，避免原实例销毁后资源被卸载导致“看不见”
    /// - 会移除 glTFast 之类的运行时加载器组件（只保留纯渲染/碰撞）
    /// - 支持多个 key（别名）都指向同一份缓存
    /// </summary>
    public static void RegisterFromInstance(string primaryKey, GameObject source, params string[] aliasKeys)
    {
        if (string.IsNullOrEmpty(primaryKey) || source == null) return;
        if (_prefabLike.ContainsKey(primaryKey)) return;

        if (_cacheRoot == null)
        {
            _cacheRoot = new GameObject("[ModelCache]");
            Object.DontDestroyOnLoad(_cacheRoot);
        }

        // 1) 复制一份层级
        var cacheCopy = Object.Instantiate(source, _cacheRoot.transform);
        cacheCopy.name = "[Cached] " + source.name;

        // 2) 移除 glTF 运行时组件（避免再次去下载/持有加载器状态）
        var gltfs = cacheCopy.GetComponentsInChildren<GLTFast.GltfAsset>(true);
        foreach (var g in gltfs)
        {
            if (!g) continue;
            g.enabled = false;               // 关掉，不触发加载；但保留组件与其持有的引用
                                             // 可选：g.hideFlags = HideFlags.DontSave; // 仅编辑器相关，可不加
        }

        // 3) 深拷贝渲染资源：Mesh + Material（关键！）
        //    这样即使原对象/加载器销毁，缓存副本依然有独立资源可渲染
        foreach (var mf in cacheCopy.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh) mf.sharedMesh = Object.Instantiate(mf.sharedMesh);
        }
        foreach (var smr in cacheCopy.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (smr.sharedMesh) smr.sharedMesh = Object.Instantiate(smr.sharedMesh);
            var mats = smr.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) if (mats[i]) mats[i] = new Material(mats[i]);
            smr.sharedMaterials = mats;
        }
        foreach (var mr in cacheCopy.GetComponentsInChildren<MeshRenderer>(true))
        {
            var mats = mr.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) if (mats[i]) mats[i] = new Material(mats[i]);
            mr.sharedMaterials = mats;
        }
        // 说明：Unity 文档明确“访问 Renderer.material 会克隆共享材质供该实例独占”，
        // 这里我们主动 new Material() 达到相同目的，确保独立生命周期。:contentReference[oaicite:2]{index=2}
        // 同理对 Mesh 做 Instantiate，避免共享 Mesh 被卸载影响。:contentReference[oaicite:3]{index=3}

        // 4) 作为“预制体”使用时，缓存体保持禁用态
        cacheCopy.SetActive(false);

        _prefabLike[primaryKey] = cacheCopy;
        if (aliasKeys != null)
        {
            foreach (var k in aliasKeys)
            {
                if (!string.IsNullOrEmpty(k) && !_prefabLike.ContainsKey(k))
                    _prefabLike[k] = cacheCopy;
            }
        }
    }

    /// <summary>
    /// 删除缓存。会把指向同一缓存体的所有 key 一并移除。
    /// </summary>
    public static bool Remove(string key)
    {
        if (!_prefabLike.TryGetValue(key, out var cached) || cached == null)
        {
            return false;
        }

        // 找到所有指向同一对象的 key 一起删
        var toRemove = new List<string>();
        foreach (var kv in _prefabLike)
            if (kv.Value == cached) toRemove.Add(kv.Key);

        foreach (var k in toRemove) _prefabLike.Remove(k);

        Object.Destroy(cached);
        return true;
    }
}
