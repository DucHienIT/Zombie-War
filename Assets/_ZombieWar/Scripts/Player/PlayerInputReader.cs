using UnityEngine;
using UnityEngine.InputSystem;
using ZombieWar.Data;

namespace ZombieWar.Player
{
    public sealed class PlayerInputReader : MonoBehaviour
    {
        private const string LogPrefix = "[Input]";

        [SerializeField] private InputActionReference _moveAction;
        [SerializeField] private PlayerDefinitionSO _definition;

        public Vector2 MoveVector { get; private set; }

        private void Awake()
        {
            if (_moveAction == null || _definition == null)
            {
                Debug.LogError($"{LogPrefix} Move action or player definition is missing.", this);
            }
        }

        private void OnEnable()
        {
            _moveAction.action.Enable();
        }

        private void OnDisable()
        {
            _moveAction.action.Disable();
            MoveVector = Vector2.zero;
        }

        private void Update()
        {
            MoveVector = ApplyRadialDeadZone(_moveAction.action.ReadValue<Vector2>(), _definition.JoystickDeadZone);
        }

        private static Vector2 ApplyRadialDeadZone(Vector2 raw, float deadZone)
        {
            float magnitude = raw.magnitude;
            if (magnitude <= deadZone)
            {
                return Vector2.zero;
            }

            float remapped = Mathf.Clamp01((magnitude - deadZone) / (1f - deadZone));
            return raw / magnitude * remapped;
        }
    }
}
