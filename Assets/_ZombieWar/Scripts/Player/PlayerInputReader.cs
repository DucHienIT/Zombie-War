using UnityEngine;
using UnityEngine.InputSystem;

namespace ZombieWar.Player
{
    public sealed class PlayerInputReader : MonoBehaviour
    {
        private const string LogPrefix = "[Input]";

        [SerializeField] private InputActionReference _moveAction;

        public Vector2 MoveVector { get; private set; }

        private void Awake()
        {
            if (_moveAction == null)
            {
                Debug.LogError($"{LogPrefix} Move action is missing.", this);
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

        // The stick control already carries the Input System's StickDeadzone processor
        // (<Gamepad>/leftStick, project defaults 0.125-0.925), so the value arrives remapped;
        // a second radial dead zone here only pushed the first response ~10 px further out.
        private void Update()
        {
            MoveVector = _moveAction.action.ReadValue<Vector2>();
        }
    }
}
