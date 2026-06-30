using Cysharp.Threading.Tasks;
using UnityEngine;

public enum AudioCueId
{
    None = 0,

    IntroBgm,
    BattleBgm,

    UiClick = 10,
    UiConfirm,
    UiMove,
    PageChange,
    TurnChange,

    PlayerAttack = 50,
    PlayerAttack_2,
    PlayerAttackHeavy,

    MonsterAttack = 100,
    MonsterStrongAttack,

    ParryCue = 200,
    ParrySuccess,
    ParryCounterStart,
    ParryCounterImpact,
    ParrySlashImpact,
}

public sealed class AudioManager : MonoBehaviour
{
    public static AudioManager Instance
    {
        get; private set;
    }

    [Header("Data")]
    [SerializeField]
    private AudioData _audioData;

    [Header("Audio Sources")]
    [SerializeField]
    private AudioSource _bgmSource;

    [SerializeField]
    private AudioSource _sfxSourceA;

    [SerializeField]
    private AudioSource _sfxSourceB;

    private int _sfxIndex;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeSources();
    }

    private void InitializeSources()
    {
        if (_bgmSource == null)
        {
            _bgmSource = gameObject.AddComponent<AudioSource>();
        }

        if (_sfxSourceA == null)
        {
            _sfxSourceA = gameObject.AddComponent<AudioSource>();
        }

        if (_sfxSourceB == null)
        {
            _sfxSourceB = gameObject.AddComponent<AudioSource>();
        }

        _bgmSource.playOnAwake = false;
        _bgmSource.loop = true;

        _sfxSourceA.playOnAwake = false;
        _sfxSourceA.loop = false;

        _sfxSourceB.playOnAwake = false;
        _sfxSourceB.loop = false;
    }

    public void PlayBgm(AudioCueId id)
    {
        if (TryGetCue(id, out AudioClip clip, out float volume, out float pitch) == false)
        {
            return;
        }

        if (_bgmSource.clip == clip && _bgmSource.isPlaying)
        {
            return;
        }

        _bgmSource.clip = clip;
        _bgmSource.volume = volume;
        _bgmSource.pitch = pitch;
        _bgmSource.Play();
    }

    public async UniTaskVoid ChangeBgmAsync(AudioCueId id, float fadeDuration = 0.5f)
    {
        if (TryGetCue(id, out AudioClip clip, out float volume, out float pitch) == false)
        {
            return;
        }

        if (_bgmSource.clip == clip && _bgmSource.isPlaying)
        {
            return;
        }

        float startVolume = _bgmSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            _bgmSource.volume = Mathf.Lerp(startVolume, 0f, t);
            await Cysharp.Threading.Tasks.UniTask.Yield();
        }

        _bgmSource.Stop();
        _bgmSource.clip = clip;
        _bgmSource.pitch = pitch;
        _bgmSource.volume = 0f;
        _bgmSource.Play();

        elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            _bgmSource.volume = Mathf.Lerp(0f, volume, t);
            await Cysharp.Threading.Tasks.UniTask.Yield();
        }

        _bgmSource.volume = volume;
    }

    public void StopBgm()
    {
        if (_bgmSource == null)
        {
            return;
        }

        _bgmSource.Stop();
        _bgmSource.clip = null;
    }

    public void PlaySfx(AudioCueId id)
    {
        if (TryGetCue(id, out AudioClip clip, out float volume, out float pitch) == false)
        {
            return;
        }

        AudioSource source = GetNextSfxSource();

        if (source == null)
        {
            return;
        }

        source.pitch = pitch;
        source.volume = volume;
        source.PlayOneShot(clip);
    }

    public void PlaySfx(AudioCueId id, float volumeMultiplier, float pitchMultiplier = 1f)
    {
        if (TryGetCue(id, out AudioClip clip, out float volume, out float pitch) == false)
        {
            return;
        }

        AudioSource source = GetNextSfxSource();

        if (source == null)
        {
            return;
        }

        source.pitch = pitch * pitchMultiplier;
        source.volume = Mathf.Clamp01(volume * volumeMultiplier);
        source.PlayOneShot(clip);
    }

    private bool TryGetCue(
        AudioCueId id,
        out AudioClip clip,
        out float volume,
        out float pitch)
    {
        clip = null;
        volume = 1f;
        pitch = 1f;

        if (_audioData == null)
        {
            Debug.LogWarning("[AudioManager] AudioData is missing.");
            return false;
        }

        return _audioData.TryGetCue(id, out clip, out volume, out pitch);
    }

    private AudioSource GetNextSfxSource()
    {
        AudioSource source = _sfxIndex % 2 == 0
            ? _sfxSourceA
            : _sfxSourceB;

        _sfxIndex++;

        return source;
    }
}