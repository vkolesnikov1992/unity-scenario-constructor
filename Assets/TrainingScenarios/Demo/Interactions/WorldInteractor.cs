using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TrainingScenarios.Demo.Interactions
{
    public sealed class WorldInteractor : MonoBehaviour
    {
        [SerializeField]
        private float _interactionRadius = 2.25f;
        [SerializeField]
        private LayerMask _interactionLayers = ~0;
        [SerializeField]
        private Text _promptText;

        private readonly Collider[] _overlaps = new Collider[24];
        private WorldInteractable _current;
        private bool _wasInteractPressed;

        private void Update()
        {
            _current = FindClosestInteractable();
            UpdatePrompt();

            var keyboard = Keyboard.current;
            var isInteractPressed = keyboard != null && keyboard.eKey.isPressed;
            if (_current && isInteractPressed && !_wasInteractPressed)
            {
                _current.Interact();
            }

            _wasInteractPressed = isInteractPressed;
        }

        private WorldInteractable FindClosestInteractable()
        {
            var count = Physics.OverlapSphereNonAlloc(
                transform.position,
                _interactionRadius,
                _overlaps,
                _interactionLayers,
                QueryTriggerInteraction.Collide);
            WorldInteractable closest = null;
            var closestDistance = float.MaxValue;

            for (var index = 0; index < count; index++)
            {
                var candidate = _overlaps[index].GetComponentInParent<WorldInteractable>();
                if (!candidate || !candidate.CanInteract)
                {
                    continue;
                }

                var distance = (candidate.InteractionPosition - transform.position).sqrMagnitude;
                if (distance >= closestDistance)
                {
                    continue;
                }

                closest = candidate;
                closestDistance = distance;
            }

            return closest;
        }

        private void UpdatePrompt()
        {
            if (!_promptText)
            {
                return;
            }

            _promptText.text = _current
                ? $"[E]  {_current.Prompt}"
                : "WASD — MOVE   E — INTERACT";
        }
    }
}
