using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ScreenBoundsColliders : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip(
        "Grosor de los muros generados. Deben ser gruesos para evitar que objetos rápidos los atraviesen."
    )]
    [SerializeField]
    private float wallThickness = 2f;

    [Header("Bordes Activos")]
    [Tooltip("Activar muro superior")]
    [SerializeField]
    private bool enableTop = true;

    [Tooltip("Activar muro inferior (Desactivar si hay pozos de muerte)")]
    [SerializeField]
    private bool enableBottom = false;

    [Tooltip("Activar muro izquierdo")]
    [SerializeField]
    private bool enableLeft = true;

    [Tooltip("Activar muro derecho")]
    [SerializeField]
    private bool enableRight = true;

    [Tooltip("Layer para los muros (ej. 'Ground' o 'InvisibleWall').")]
    [SerializeField]
    private string collisionLayerName = "Default";

    [Header("Física")]
    [Tooltip("Material sin fricción para evitar que el player se pegue.")]
    [SerializeField]
    private PhysicsMaterial2D noFrictionMaterial;

    private Camera _cam;
    private BoxCollider2D _topWall;
    private BoxCollider2D _bottomWall;
    private BoxCollider2D _leftWall;
    private BoxCollider2D _rightWall;

    private float _lastOrthoSize;
    private float _lastAspect;

    private void Awake()
    {
        _cam = GetComponent<Camera>();

        GameObject wallsContainer = new GameObject("ScreenBounds_Generated");
        wallsContainer.transform.SetParent(transform);
        wallsContainer.transform.localPosition = Vector3.zero;

        _topWall = CreateWall("TopWall", wallsContainer);
        _bottomWall = CreateWall("BottomWall", wallsContainer);
        _leftWall = CreateWall("LeftWall", wallsContainer);
        _rightWall = CreateWall("RightWall", wallsContainer);
    }

    private void Start()
    {
        if (noFrictionMaterial == null)
        {
            noFrictionMaterial = new PhysicsMaterial2D("Auto_NoFriction")
            {
                friction = 0f,
                bounciness = 0f
            };
        }

        _topWall.sharedMaterial = noFrictionMaterial;
        _bottomWall.sharedMaterial = noFrictionMaterial;
        _leftWall.sharedMaterial = noFrictionMaterial;
        _rightWall.sharedMaterial = noFrictionMaterial;

        UpdateBounds();
        UpdateActiveWalls();
    }

    private void LateUpdate()
    {
        if (_cam.orthographicSize != _lastOrthoSize || _cam.aspect != _lastAspect)
        {
            UpdateBounds();
        }

        UpdateActiveWalls();
    }

    private BoxCollider2D CreateWall(string name, GameObject parent)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent.transform);
        wall.transform.localPosition = Vector3.zero;

        int layerIndex = LayerMask.NameToLayer(collisionLayerName);
        if (layerIndex != -1)
            wall.layer = layerIndex;

        BoxCollider2D col = wall.AddComponent<BoxCollider2D>();

        Rigidbody2D rb = wall.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;
        rb.useFullKinematicContacts = true;

        return col;
    }

    private void UpdateActiveWalls()
    {
        if (_topWall.gameObject.activeSelf != enableTop)
            _topWall.gameObject.SetActive(enableTop);
        if (_bottomWall.gameObject.activeSelf != enableBottom)
            _bottomWall.gameObject.SetActive(enableBottom);
        if (_leftWall.gameObject.activeSelf != enableLeft)
            _leftWall.gameObject.SetActive(enableLeft);
        if (_rightWall.gameObject.activeSelf != enableRight)
            _rightWall.gameObject.SetActive(enableRight);
    }

    private void UpdateBounds()
    {
        float verticalHeight = _cam.orthographicSize * 2f;
        float horizontalWidth = verticalHeight * _cam.aspect;

        _topWall.size = new Vector2(horizontalWidth + wallThickness * 2, wallThickness);
        _topWall.transform.localPosition = new Vector3(
            0,
            (verticalHeight / 2) + (wallThickness / 2),
            0
        );

        _bottomWall.size = new Vector2(horizontalWidth + wallThickness * 2, wallThickness);
        _bottomWall.transform.localPosition = new Vector3(
            0,
            -(verticalHeight / 2) - (wallThickness / 2),
            0
        );

        _leftWall.size = new Vector2(wallThickness, verticalHeight);
        _leftWall.transform.localPosition = new Vector3(
            -(horizontalWidth / 2) - (wallThickness / 2),
            0,
            0
        );

        _rightWall.size = new Vector2(wallThickness, verticalHeight);
        _rightWall.transform.localPosition = new Vector3(
            (horizontalWidth / 2) + (wallThickness / 2),
            0,
            0
        );

        _lastOrthoSize = _cam.orthographicSize;
        _lastAspect = _cam.aspect;
    }
}
