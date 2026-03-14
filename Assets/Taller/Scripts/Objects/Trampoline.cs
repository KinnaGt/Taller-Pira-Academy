using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Trampoline : MonoBehaviour
{
    [Header("Configuración Mecánica")]
    [SerializeField]
    private float bounceForce = 25f;

    [SerializeField]
    private LayerMask playerLayer;

    [Header("Configuración Visual")]
    [SerializeField]
    private Sprite idleSprite;

    [SerializeField]
    private Sprite activeSprite;

    [Tooltip("Tiempo en segundos que se mantiene el sprite 'Active'")]
    [SerializeField]
    private float activeDuration = 0.2f;

    [Header("Feedback")]
    [SerializeField]
    private GameObject bounceVFXPrefab;

    private SpriteRenderer _sr;
    private Coroutine _resetRoutine;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        GetComponent<BoxCollider2D>().isTrigger = true;

        if (idleSprite != null)
            _sr.sprite = idleSprite;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if ((playerLayer.value & (1 << collision.gameObject.layer)) > 0)
        {
            var player = collision.gameObject.GetComponentInParent<PlayerController>();

            if (player != null)
            {
                player.Bounce(bounceForce);

                ActivateVisuals();

                if (bounceVFXPrefab != null)
                    Instantiate(bounceVFXPrefab, transform.position, Quaternion.identity);
            }
        }
    }

    private void ActivateVisuals()
    {
        if (activeSprite == null)
            return;

        _sr.sprite = activeSprite;

        if (_resetRoutine != null)
            StopCoroutine(_resetRoutine);
        _resetRoutine = StartCoroutine(ResetSpriteRoutine());
    }

    private IEnumerator ResetSpriteRoutine()
    {
        yield return new WaitForSeconds(activeDuration);
        _sr.sprite = idleSprite;
        _resetRoutine = null;
    }
}
