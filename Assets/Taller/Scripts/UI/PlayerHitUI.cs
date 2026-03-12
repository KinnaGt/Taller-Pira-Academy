using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class PlayerHitUI : MonoBehaviour
{
    [Header("Referencia")]
    [Tooltip("El PlayerController de la escena. Se puede asignar o auto-buscar en Start.")]
    [SerializeField]
    private PlayerController _playerController;

    [Header("Flash")]
    [SerializeField]
    private Color _hitColor = new Color(1f, 0.15f, 0.15f, 1f);

    [SerializeField]
    private float _flashInTime = 0.06f;

    [SerializeField]
    private float _flashHoldTime = 0.08f;

    [SerializeField]
    private float _flashOutTime = 0.18f;

    [Header("Shake")]
    [SerializeField]
    private float _shakeMagnitude = 18f;

    [SerializeField]
    private float _shakeDuration = 0.35f;

    [SerializeField]
    private int _shakeFrequency = 18;

    // ─── Estado interno ───────────────────────────────────────────────────────
    private Image _image;
    private Color _baseColor;
    private Vector3 _baseLocalPos;
    private Coroutine _activeFlash;
    private Coroutine _activeShake;

    private bool _baseCaptured;

    // ═══════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        _image = GetComponent<Image>();
        _baseColor = _image.color;
    }

    private void Start()
    {
        if (_playerController == null)
            _playerController = FindFirstObjectByType<PlayerController>();

        if (_playerController == null)
            Debug.LogWarning("[PlayerHitUI] No se encontró PlayerController en la escena.");
    }

    private void OnEnable()
    {
        PlayerController.OnHitPerformed += HandleHit;
    }

    private void OnDisable()
    {
        PlayerController.OnHitPerformed -= HandleHit;
    }

    // ─── Handler ──────────────────────────────────────────────────────────────

    private void HandleHit()
    {
        if (!_baseCaptured)
        {
            _baseLocalPos = transform.localPosition;
            _baseCaptured = true;
        }

        if (_activeFlash != null)
        {
            StopCoroutine(_activeFlash);
            _image.color = _baseColor;
        }
        if (_activeShake != null)
        {
            StopCoroutine(_activeShake);
            transform.localPosition = _baseLocalPos;
        }

        _activeFlash = StartCoroutine(FlashRoutine());
        _activeShake = StartCoroutine(ShakeRoutine());
    }

    // ─── Flash ────────────────────────────────────────────────────────────────

    private IEnumerator FlashRoutine()
    {
        // Fade in → hold → fade out
        yield return LerpColor(_baseColor, _hitColor, _flashInTime);
        yield return new WaitForSeconds(_flashHoldTime);
        yield return LerpColor(_hitColor, _baseColor, _flashOutTime);

        _image.color = _baseColor;
        _activeFlash = null;
    }

    private IEnumerator LerpColor(Color from, Color to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _image.color = Color.Lerp(from, to, EaseOut(elapsed / duration));
            yield return null;
        }
    }

    // ─── Shake ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Shake senoidal que decae en amplitud con el tiempo.
    /// Análogo a un resorte que vibra y pierde energía gradualmente.
    /// </summary>
    private IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;

        while (elapsed < _shakeDuration)
        {
            elapsed += Time.deltaTime;

            // Decaimiento lineal de la amplitud
            float decay = 1f - (elapsed / _shakeDuration);
            float amplitude = _shakeMagnitude * decay;

            // Oscilación senoidal en X, coseno en Y para dar sensación 2D de impacto
            float freqRad = _shakeFrequency * Mathf.PI * 2f;
            float offsetX = Mathf.Sin(elapsed * freqRad) * amplitude;
            float offsetY = Mathf.Cos(elapsed * freqRad * 0.7f) * amplitude * 0.4f;

            transform.localPosition = _baseLocalPos + new Vector3(offsetX, offsetY, 0f);
            yield return null;
        }

        transform.localPosition = _baseLocalPos;
        _activeShake = null;
    }

    // ─── Utils ────────────────────────────────────────────────────────────────

    private static float EaseOut(float t) => 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
}
