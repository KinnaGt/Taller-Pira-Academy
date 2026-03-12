using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public interface IControllable
{
    void DisableControl();
}

[RequireComponent(typeof(Collider2D))]
public class EndFlag : MonoBehaviour
{
    [Header("UI Dependencies")]
    [SerializeField]
    private CanvasGroup _endGamePanel;

    [SerializeField]
    private float _fadeDuration = 0.5f;

    private bool _isTriggered;

    private void Awake()
    {
        if (_endGamePanel == null)
        {
            Debug.LogError(
                "EndFlag requires a reference to the end game panel. Please assign a CanvasGroup to the EndFlag script.\n Iñaki te lo traduce en criollo, el EndFlag requiere una referencia al panel de fin de juego. Asigna un CanvasGroup al script EndFlag en el inspector."
            );
            UnityEditor.EditorApplication.isPlaying = false;
#if UNITY_EDITOR

#endif
        }
    }

    private void Start()
    {
        if (_endGamePanel != null)
        {
            _endGamePanel.alpha = 0f;
            _endGamePanel.interactable = false;
            _endGamePanel.blocksRaycasts = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_isTriggered || !collision.CompareTag("Player"))
            return;

        _isTriggered = true;

        HaltPlayer(collision.gameObject);
        EnableEndGamePanel();
    }

    private void HaltPlayer(GameObject playerGO)
    {
        if (playerGO.TryGetComponent(out IControllable controllable))
        {
            controllable.DisableControl();
        }
        else
        {
            if (playerGO.TryGetComponent(out PlayerInput input))
            {
                input.DeactivateInput();
                input.enabled = false;
            }

            if (playerGO.TryGetComponent(out Rigidbody2D rb))
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                rb.angularVelocity = 0f;

                rb.constraints =
                    RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
            }
        }
    }

    private void EnableEndGamePanel()
    {
        if (_endGamePanel == null)
            return;
        StartCoroutine(FadeInPanelRoutine());
    }

    private IEnumerator FadeInPanelRoutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < _fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            _endGamePanel.alpha = Mathf.Clamp01(elapsedTime / _fadeDuration);
            yield return null;
        }

        _endGamePanel.alpha = 1f;
        _endGamePanel.interactable = true;
        _endGamePanel.blocksRaycasts = true;
    }
}
