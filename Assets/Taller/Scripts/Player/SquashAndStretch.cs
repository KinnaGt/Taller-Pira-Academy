using UnityEngine;

public class SquashAndStretch : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField]
    private PlayerController _playerController;

    [Tooltip(
        "El Transform que se deformará. IDEALMENTE debe ser un hijo con el SpriteRenderer, NO el objeto padre con la física."
    )]
    [SerializeField]
    private Transform _visualsRoot;

    [Header("Configuración")]
    [SerializeField]
    private Vector2 _jumpStretch = new Vector2(0.75f, 1.25f);

    [SerializeField]
    private Vector2 _landSquash = new Vector2(1.25f, 0.75f);

    [Tooltip("Velocidad de retorno a la normalidad. Mayor = más rápido.")]
    [SerializeField]
    private float _returnSpeed = 15f;

    private Vector2 _currentDeformation = Vector2.one;

    private Vector2 _baseScaleMagnitude;

    private void Awake()
    {
        if (_visualsRoot == null)
            _visualsRoot = transform;

        Vector3 s = _visualsRoot.localScale;
        _baseScaleMagnitude = new Vector2(Mathf.Abs(s.x), Mathf.Abs(s.y));
    }

    private void OnEnable()
    {
        if (_playerController != null)
        {
            _playerController.OnJumpPerformed += ApplyJumpStretch;
            _playerController.OnLandPerformed += ApplyLandSquash;
        }
    }

    private void OnDisable()
    {
        if (_playerController != null)
        {
            _playerController.OnJumpPerformed -= ApplyJumpStretch;
            _playerController.OnLandPerformed -= ApplyLandSquash;
        }
    }

    private void LateUpdate()
    {
        _currentDeformation = Vector2.Lerp(
            _currentDeformation,
            Vector2.one,
            Time.deltaTime * _returnSpeed
        );

        float currentSignX = Mathf.Sign(_visualsRoot.localScale.x);
        float currentSignY = Mathf.Sign(_visualsRoot.localScale.y);

        _visualsRoot.localScale = new Vector3(
            _baseScaleMagnitude.x * _currentDeformation.x * currentSignX,
            _baseScaleMagnitude.y * _currentDeformation.y * currentSignY,
            _visualsRoot.localScale.z
        );
    }

    private void ApplyJumpStretch()
    {
        _currentDeformation = _jumpStretch;
    }

    private void ApplyLandSquash()
    {
        _currentDeformation = _landSquash;
    }
}
