using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class Bomb : MonoBehaviour
{
    [Header("Visual Feedback")]
    [SerializeField]
    private SpriteRenderer _spriteRenderer;

    [SerializeField]
    private Sprite _defaultSprite;

    [SerializeField]
    private Sprite _warningSprite;

    [SerializeField]
    private float _blinkInterval = 0.2f;

    [Header("Fuse Settings")]
    [SerializeField]
    private float _fuseTime = 2f;

    [SerializeField]
    private LayerMask _groundLayer;

    [Header("Spawn Animation")]
    [SerializeField]
    private AnimationCurve _spawnCurve;

    [SerializeField]
    private float _spawnDuration = 0.35f;

    [Header("Explosion FX")]
    [SerializeField]
    private ShockwaveVFX _shockwaveVFX;

    [Header("Screen Shake")]
    [SerializeField]
    private float _shakeDuration = 0.3f;

    [SerializeField]
    private float _shakeMagnitude = 0.25f;

    private bool _isTriggered;
    private IObjectPool<Bomb> _pool;
    private Rigidbody2D _rb;

    private void Awake()
    {
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();

        TryGetComponent(out _rb);
    }

    public void SetPool(IObjectPool<Bomb> pool) => _pool = pool;

    public void ResetState()
    {
        _isTriggered = false;
        _spriteRenderer.sprite = _defaultSprite;
        _spriteRenderer.color = Color.white;
        StopAllCoroutines();

        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
        }

        transform.localScale = Vector3.zero;
        StartCoroutine(SpawnAnimationRoutine());
    }

    #region Spawn Animation

    private IEnumerator SpawnAnimationRoutine()
    {
        float elapsed = 0f;

        while (elapsed < _spawnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _spawnDuration);
            transform.localScale = Vector3.one * _spawnCurve.Evaluate(t);
            yield return null;
        }

        transform.localScale = Vector3.one;
    }

    #endregion

    #region Fuse

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_isTriggered)
            return;

        if (((1 << collision.gameObject.layer) & _groundLayer.value) != 0)
        {
            _isTriggered = true;
            StartCoroutine(FuseSequenceRoutine());
        }
    }

    private IEnumerator FuseSequenceRoutine()
    {
        float elapsedTime = 0f;
        float blinkTimer = 0f;
        bool isWarningSpriteActive = false;

        while (elapsedTime < _fuseTime)
        {
            float dt = Time.deltaTime;
            elapsedTime += dt;
            blinkTimer += dt;

            if (blinkTimer >= _blinkInterval)
            {
                blinkTimer = 0f;
                isWarningSpriteActive = !isWarningSpriteActive;
                _spriteRenderer.sprite = isWarningSpriteActive ? _warningSprite : _defaultSprite;
            }

            yield return null;
        }

        Detonate();
    }

    #endregion

    #region Detonation

    private void Detonate()
    {
        CameraShake.Instance?.ShakeFromPosition(
            transform.position,
            _shakeDuration,
            _shakeMagnitude
        );

        _spriteRenderer.enabled = false;

        if (_shockwaveVFX != null)
        {
            _shockwaveVFX.Play(() =>
            {
                _spriteRenderer.enabled = true;
                ReturnToPool();
            });
        }
        else
        {
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        if (_pool != null)
            _pool.Release(this);
        else
            Destroy(gameObject);
    }

    #endregion
}
