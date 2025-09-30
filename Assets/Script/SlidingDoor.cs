using UnityEngine;

public class SlidingDoor : MonoBehaviour
{
    [Header("Door Panels")]
    public Transform leftDoor;
    public Transform rightDoor;

    [Header("Slide")]
    public Vector3 localSlideAxis = Vector3.right; // 门沿哪个本地方向滑动
    public float openDistance = 1.2f;              // 打开时每扇门移动的距离
    public float openSpeed = 2.0f;                 // 打开速度
    public float closeSpeed = 6.0f;                // 关闭速度（更快）

    // 运行态
    Vector3 leftClosed, rightClosed;
    Vector3 leftOpen, rightOpen;
    bool targetOpen;

    void Awake()
    {
        if (!leftDoor || !rightDoor)
        {
            Debug.LogError("[SlidingDoor] 请指定左右门板 Transform");
            enabled = false; return;
        }

        // 记录初始“关门”位置
        leftClosed = leftDoor.localPosition;
        rightClosed = rightDoor.localPosition;

        // 计算“开门”目标位置（两侧相反方向）
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

        // 用 MoveTowards / Lerp 平滑推进即可（“t”是插值因子） 
        leftDoor.localPosition = Vector3.MoveTowards(leftDoor.localPosition, lTarget, spd * Time.deltaTime);
        rightDoor.localPosition = Vector3.MoveTowards(rightDoor.localPosition, rTarget, spd * Time.deltaTime);
        // （Unity 的 Lerp/MoveTowards 常用于平滑位移插值）:contentReference[oaicite:2]{index=2}
    }
}
