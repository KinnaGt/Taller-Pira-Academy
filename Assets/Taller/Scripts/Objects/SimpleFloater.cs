using UnityEngine;

public class SimpleFloater : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField]
    private float amplitude = 0.25f;

    [SerializeField]
    private float frequency = 2f;

    [SerializeField]
    private float rotationSpeed = 100f;

    private Vector3 _startPos;

    private void Start()
    {
        _startPos = transform.position;
    }

    private void Update()
    {
        float newY = _startPos.y + Mathf.Sin(Time.time * frequency) * amplitude;

        transform.position = new Vector3(_startPos.x, newY, _startPos.z);
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }
}
