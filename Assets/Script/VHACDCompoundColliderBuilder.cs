// Assets/Scripts/VHACDCompoundColliderBuilder.cs
using System.Collections.Generic;
using UnityEngine;
using MeshProcess; // 你提供的 VHACD.cs 在这个命名空间

public static class VHACDCompoundColliderBuilder
{
    const int ConvexTriLimit = 255;

    public static void Build(GameObject root, int maxHullsPerMesh = 32, float rigidbodyMass = 300f)
    {
        if (!root) return;

        int total = 0;

        var filters = root.GetComponentsInChildren<MeshFilter>(includeInactive: false);
        if (filters.Length == 0)
        {
            Debug.LogWarning("[VHACD] 没找到 MeshFilter，是否过早调用？");
        }

        foreach (var mf in filters)
        {
            var mesh = mf.sharedMesh;
            if (!mesh) continue;

            // 清掉旧 collider
            foreach (var c in mf.GetComponents<Collider>()) Object.Destroy(c);

            // ----- 情况 0：CPU 网格不可读（多发生在未设置 GLTFAST_KEEP_MESH_DATA）-----
            if (!mesh.isReadable)
            {
                Debug.LogWarning($"[VHACD] {mf.name} mesh.isReadable=false。请在 Scripting Define Symbols 加 GLTFAST_KEEP_MESH_DATA。改用 BoxCollider 兜底。");
                mf.gameObject.AddComponent<BoxCollider>();
                total++;
                continue;
            }

            // ----- 情况 1：小网格，直接 convex -----
            int triCount = 0;
            try { triCount = mesh.triangles.Length / 3; } catch { triCount = int.MaxValue; }
            if (triCount <= ConvexTriLimit)
            {
                var mc = mf.gameObject.AddComponent<MeshCollider>();

                // ✅ 先预烘焙，并且和后面 mc.convex = true 保持一致
                Physics.BakeMesh(mesh.GetInstanceID(), /*convex*/ true);


                mc.sharedMesh = mesh;
                mc.convex = true;
                total++;
                Debug.Log($"[VHACD] {mf.name} 直接用 Convex MeshCollider（三角={triCount})");
                continue;
            }

            // ----- 情况 2：复杂网格，跑 VHACD -----
            try
            {
                var vhacd = mf.GetComponent<VHACD>();
                if (!vhacd) vhacd = mf.gameObject.AddComponent<VHACD>();

                var p = vhacd.m_parameters; p.Init();
                p.m_concavity = 0.01;           // 更大的凹度容忍度 => 更少块数
                p.m_resolution = 100_000;       // 体素分辨率下降
                p.m_planeDownsampling = 8;      // 提升平面下采样
                p.m_convexhullDownsampling = 8; // 提升凸包下采样
                p.m_maxNumVerticesPerCH = 32;   // 单个凸包顶点更少，更易满足 PhysX 限制
                p.m_projectHullVertices = true; // 把凸包顶点投影回原始网格以提升精度

                p.m_maxConvexHulls = (uint)Mathf.Clamp(maxHullsPerMesh, 1, 256);
                vhacd.m_parameters = p;

                var hulls = vhacd.GenerateConvexMeshes(mesh);

                if (hulls != null && hulls.Count > 0)
                {
                    int use = Mathf.Min(hulls.Count, maxHullsPerMesh);
                    Debug.Log($"[VHACD] {mf.name} 三角={triCount}  → hulls={hulls.Count}（使用 {use}）");

                    for (int i = 0; i < use; i++)
                    {
                        var h = hulls[i];
                        if (!h) continue;

                        var child = new GameObject($"VHACD_Hull_{i}");
                        child.transform.SetParent(mf.transform, false);
                        child.layer = mf.gameObject.layer;

                        var mc = child.AddComponent<MeshCollider>();

                        // ✅ 先预烘焙 hull，减少赋值时的卡顿
                        Physics.BakeMesh(h.GetInstanceID(), /*convex*/ true);

                        mc.sharedMesh = h;
                        mc.convex = true;
                        total++;
                    }
                }
                else
                {
                    Debug.LogWarning($"[VHACD] {mf.name} 返回 0 hull（可能参数太严或输入异常）。改用 BoxCollider。");
                    mf.gameObject.AddComponent<BoxCollider>();
                    total++;
                }
            }
            catch (System.DllNotFoundException e)
            {
                Debug.LogError($"[VHACD] libvhacd 未加载。把 DLL 放入 Assets/Plugins/x86_64/ 或改用官方包。Fallback BoxCollider。详情：{e.Message}");
                mf.gameObject.AddComponent<BoxCollider>();
                total++;
            }
        }

        // 父节点复合刚体
        var rb = root.GetComponent<Rigidbody>();
        if (!rb) rb = root.AddComponent<Rigidbody>();
        rb.mass = Mathf.Max(0.1f, rigidbodyMass);

        Debug.Log($"VHACDCompoundColliderBuilder：总共生成 {total} 个 Collider。");
    }
}
