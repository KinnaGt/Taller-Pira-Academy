using UnityEngine;

public class CameraShakeListener : MonoBehaviour
{
    public static Vector3 Position { get; private set; }

    private void LateUpdate()
    {
        Position = transform.position;
    }
}
