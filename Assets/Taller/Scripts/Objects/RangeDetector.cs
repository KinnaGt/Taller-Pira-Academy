using UnityEngine;

public class RangeDetector : MonoBehaviour
{
    public bool IsPlayerInRange { get; private set; }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            IsPlayerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            IsPlayerInRange = false;
    }
}
