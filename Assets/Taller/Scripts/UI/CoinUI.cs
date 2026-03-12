using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CoinUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField]
    private Image _coinImage;

    [SerializeField]
    private TextMeshProUGUI _coinText;

    [Header("Animación")]
    [Tooltip("Cuánto escala la moneda en el pico del bounce.")]
    [SerializeField]
    private float _bounceScale = 1.4f;

    [Tooltip("Duración total de la animación en segundos.")]
    [SerializeField]
    private float _animDuration = 0.4f;

    [Tooltip("Color del texto al recibir monedas.")]
    [SerializeField]
    private Color _flashColor = new Color(1f, 0.9f, 0.2f);

    // ─── State ───────────────────────────────────────────────────────────────
    private int _currentAmount;
    private Coroutine _activeAnim;

    private Vector3 _imageBaseScale;
    private Color _textBaseColor;

    // ═══════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        _imageBaseScale = _coinImage != null ? _coinImage.transform.localScale : Vector3.one;
        _textBaseColor = _coinText != null ? _coinText.color : Color.white;
    }

    private void OnEnable() => CollectibleItem.OnCollected += HandleCoinCollected;

    private void OnDisable() => CollectibleItem.OnCollected -= HandleCoinCollected;

    // ─── Handler  ───────────────────────────────────────────────────

    private void HandleCoinCollected(int value)
    {
        _currentAmount += value;
        UpdateText();

        if (_activeAnim != null)
            StopCoroutine(_activeAnim);
        _activeAnim = StartCoroutine(PlayPickupAnim());
    }

    // ─── Text ────────────────────────────────────────────────────────────────

    private void UpdateText()
    {
        if (_coinText != null)
            _coinText.text = _currentAmount.ToString();
    }

    // ─── Animation ────────────────────────────────────────────────────────────

    private IEnumerator PlayPickupAnim()
    {
        float halfDuration = _animDuration * 0.5f;
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;

            SetImageScale(Mathf.Lerp(1f, _bounceScale, EaseOut(t)));
            SetTextColor(Color.Lerp(_textBaseColor, _flashColor, EaseOut(t)));

            yield return null;
        }

        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;

            SetImageScale(Mathf.Lerp(_bounceScale, 1f, EaseOut(t)));
            SetTextColor(Color.Lerp(_flashColor, _textBaseColor, EaseOut(t)));

            yield return null;
        }

        SetImageScale(1f);
        SetTextColor(_textBaseColor);
        _activeAnim = null;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private void SetImageScale(float scale)
    {
        if (_coinImage != null)
            _coinImage.transform.localScale = _imageBaseScale * scale;
    }

    private void SetTextColor(Color color)
    {
        if (_coinText != null)
            _coinText.color = color;
    }

    private static float EaseOut(float t) => 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
}
