using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class BossBall : MonoBehaviour
{
    [Header("Motion")]
    public float speed = 20f;
    public float lifeTime = 8f;               // 防止飞太远不回收
    public LayerMask hitMask = ~0;

    [Header("On Hit")]
    public GameObject hitVfxPrefab;
    public float hitVfxLife = 3f;
    public string playerTag = "Player";
    public Transform defaultRespawnPoint;     // ← 改成这个名字

    Rigidbody _rb;
    Vector3 _vel;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    public void Launch(Vector3 dir)
    {
        _vel = (dir.sqrMagnitude > 0.0001f ? dir.normalized : transform.forward) * speed;
        _rb.velocity = _vel;
        Destroy(gameObject, lifeTime);
    }

    void OnCollisionEnter(Collision c)
    {
        if (c.collider.CompareTag(playerTag))
        {
            var rag = c.collider.GetComponentInParent<PlayerRagdollController>();
            if (rag != null)
            {
                Transform respawn = defaultRespawnPoint;
                rag.KnockoutAndRespawn(respawn, 2f);
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
