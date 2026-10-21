using UnityEngine;
using UnityEngine.InputSystem;

namespace TrainingScenarios.Demo.Interactions
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class TopDownPlayerController : MonoBehaviour
    {
        [SerializeField]
        private float _moveSpeed = 5f;
        [SerializeField]
        private float _turnSpeed = 14f;

        private CharacterController _controller;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            var input = new Vector2(
                ReadAxis(keyboard.aKey.isPressed, keyboard.dKey.isPressed),
                ReadAxis(keyboard.sKey.isPressed, keyboard.wKey.isPressed));
            var direction = new Vector3(input.x, 0f, input.y);
            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }

            _controller.SimpleMove(direction * _moveSpeed);

            if (direction.sqrMagnitude > 0.001f)
            {
                var targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    _turnSpeed * Time.deltaTime);
            }
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            _controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            _controller.enabled = true;
        }

        private static float ReadAxis(bool negative, bool positive)
        {
            return (positive ? 1f : 0f) - (negative ? 1f : 0f);
        }
    }
}
