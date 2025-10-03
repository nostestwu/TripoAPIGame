using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemFixer : MonoBehaviour
{
    void Awake()
    {
        EventSystem[] systems = FindObjectsOfType<EventSystem>();
        if (systems.Length > 1)
        {
            // ������һ�������� /��������
            for (int i = 1; i < systems.Length; i++)
            {
                Debug.Log("[EventSystemFixer] Destroying extra EventSystem: " + systems[i].gameObject.name);
                Destroy(systems[i].gameObject);
            }
        }
        // ���ߣ�disable instead of destroy

        var listeners = FindObjectsOfType<AudioListener>();
        if (listeners.Length > 1)
        {
            // ������һ������������
            for (int i = 1; i < listeners.Length; i++)
            {
                Destroy(listeners[i]);
            }
        }
    }
}
