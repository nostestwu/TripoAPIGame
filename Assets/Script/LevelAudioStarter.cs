// LevelAudioStarter.cs —— 丢到 MainMenu & 每个 Level 场景
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
