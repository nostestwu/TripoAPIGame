// AudioManager.cs  —— 放在 Bootstrap 场景里常驻
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer")]
    public AudioMixer mixer;                 // 拖 GameMixer
    public AudioMixerGroup musicGroup;       // 拖 Music
    public AudioMixerGroup sfxGroup;         // 拖 SFX
    public AudioMixerGroup uiGroup;          // 拖 UI

    [Header("SFX Library (defaults)")]
    public AudioClip uiHover;
    public AudioClip uiClick;
    public AudioClip spawnClip;
    public AudioClip teleportClip;
    public AudioClip doorOpenClip;
    public AudioClip deadClip;
    public AudioClip victoryClip;

    [Header("Pool")]
    public int sfxVoices = 12;               // 同时最多多少个 SFX
    public int uiVoices = 6;                 // 同时最多多少个 UI 音

    AudioSource _musicA, _musicB;            // 做跨淡入淡出
    bool _usingA = true;
    readonly Queue<AudioSource> _sfxPool = new();
    readonly Queue<AudioSource> _uiPool = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 1) BGM 双声道交叉淡入
        _musicA = CreateOneShotSource("MusicA", musicGroup, spatial: false);
        _musicB = CreateOneShotSource("MusicB", musicGroup, spatial: false);
        _musicA.loop = _musicB.loop = true;

        // 2) SFX 池（3D）
        for (int i = 0; i < sfxVoices; i++)
            _sfxPool.Enqueue(CreateOneShotSource("SFX_" + i, sfxGroup, spatial: true));

        // 3) UI 池（2D）
        for (int i = 0; i < uiVoices; i++)
            _uiPool.Enqueue(CreateOneShotSource("UI_" + i, uiGroup, spatial: false));
    }

    AudioSource CreateOneShotSource(string name, AudioMixerGroup group, bool spatial)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var src = go.AddComponent<AudioSource>();
        src.outputAudioMixerGroup = group;
        src.playOnAwake = false;
        src.spatialBlend = spatial ? 1f : 0f;    // 3D or 2D
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = 1.5f; src.maxDistance = 25f;
        return src;
    }

    // ---------- Music ----------
    public void PlayBGM(AudioClip clip, float fade = 0.75f, float volume = 1f)
    {
        if (!clip) return;
        var from = _usingA ? _musicA : _musicB;
        var to = _usingA ? _musicB : _musicA;
        _usingA = !_usingA;

        to.clip = clip;
        to.volume = 0f;
        to.Play();
        StopAllCoroutines();
        StartCoroutine(FadeCross(from, to, fade, volume));
    }
    System.Collections.IEnumerator FadeCross(AudioSource from, AudioSource to, float t, float targetVol)
    {
        float timer = 0f;
        float fromStart = from ? from.volume : 0f;
        while (timer < t)
        {
            timer += Time.unscaledDeltaTime;
            float k = t <= 0f ? 1f : Mathf.Clamp01(timer / t);
            if (from) from.volume = Mathf.Lerp(fromStart, 0f, k);
            to.volume = Mathf.Lerp(0f, targetVol, k);
            yield return null;
        }
        if (from) { from.Stop(); from.volume = 1f; }
        to.volume = targetVol;
    }

    // ---------- SFX (3D) ----------
    public void PlaySFX(AudioClip clip, Vector3 pos, float vol = 1f)
    {
        if (!clip || _sfxPool.Count == 0) return;
        var src = _sfxPool.Dequeue();
        src.transform.position = pos;
        src.volume = vol;
        src.PlayOneShot(clip); // 官方用于短音效的API。:contentReference[oaicite:4]{index=4}
        StartCoroutine(ReturnWhenDone(src, clip.length, _sfxPool));
    }

    // ---------- UI (2D) ----------
    public void PlayUI(AudioClip clip, float vol = 1f)
    {
        if (!clip || _uiPool.Count == 0) return;
        var src = _uiPool.Dequeue();
        src.volume = vol;
        src.PlayOneShot(clip); // 同上。:contentReference[oaicite:5]{index=5}
        StartCoroutine(ReturnWhenDone(src, clip.length, _uiPool));
    }

    System.Collections.IEnumerator ReturnWhenDone(AudioSource s, float dur, Queue<AudioSource> pool)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0.02f, dur));
        pool.Enqueue(s);
    }

    // ---------- Volume (dB) ----------
    public void SetMasterDb(float db) => mixer?.SetFloat("MasterVol", db); // 需要你在Mixer里Expose这些参数
    public void SetMusicDb(float db) => mixer?.SetFloat("MusicVol", db);
    public void SetSfxDb(float db) => mixer?.SetFloat("SFXVol", db);
    public void SetUiDb(float db) => mixer?.SetFloat("UIVol", db);
}
