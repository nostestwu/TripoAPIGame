using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class DestructibleDoor : MonoBehaviour
{
    [Header("Hierarchy")]
    [Tooltip("装所有碎片的根（空节点）。")]
    public Transform breakableRoot;

    [Tooltip("整面门的碰撞体（爆炸后会关闭），可留空")]
    public Collider solidWallCollider;

    [Header("Initial State")]
    public bool freezeFragmentsOnStart = true;
    public bool ensureCollidersEnabled = true;

    [Header("Explosion Reaction")]
    public float extraUpwards = 0.2f;
    public float randomTorque = 1.5f;
    public bool scaleForceByDistance = true;
    [Tooltip("爆炸时若有碎片或根处于未激活，强制激活它们")]
    public bool ensureActiveOnExplode = true;

    [Header("Debug")]
    public bool verbose = false;

    readonly List<Rigidbody> _fragments = new();
    readonly List<Collider> _fragmentColliders = new();
    bool _opened = false;

    void Reset()
    {
        if (breakableRoot == null && transform.childCount > 0)
            breakableRoot = transform.GetChild(0);
        if (solidWallCollider == null)
            solidWallCollider = GetComponent<Collider>();
    }

    void Awake()
    {
        if (!breakableRoot)
        {
            Debug.LogError("[DestructibleDoor] 请指定 breakableRoot", this);
            return;
        }
        _fragments.Clear();
        _fragments.AddRange(breakableRoot.GetComponentsInChildren<Rigidbody>(true));
        _fragmentColliders.Clear();
        _fragmentColliders.AddRange(breakableRoot.GetComponentsInChildren<Collider>(true));
    }

    void Start()
    {
        if (freezeFragmentsOnStart) FreezeAllFragments();
        if (ensureCollidersEnabled) EnableAllFragmentColliders();
        if (solidWallCollider) solidWallCollider.enabled = true;

        if (verbose) Debug.Log($"[DestructibleDoor] cached fragments={_fragments.Count}, colliders={_fragmentColliders.Count}", this);
    }

    public void ExplodeOpen(Vector3 center, float force, float radius)
    {
        if (_opened) return;
        _opened = true;

        if (ensureActiveOnExplode)
        {
            if (!gameObject.activeInHierarchy)
            {
                if (verbose) Debug.Log("[DestructibleDoor] Activating door root", this);
                gameObject.SetActive(true);
            }
            if (breakableRoot && !breakableRoot.gameObject.activeSelf)
            {
                if (verbose) Debug.Log("[DestructibleDoor] Activating breakableRoot", breakableRoot);
                breakableRoot.gameObject.SetActive(true);
            }
            // 确保所有碎片节点激活
            foreach (var rb in _fragments)
            {
                if (rb && !rb.gameObject.activeSelf) rb.gameObject.SetActive(true);
            }
        }

        if (solidWallCollider) solidWallCollider.enabled = false;

        for (int i = 0; i < _fragments.Count; i++)
        {
            var rb = _fragments[i];
            if (!rb) continue;

            rb.isKinematic = false;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            float scaledForce = force;
            if (scaleForceByDistance)
            {
                var col = rb.GetComponent<Collider>();
                Vector3 p = col ? col.ClosestPoint(center) : rb.worldCenterOfMass;
                float t = Mathf.Clamp01(Vector3.Distance(center, p) / Mathf.Max(0.0001f, radius));
                scaledForce = Mathf.Lerp(force, 0f, t);
            }

            rb.AddExplosionForce(scaledForce, center, radius, extraUpwards, ForceMode.Impulse);
            if (randomTorque > 0f) rb.AddTorque(Random.onUnitSphere * randomTorque, ForceMode.Impulse);
        }

        if (verbose) Debug.Log("[DestructibleDoor] BOOM! fragments released.", this);
    }

    public void FreezeAllFragments()
    {
        for (int i = 0; i < _fragments.Count; i++)
        {
            var rb = _fragments[i];
            if (!rb) continue;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }
        if (verbose) Debug.Log("[DestructibleDoor] fragments frozen.", this);
    }

    public void EnableAllFragmentColliders()
    {
        for (int i = 0; i < _fragmentColliders.Count; i++)
        {
            var c = _fragmentColliders[i];
            if (!c) continue;
            c.enabled = true;
        }
    }
}
