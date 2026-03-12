using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField]
    private float _interactionRadius = 1.5f;

    [SerializeField]
    private Transform _interactionPoint;

    [SerializeField]
    private LayerMask _interactableLayer;

    private ContactFilter2D _interactionFilter;
    private Collider2D[] _hitBuffer = new Collider2D[3];

    private void Awake()
    {
        if (_interactionPoint == null)
            _interactionPoint = transform;

        _interactionFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = _interactableLayer,
            useTriggers = true
        };
    }

    private void Start()
    {
        PlayerInput input = GetComponent<PlayerInput>();

        if (input == null)
        {
            Debug.LogError(
                "[DIAGNÓSTICO CRÍTICO] No se encontró PlayerInput en este GameObject. 'Send Messages' FALLARÁ si ambos scripts no están en el mismo objeto."
            );
            return;
        }

        InputAction interactAction = input.actions?.FindAction("Interact");
        if (interactAction == null)
        {
            Debug.LogError(
                "[DIAGNÓSTICO CRÍTICO] La acción 'Interact' NO EXISTE. Revisa que esté escrita exactamente así en tu Input Actions Asset (sensible a mayúsculas)."
            );
        }
    }

    public void OnInteract(InputValue value)
    {
        if (!value.isPressed)
            return;

        TryInteract();
    }

    private void TryInteract()
    {
        int hits = Physics2D.OverlapCircle(
            _interactionPoint.position,
            _interactionRadius,
            _interactionFilter,
            _hitBuffer
        );

        if (hits == 0)
            return;

        for (int i = 0; i < hits; i++)
        {
            GameObject hitObject = _hitBuffer[i].gameObject;

            if (_hitBuffer[i].TryGetComponent(out IInteractable interactable))
            {
                interactable.Interact();
                break;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_interactionPoint == null)
            return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(_interactionPoint.position, _interactionRadius);
    }
}
