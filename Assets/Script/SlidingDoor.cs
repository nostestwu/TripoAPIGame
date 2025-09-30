using UnityEngine;

public class SlidingDoor : MonoBehaviour
{
    [Header("Door Panels")]
    public Transform leftDoor;
    public Transform rightDoor;

    [Header("Slide")]
    public Vector3 localSlideAxis = Vector3.right; // �����ĸ����ط��򻬶�
    public float openDistance = 1.2f;              // ��ʱÿ�����ƶ��ľ���
    public float openSpeed = 2.0f;                 // ���ٶ�
    public float closeSpeed = 6.0f;                // �ر��ٶȣ����죩

    // ����̬
    Vector3 leftClosed, rightClosed;
    Vector3 leftOpen, rightOpen;
    bool targetOpen;

    void Awake()
    {
        if (!leftDoor || !rightDoor)
        {
            Debug.LogError("[SlidingDoor] ��ָ�������Ű� Transform");
            enabled = false; return;
        }

        // ��¼��ʼ�����š�λ��
        leftClosed = leftDoor.localPosition;
        rightClosed = rightDoor.localPosition;

        // ���㡰���š�Ŀ��λ�ã������෴����
        Vector3 axis = localSlideAxis.normalized;
        leftOpen = leftClosed + axis * openDistance;
        rightOpen = rightClosed - axis * openDistance;
    }

    public void SetOpen(bool open)
    {
        targetOpen = open;
    }

    void Update()
    {
        float spd = targetOpen ? openSpeed : closeSpeed;
        Vector3 lTarget = targetOpen ? leftOpen : leftClosed;
        Vector3 rTarget = targetOpen ? rightOpen : rightClosed;

        // �� MoveTowards / Lerp ƽ���ƽ����ɣ���t���ǲ�ֵ���ӣ� 
        leftDoor.localPosition = Vector3.MoveTowards(leftDoor.localPosition, lTarget, spd * Time.deltaTime);
        rightDoor.localPosition = Vector3.MoveTowards(rightDoor.localPosition, rTarget, spd * Time.deltaTime);
        // ��Unity �� Lerp/MoveTowards ������ƽ��λ�Ʋ�ֵ��:contentReference[oaicite:2]{index=2}
    }
}
