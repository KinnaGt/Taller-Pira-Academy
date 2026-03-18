using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SimplePatrolEnemy : MonoBehaviour
{
    [Header("Configuración de Ruta")]
    [Tooltip("Transform que define el punto A. Puede ser un hijo del enemigo.")]
    [SerializeField]
    private Transform pointA;

    [Tooltip("Transform que define el punto B. Puede ser un hijo del enemigo.")]
    [SerializeField]
    private Transform pointB;

    [Header("Movimiento")]
    [SerializeField, Range(0f, 10f)]
    private float speed = 3f;

    [SerializeField, Range(0f, 5f)]
    private float waitTimeAtEdge = 1f;

    [SerializeField]
    private float threshold = 0.2f;

    [Header("Squash & Stretch")]
    [Tooltip("Qué tan aplastado se ve al girar (menor = más squash).")]
    [SerializeField, Range(0.3f, 0.9f)]
    private float squashX = 0.6f;

    [Tooltip("Qué tan estirado se ve al girar (mayor = más stretch).")]
    [SerializeField, Range(1.1f, 2f)]
    private float stretchY = 1.3f;

    [Tooltip("Duración de la animación de squash en segundos.")]
    [SerializeField, Range(0.05f, 0.5f)]
    private float squashDuration = 0.15f;

    private Rigidbody2D _rb;
    private Vector3 _targetPosition;
    private Vector3 _posA;
    private Vector3 _posB;
    private bool _isWaiting;
    private bool _movingToB = true;
    private Vector3 _originalScale;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.useFullKinematicContacts = true;
    }

    private void Start()
    {
        if (pointA == null || pointB == null)
        {
            Debug.LogError(
                $"Un error en ingles muy largo de que hiciste mal las cosas\n Iñaki te lo traduce en criollo, faltan puntos de ruta en {gameObject.name}. Desactivando script."
            );

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            return;
        }

        _posA = pointA.position;
        _posB = pointB.position;

        _targetPosition = _posB;
        _movingToB = true;

        _originalScale = transform.localScale;
    }

    private void FixedUpdate()
    {
        if (_isWaiting)
            return;

        MoveTowardsTarget();
        CheckDistance();
    }

    private void MoveTowardsTarget()
    {
        Vector2 direction = (_targetPosition - transform.position).normalized;

        _rb.linearVelocity = new Vector2(direction.x * speed, 0f);

        if (Mathf.Abs(_rb.linearVelocity.x) > 0.01f)
        {
            Vector3 localScale = transform.localScale;
            localScale.x = -Mathf.Sign(_rb.linearVelocity.x) * Mathf.Abs(localScale.x);
            transform.localScale = localScale;
        }
    }

    private void CheckDistance()
    {
        float distanceX = Mathf.Abs(transform.position.x - _targetPosition.x);

        if (distanceX < threshold)
        {
            StartCoroutine(WaitAndSwitchRoutine());
        }
    }

    private IEnumerator WaitAndSwitchRoutine()
    {
        _isWaiting = true;
        _rb.linearVelocity = Vector2.zero;

        StartCoroutine(SquashAndStretchRoutine());

        yield return new WaitForSeconds(waitTimeAtEdge);

        _movingToB = !_movingToB;
        _targetPosition = _movingToB ? _posB : _posA;

        _isWaiting = false;
    }

    private IEnumerator SquashAndStretchRoutine()
    {
        float signX = Mathf.Sign(transform.localScale.x);

        Vector3 squashedScale = new Vector3(
            signX * Mathf.Abs(_originalScale.x) * squashX,
            _originalScale.y * stretchY,
            _originalScale.z
        );

        float elapsed = 0f;
        float halfDuration = squashDuration / 2f;

        while (elapsed < halfDuration)
        {
            transform.localScale = Vector3.Lerp(
                _originalScale,
                squashedScale,
                elapsed / halfDuration
            );
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = squashedScale;

        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            transform.localScale = Vector3.Lerp(
                squashedScale,
                _originalScale,
                elapsed / halfDuration
            );
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = _originalScale;
    }

    private void OnDrawGizmos()
    {
        if (pointA != null && pointB != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 a = Application.isPlaying ? _posA : pointA.position;
            Vector3 b = Application.isPlaying ? _posB : pointB.position;

            Gizmos.DrawLine(a, b);
            Gizmos.DrawWireSphere(a, 0.3f);
            Gizmos.DrawWireSphere(b, 0.3f);
        }
    }
}
