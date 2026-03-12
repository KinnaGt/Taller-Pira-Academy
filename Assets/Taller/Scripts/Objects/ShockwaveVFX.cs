using System;
using System.Collections;
using UnityEngine;

public class ShockwaveVFX : MonoBehaviour
{
    [SerializeField]
    private float _maxScale = 6f;

    [SerializeField]
    private float _duration = 0.35f;

    [SerializeField]
    private Color _startColor = new Color(1f, 0.6f, 0.1f, 0.8f);

    private SpriteRenderer _sr;

    private CircleCollider2D _col;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _col = GetComponent<CircleCollider2D>();
    }

    public void Play(Action onComplete)
    {
        gameObject.SetActive(true);
        if (_col != null)
            _col.enabled = true;
        StartCoroutine(ShockwaveRoutine(onComplete));
    }

    private IEnumerator ShockwaveRoutine(Action onComplete)
    {
        float elapsed = 0f;
        Color color = _startColor;

        while (elapsed < _duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _duration;

            transform.localScale = Vector3.one * Mathf.Lerp(0f, _maxScale, Mathf.Sqrt(t));
            color.a = Mathf.Lerp(_startColor.a, 0f, t);
            _sr.color = color;

            yield return null;
        }

        gameObject.SetActive(false);
        transform.localScale = Vector3.zero;
        _sr.color = Color.clear;
        if (_col != null)
            _col.enabled = false;

        onComplete?.Invoke();
    }
}
