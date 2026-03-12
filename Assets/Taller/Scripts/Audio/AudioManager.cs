using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

public class AudioManager : MonoBehaviour
{
    [Header("Event Channels")]
    [SerializeField]
    private AudioEventChannelSO _sfxEventChannel;

    [SerializeField]
    private AudioEventChannelSO _bgmEventChannel;

    [Header("BGM Source")]
    [SerializeField]
    private AudioSource _bgmSource;

    [Header("SFX Pool Settings")]
    [SerializeField]
    private int _defaultPoolSize = 10;

    [SerializeField]
    private int _maxPoolSize = 20;

    private ObjectPool<AudioSource> _audioSourcePool;

    private void Awake()
    {
        InitializePool();
        if (_bgmSource == null)
            _bgmSource = gameObject.AddComponent<AudioSource>();
    }

    private void OnEnable()
    {
        if (_sfxEventChannel != null)
            _sfxEventChannel.OnAudioRequested += PlaySFX;
        if (_bgmEventChannel != null)
            _bgmEventChannel.OnAudioRequested += PlayBGM;
    }

    private void OnDisable()
    {
        if (_sfxEventChannel != null)
            _sfxEventChannel.OnAudioRequested -= PlaySFX;
        if (_bgmEventChannel != null)
            _bgmEventChannel.OnAudioRequested -= PlayBGM;
    }

    private void InitializePool()
    {
        _audioSourcePool = new ObjectPool<AudioSource>(
            createFunc: CreateAudioSource,
            actionOnGet: source => source.gameObject.SetActive(true),
            actionOnRelease: source =>
            {
                source.gameObject.SetActive(false);
                source.clip = null;
                source.outputAudioMixerGroup = null;
            },
            actionOnDestroy: source => Destroy(source.gameObject),
            collectionCheck: false,
            defaultCapacity: _defaultPoolSize,
            maxSize: _maxPoolSize
        );
    }

    private AudioSource CreateAudioSource()
    {
        GameObject go = new GameObject("PooledAudioSource");
        go.transform.SetParent(transform);
        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        return source;
    }

    private void PlaySFX(AudioConfigSO config)
    {
        AudioSource source = _audioSourcePool.Get();

        source.clip = config.Clip;
        source.outputAudioMixerGroup = config.MixerGroup;
        source.volume = config.Volume;
        source.pitch = Random.Range(config.MinPitch, config.MaxPitch);
        source.loop = config.Loop;

        source.Play();

        if (!source.loop)
            StartCoroutine(ReleaseSourceRoutine(source));
    }

    private IEnumerator ReleaseSourceRoutine(AudioSource source)
    {
        yield return new WaitForSeconds(source.clip.length / Mathf.Abs(source.pitch));
        _audioSourcePool.Release(source);
    }

    private void PlayBGM(AudioConfigSO config)
    {
        if (_bgmSource.clip == config.Clip && _bgmSource.isPlaying)
            return;

        _bgmSource.clip = config.Clip;
        _bgmSource.outputAudioMixerGroup = config.MixerGroup;
        _bgmSource.volume = config.Volume;
        _bgmSource.pitch = 1f;
        _bgmSource.loop = config.Loop;

        _bgmSource.Play();
    }
}
