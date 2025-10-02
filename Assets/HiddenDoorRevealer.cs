using UnityEngine;

public class HiddenDoorRevealer : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("���������������Ĵ�����")]
    public HoopTrigger[] hoops;
    [Tooltip("�����ţ�����Ĵ��������������壩����ʼʱ��Ҫ����")]
    public GameObject hiddenDoorRoot;

    int _total, _green;

    void Awake()
    {
        if (hiddenDoorRoot) hiddenDoorRoot.SetActive(false); // ������:contentReference[oaicite:5]{index=5}

        _total = (hoops != null) ? hoops.Length : 0;
        _green = 0;

        if (hoops != null)
        {
            foreach (var h in hoops)
            {
                if (!h) continue;
                // ����һ��
                h.onScored.AddListener(OnHoopScored);
            }
        }
    }

    void OnHoopScored()
    {
        _green++;
        if (_green >= _total && hiddenDoorRoot)
        {
            hiddenDoorRoot.SetActive(true);   // ȫ������ �� ��ʾ�ţ��ں���Ĵ����ţ�:contentReference[oaicite:6]{index=6}
        }
    }
}
