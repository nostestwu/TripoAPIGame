// LevelAudioStarter.cs ���� ���� MainMenu & ÿ�� Level ����
using UnityEngine;

public class LevelAudioStarter : MonoBehaviour
{
    public AudioClip bgm;
    public float fadeIn = 0.75f;

    void Start()
    {
        if (AudioManager.Instance && bgm)
            AudioManager.Instance.PlayBGM(bgm, fadeIn, 1f);
    }
}
