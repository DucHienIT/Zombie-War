using UnityEngine;
using ZombieWar.Player;

namespace ZombieWar.Core
{
    // Slides the camera follow target ahead of the soldier along the joystick direction so the
    // screen leans into where the player is going instead of trailing them.
    public sealed class CameraLookAhead : MonoBehaviour
    {
        private const string LogPrefix = "[Camera]";

        [SerializeField] private PlayerMotor _motor;
        [SerializeField] private Transform _player;
        // Child of Player that the virtual camera follows; only ever offset horizontally.
        [SerializeField] private Transform _followTarget;

        [Header("Look Ahead")]
        [SerializeField] private float _maxOffset = 1f;
        [SerializeField] private float _smoothTime = 0.4f;

        private Vector3 _offset;
        private Vector3 _velocity;

        private void Awake()
        {
            if (_motor == null || _player == null || _followTarget == null)
            {
                Debug.LogError($"{LogPrefix} CameraLookAhead has an unassigned reference.", this);
            }
        }

        private void Update()
        {
            Vector3 desired = _motor.WorldMoveDirection * (_motor.NormalizedSpeed * _maxOffset);
            _offset = Vector3.SmoothDamp(_offset, desired, ref _velocity, _smoothTime);
            _followTarget.position = _player.position + _offset;
        }
    }
}
