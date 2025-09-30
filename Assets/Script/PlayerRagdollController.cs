using System.Collections;
using UnityEngine;

public class PlayerRagdollController : MonoBehaviour
{
    [Header("Main Parts")]
    public Animator animator;           // 玩家主 Animator
    public Rigidbody mainRb;            // 玩家主刚体（角色控制用）
    public Collider mainCollider;       // 玩家主碰撞体（例如胶囊）
    public Transform ragdollRoot;       // Ragdoll 骨架根

    [Header("Optional: 受到控制的脚本（倒地时禁用）")]
    public MonoBehaviour[] movementScriptsToDisable;

    Rigidbody[] ragRBs;
    Collider[] ragCols;

    public CharacterController charController;

    void Awake()
    {
        if (!ragdollRoot) ragdollRoot = transform;

        // 找到所有“肢体刚体与碰撞体”（排除主刚体/主碰撞体）
        ragRBs = ragdollRoot.GetComponentsInChildren<Rigidbody>(true);
        ragCols = ragdollRoot.GetComponentsInChildren<Collider>(true);

        // 开始时默认“非 ragdoll”状态
        SetRagdoll(false);
    }

    public void KnockoutAndRespawn(Transform respawnPoint, float seconds)
    {
        StopAllCoroutines();
        StartCoroutine(KnockoutRoutine(respawnPoint, seconds));
    }

    IEnumerator KnockoutRoutine(Transform respawnPoint, float seconds)
    {
        SetRagdoll(true);
        yield return new WaitForSeconds(seconds);

        // 先从 ragdoll 状态退出，再传送并立即把骨骼姿势“对齐到动画”
        SetRagdoll(false);

        if (respawnPoint)
            transform.SetPositionAndRotation(respawnPoint.position, respawnPoint.rotation);

        // 关键：强制 Animator 立即生效，避免出现一帧 ragdoll 姿势
        SnapToAnimatorPose();

        // 清下速度更稳妥
        if (mainRb)
        {
            mainRb.velocity = Vector3.zero;
            mainRb.angularVelocity = Vector3.zero;
        }

        // 保证物理立即知道我们刚才的瞬移
        Physics.SyncTransforms();
    }


    public void SetRagdoll(bool on)
    {
        // 运动/输入脚本：ragdoll 时禁用
        if (movementScriptsToDisable != null)
            foreach (var s in movementScriptsToDisable) if (s) s.enabled = !on;

        // 如果你用 CharacterController（没有刚体），ragdoll 时把它关掉
        if (charController) charController.enabled = !on;

        // Animator：ragdoll 时关闭，返回动画时开启
        if (animator) animator.enabled = !on;

        // 主碰撞体/主刚体
        if (mainCollider) mainCollider.enabled = !on;
        if (mainRb)
        {
            mainRb.velocity = Vector3.zero;
            mainRb.angularVelocity = Vector3.zero;
            mainRb.isKinematic = on;   // 倒地时让主刚体不参与物理
        }

        // 肢体刚体/碰撞体
        if (ragRBs != null)
            foreach (var rb in ragRBs)
                if (rb && rb != mainRb)
                {
                    rb.isKinematic = !on;
                    if (on) { rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
                }

        if (ragCols != null)
            foreach (var c in ragCols)
                if (c && c != mainCollider) c.enabled = on;

        // 从 ragdoll 切回动画的当帧，立刻把动画姿势应用到骨骼，避免“弹一下”
        if (!on) SnapToAnimatorPose();
    }

    // 让 Animator 立刻重新绑定并把当前动画姿势应用到骨骼
    void SnapToAnimatorPose()
    {
        if (!animator) return;

        // 官方文档推荐：Rebind 使 Animator 重新抓取骨骼绑定
        animator.Rebind();
        // 立刻评估一帧（delta=0 也会刷新到当前状态），骨骼瞬间对齐到动画姿势
        animator.Update(0f);

        // （可选）如果你的 Animator 在“Animate Physics”模式，可以确保物理与动画同步
        Physics.SyncTransforms();
    }

}
