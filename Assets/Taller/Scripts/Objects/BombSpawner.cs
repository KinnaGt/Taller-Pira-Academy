using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

public class BombSpawner : MonoBehaviour, IInteractable
{
    [Header("Lever Settings")]
    [SerializeField]
    private SpriteRenderer _leverRenderer;

    [SerializeField]
    private Sprite _leverOnSprite;

    [SerializeField]
    private Sprite _leverOffSprite;

    [SerializeField]
    private bool _isOn = false;

    [Header("Spawn Settings")]
    [SerializeField]
    private RangeDetector _rangeDetector;

    [SerializeField]
    private Bomb _bombPrefab;

    [SerializeField]
    private Transform _spawnPoint;

    [SerializeField]
    private float _spawnInterval = 2f;

    [SerializeField]
    private float _initialDelay = 1f;

    [Header("Pool Settings")]
    [SerializeField]
    private int _defaultCapacity = 10;

    [SerializeField]
    private int _maxSize = 20;

    private IObjectPool<Bomb> _bombPool;
    private Coroutine _spawnRoutine;

    [Header("Lever VFX")]
    [SerializeField]
    private SpriteRenderer _leverVfxRenderer;

    [SerializeField]
    private Sprite _vfxOnSprite;

    [SerializeField]
    private Sprite _vfxOffSprite;

    [SerializeField]
    private float _vfxFloatDistance = 1.2f;

    [SerializeField]
    private float _vfxDuration = 0.7f;

    [SerializeField]
    private AudioSource _audioSource;

    [SerializeField]
    private AudioClip _leverOnClip;

    [SerializeField]
    private AudioClip _leverOffClip;

    private Coroutine _vfxRoutine;
    private Vector3 _vfxRestPosition;

    private void Awake()
    {
        _bombPool = new ObjectPool<Bomb>(
            createFunc: CreateBomb,
            actionOnGet: OnGetBomb,
            actionOnRelease: OnReleaseBomb,
            actionOnDestroy: OnDestroyBomb,
            collectionCheck: false,
            defaultCapacity: _defaultCapacity,
            maxSize: _maxSize
        );

        if (_leverVfxRenderer != null)
            _vfxRestPosition = _leverVfxRenderer.transform.localPosition;
    }

    private void Start()
    {
        UpdateLeverVisuals();
        _spawnRoutine = StartCoroutine(SpawnRoutine());
    }

    private void ToggleLever()
    {
        _isOn = !_isOn;
        UpdateLeverVisuals();
        PlayLeverFeedback();
    }

    private void PlayLeverFeedback()
    {
        if (_audioSource != null)
        {
            AudioClip clip = _isOn ? _leverOnClip : _leverOffClip;
            if (clip != null)
                _audioSource.PlayOneShot(clip);
        }

        if (_vfxRoutine != null)
            StopCoroutine(_vfxRoutine);
        if (_leverVfxRenderer != null)
            _vfxRoutine = StartCoroutine(LeverVfxRoutine());
    }

    private IEnumerator LeverVfxRoutine()
    {
        _leverVfxRenderer.sprite = _isOn ? _vfxOnSprite : _vfxOffSprite;
        _leverVfxRenderer.color = _isOn
            ? new Color(0.3f, 1f, 0.3f, 1f)
            : new Color(1f, 0.3f, 0.3f, 1f);
        _leverVfxRenderer.enabled = true;
        _leverVfxRenderer.transform.localPosition = _vfxRestPosition;
        Vector3 targetPos = _vfxRestPosition + Vector3.up * _vfxFloatDistance;
        float elapsed = 0f;

        while (elapsed < _vfxDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _vfxDuration;

            _leverVfxRenderer.transform.localPosition = Vector3.Lerp(
                _vfxRestPosition,
                targetPos,
                Mathf.SmoothStep(0f, 1f, t)
            );
            float alpha = t < 0.5f ? 1f : 1f - ((t - 0.5f) / 0.5f);
            Color c = _leverVfxRenderer.color;
            c.a = alpha;
            _leverVfxRenderer.color = c;

            yield return null;
        }

        _leverVfxRenderer.transform.localPosition = _vfxRestPosition;
        _leverVfxRenderer.color = Color.clear;
        _vfxRoutine = null;
    }

    private IEnumerator SpawnRoutine()
    {
        if (_initialDelay > 0f)
            yield return new WaitForSeconds(_initialDelay);

        WaitForSeconds wait = new WaitForSeconds(_spawnInterval);

        while (true)
        {
            if (_isOn)
                _bombPool.Get();

            yield return wait;
        }
    }

    public void Interact()
    {
        if (_rangeDetector != null && !_rangeDetector.IsPlayerInRange)
            return;
        ToggleLever();
    }

    private void UpdateLeverVisuals()
    {
        if (_leverRenderer != null)
        {
            _leverRenderer.sprite = _isOn ? _leverOnSprite : _leverOffSprite;
        }
    }

    #region Object Pool Callbacks
    private Bomb CreateBomb()
    {
        Bomb bombInstance = Instantiate(_bombPrefab);
        bombInstance.SetPool(_bombPool);
        return bombInstance;
    }

    private void OnGetBomb(Bomb bomb)
    {
        bomb.transform.position = _spawnPoint.position;
        bomb.transform.rotation = _spawnPoint.rotation;
        bomb.gameObject.SetActive(true);
        bomb.ResetState();
    }

    private void OnReleaseBomb(Bomb bomb)
    {
        bomb.gameObject.SetActive(false);
    }

    private void OnDestroyBomb(Bomb bomb)
    {
        Destroy(bomb.gameObject);
    }
    #endregion
}
