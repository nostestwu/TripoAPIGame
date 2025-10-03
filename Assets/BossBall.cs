using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class BossBall : MonoBehaviour
{
    public float speed = 20f;
    public float lifeTime = 8f;
    public LayerMask hitMask = ~0;

    public GameObject hitVfxPrefab;
    public float hitVfxLife = 3f;
    public string playerTag = "Player";

    // 这里我们把 defaultRespawnPoint 改为可以动态查 PlayerSpawn
    public Transform respawnPointOverride; // Inspector 可指定
    Transform _defaultRespawnPointCached;

    Rigidbody _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    public void Launch(Vector3 dir)
    {
        _rb.velocity = dir.normalized * speed;
        Destroy(gameObject, lifeTime);
    }

    void OnCollisionEnter(Collision c)
    {
        // 命中玩家
        if (c.collider.CompareTag(playerTag))
        {
            var rag = c.collider.GetComponentInParent<PlayerRagdollController>();
            if (rag != null)
            {
                // 拿复活点：优先 override，其次查场景中的 PlayerSpawn
                Transform rp = respawnPointOverride;
                if (rp == null)
                {
                    // 缓存一次搜索
                    if (_defaultRespawnPointCached == null)
                    {
                        var ps = GameObject.FindObjectOfType<PlayerSpawn>();
                        if (ps != null) _defaultRespawnPointCached = ps.transform;
                    }
                    rp = _defaultRespawnPointCached;
                }
                rag.KnockoutAndRespawn(rp, 2f);
            }
        }

        if (hitVfxPrefab)
        {
            var vfx = Instantiate(hitVfxPrefab, c.GetContact(0).point, Quaternion.identity);
            Destroy(vfx, hitVfxLife);
        }

        Destroy(gameObject);
    }
}
