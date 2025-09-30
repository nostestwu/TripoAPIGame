// UIAudioFeedback.cs —— 给按钮/Toggle等用
using UnityEngine;
using UnityEngine.EventSystems;

public class UIAudioFeedback : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public AudioClip hover;
    public AudioClip click;

    public void OnPointerEnter(PointerEventData e)
    {
        if (AudioManager.Instance)
            AudioManager.Instance.PlayUI(hover ? hover : AudioManager.Instance.uiHover);
    }
    public void OnPointerClick(PointerEventData e)
    {
        if (AudioManager.Instance)
            AudioManager.Instance.PlayUI(click ? click : AudioManager.Instance.uiClick);
    }
}
