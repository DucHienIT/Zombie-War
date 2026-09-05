using UnityEngine;
using ZombieWar.Core;
using ZombieWar.Data;

namespace ZombieWar.Player
{
    public sealed class PlayerMotor : MonoBehaviour
    {
        private const string LogPrefix = "[Player]";
        private const float MovingThresholdSqr = 0.0001f;

        [SerializeField] private PlayerDefinitionSO _definition;
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private PlayerAim _aim;
        [SerializeField] private PlayerHealth _health;
        [SerializeField] private PlayerStatSheet _stats;
        [SerializeField] private GameFlowController _flow;

        private Transform _transform;

        // Movement expressed in the soldier's local frame: x = strafe, y = forward. Feeds the locomotion blend tree.
        public Vector2 LocalMoveDirection { get; private set; }
        public float NormalizedSpeed { get; private set; }
        public Vector3 WorldMoveDirection { get; private set; }
        public Vector3 Forward => _transform.forward;

        private void Awake()
        {
            _transform = transform;
            if (_definition == null || _rigidbody == null || _input == null || _aim == null || _health == null
                || _stats == null || _flow == null)
            {
                Debug.LogError($"{LogPrefix} PlayerMotor has an unassigned reference.", this);
            }
        }

        private void FixedUpdate()
        {
            float deltaTime = Time.fixedDeltaTime;
            bool canMove = _flow.State == GameState.Playing && _health.IsAlive;
            Vector2 input = canMove ? _input.MoveVector : Vector2.zero;

            float moveSpeed = _definition.MoveSpeed * _stats.Multiplier(StatId.MoveSpeed);
            // Camera is north-up, so joystick axes map straight onto world X/Z.
            Vector3 targetVelocity = new Vector3(input.x, 0f, input.y) * moveSpeed;
            Vector3 velocity = _rigidbody.velocity;
            Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
            horizontal = Vector3.MoveTowards(horizontal, targetVelocity, _definition.Acceleration * deltaTime);
            _rigidbody.velocity = new Vector3(horizontal.x, velocity.y, horizontal.z);

            RotateTowardsFacing(horizontal, deltaTime);
            PublishMotion(horizontal, moveSpeed);
        }

        private void RotateTowardsFacing(Vector3 horizontalVelocity, float deltaTime)
        {
            Vector3 facing;
            if (_aim.HasTarget)
            {
                facing = _aim.TargetPosition - _transform.position;
                facing.y = 0f;
            }
            else
            {
                facing = horizontalVelocity;
            }

            if (facing.sqrMagnitude <= MovingThresholdSqr)
            {
                return;
            }

            Quaternion target = Quaternion.LookRotation(facing);
            Quaternion next = Quaternion.RotateTowards(_rigidbody.rotation, target, _definition.TurnSpeedDegrees * deltaTime);
            _rigidbody.MoveRotation(next);
        }

        // Normalised against the boosted speed so the locomotion blend tree still tops out at 1.
        private void PublishMotion(Vector3 horizontalVelocity, float speed)
        {
            Vector3 local = _transform.InverseTransformDirection(horizontalVelocity) / speed;
            LocalMoveDirection = new Vector2(local.x, local.z);
            NormalizedSpeed = horizontalVelocity.magnitude / speed;
            WorldMoveDirection = NormalizedSpeed > 0f ? horizontalVelocity / horizontalVelocity.magnitude : Vector3.zero;
        }
    }
}
