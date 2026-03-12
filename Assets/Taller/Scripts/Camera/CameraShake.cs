using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class CameraShake : CinemachineExtension
{
    public static CameraShake Instance { get; private set; }

    [SerializeField]
    private float _defaultDuration = 0.3f;

    [SerializeField]
    private float _defaultMagnitude = 0.25f;

    [SerializeField]
    private float _noiseFrequency = 40f;

    [SerializeField]
    private float _maxShakeDistance = 15f;

    private Vector3 _currentOffset;
    private Coroutine _shakeRoutine;

    CinemachineCamera _virtualCamera;

    protected override void Awake()
    {
        base.Awake();

        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        _virtualCamera = GetComponent<CinemachineCamera>();

        if (_virtualCamera.Follow == null)
        {
            Debug.LogError(
                "Camera requires a Follow target to work. Please assign a target to the Cinemachine Virtual Camera.\n Iñaki te lo traduce en criollo, la camara requiere un objetivo de seguimiento para funcionar. Asigna un objetivo a la Cinemachine Virtual Camera en Tracking target."
            );
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            return;
        }
    }

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime
    )
    {
        if (stage == CinemachineCore.Stage.Finalize)
            state.PositionCorrection += _currentOffset;
    }

    public void ShakeFromPosition(
        Vector3 explosionPos,
        float duration = -1f,
        float maxMagnitude = -1f
    )
    {
        float distance = Vector2.Distance(CameraShakeListener.Position, explosionPos);
        if (distance >= _maxShakeDistance)
            return;

        float ratio = 1f - Mathf.Clamp01(distance / _maxShakeDistance);
        float magnitude = (maxMagnitude < 0 ? _defaultMagnitude : maxMagnitude) * (ratio * ratio);

        if (_shakeRoutine != null)
            StopCoroutine(_shakeRoutine);
        _shakeRoutine = StartCoroutine(
            ShakeRoutine(duration < 0 ? _defaultDuration : duration, magnitude)
        );
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;
        float seedX = Random.Range(0f, 100f);
        float seedY = Random.Range(0f, 100f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - (elapsed / duration);

            float x =
                (Mathf.PerlinNoise(seedX + elapsed * _noiseFrequency, 0f) - 0.5f)
                * 2f
                * magnitude
                * t;
            float y =
                (Mathf.PerlinNoise(0f, seedY + elapsed * _noiseFrequency) - 0.5f)
                * 2f
                * magnitude
                * t;

            _currentOffset = new Vector3(x, y, 0f);
            yield return null;
        }

        _currentOffset = Vector3.zero;
        _shakeRoutine = null;
    }
}
