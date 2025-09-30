using System.Collections;
using UnityEngine;

public class PlayerRagdollController : MonoBehaviour
{
    [Header("Main Parts")]
    public Animator animator;           // ����� Animator
    public Rigidbody mainRb;            // ��������壨��ɫ�����ã�
    public Collider mainCollider;       // �������ײ�壨���罺�ң�
    public Transform ragdollRoot;       // Ragdoll �Ǽܸ�

    [Header("Optional: �ܵ����ƵĽű�������ʱ���ã�")]
    public MonoBehaviour[] movementScriptsToDisable;

    Rigidbody[] ragRBs;
    Collider[] ragCols;

    public CharacterController charController;

    void Awake()
    {
        if (!ragdollRoot) ragdollRoot = transform;

        // �ҵ����С�֫���������ײ�塱���ų�������/����ײ�壩
        ragRBs = ragdollRoot.GetComponentsInChildren<Rigidbody>(true);
        ragCols = ragdollRoot.GetComponentsInChildren<Collider>(true);

        // ��ʼʱĬ�ϡ��� ragdoll��״̬
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

        // �ȴ� ragdoll ״̬�˳����ٴ��Ͳ������ѹ������ơ����뵽������
        SetRagdoll(false);

        if (respawnPoint)
            transform.SetPositionAndRotation(respawnPoint.position, respawnPoint.rotation);

        // �ؼ���ǿ�� Animator ������Ч���������һ֡ ragdoll ����
        SnapToAnimatorPose();

        // �����ٶȸ�����
        if (mainRb)
        {
            mainRb.velocity = Vector3.zero;
            mainRb.angularVelocity = Vector3.zero;
        }

        // ��֤��������֪�����Ǹղŵ�˲��
        Physics.SyncTransforms();
    }


    public void SetRagdoll(bool on)
    {
        // �˶�/����ű���ragdoll ʱ����
        if (movementScriptsToDisable != null)
            foreach (var s in movementScriptsToDisable) if (s) s.enabled = !on;

        // ������� CharacterController��û�и��壩��ragdoll ʱ�����ص�
        if (charController) charController.enabled = !on;

        // Animator��ragdoll ʱ�رգ����ض���ʱ����
        if (animator) animator.enabled = !on;

        // ����ײ��/������
        if (mainCollider) mainCollider.enabled = !on;
        if (mainRb)
        {
            mainRb.velocity = Vector3.zero;
            mainRb.angularVelocity = Vector3.zero;
            mainRb.isKinematic = on;   // ����ʱ�������岻��������
        }

        // ֫�����/��ײ��
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

        // �� ragdoll �лض����ĵ�֡�����̰Ѷ�������Ӧ�õ����������⡰��һ�¡�
        if (!on) SnapToAnimatorPose();
    }

    // �� Animator �������°󶨲��ѵ�ǰ��������Ӧ�õ�����
    void SnapToAnimatorPose()
    {
        if (!animator) return;

        // �ٷ��ĵ��Ƽ���Rebind ʹ Animator ����ץȡ������
        animator.Rebind();
        // ��������һ֡��delta=0 Ҳ��ˢ�µ���ǰ״̬��������˲����뵽��������
        animator.Update(0f);

        // ����ѡ�������� Animator �ڡ�Animate Physics��ģʽ������ȷ�������붯��ͬ��
        Physics.SyncTransforms();
    }

}
