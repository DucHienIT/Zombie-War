using Cinemachine;
using DG.Tweening;
using UnityEngine;
using ZombieWar.Core;
using ZombieWar.Data;
using ZombieWar.VFX;

namespace ZombieWar.Player
{
    // Everything a bite shows beyond the material flash: blood, a camera bump and the body
    // recoiling away from the attacker. Death hands the pivot to PlayerDeathPresenter instead.
    public sealed class PlayerHitReactionPresenter : MonoBehaviour
    {
        private const string LogPrefix = "[Player]";

        [SerializeField] private PlayerHealth _health;
        [SerializeField] private PlayerDefinitionSO _definition;
        [SerializeField] private Transform _modelPivot;
        // Chest-height point the blood spray starts from.
        [SerializeField] private Transform _bloodOrigin;
        [SerializeField] private VfxService _vfx;
        [SerializeField] private CinemachineImpulseSource _impulseSource;

        [Header("Jolt")]
        [SerializeField] private float _joltDegrees = 12f;
        [SerializeField] private float _joltDuration = 0.22f;
        [SerializeField] private float _joltElasticity = 0.4f;

        private Transform _transform;

        private void Awake()
        {
            _transform = transform;
            bool missing = _health == null || _definition == null || _modelPivot == null || _bloodOrigin == null
                           || _vfx == null || _impulseSource == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} PlayerHitReactionPresenter has an unassigned reference.", this);
            }
        }

        private void OnEnable()
        {
            _health.OnDamaged += HandleDamaged;
        }

        private void OnDisable()
        {
            _health.OnDamaged -= HandleDamaged;
        }

        private void HandleDamaged(DamageInfo info)
        {
            Vector3 push = info.Direction;
            push.y = 0f;
            bool directional = push.sqrMagnitude > 0f;
            if (directional)
            {
                push.Normalize();
            }

            Quaternion spray = directional ? Quaternion.LookRotation(push) : Quaternion.identity;
            _vfx.Play(_definition.HitVfx, _bloodOrigin.position, spray);
            _impulseSource.GenerateImpulseWithForce(_definition.HitCameraImpulse);

            // The death fall owns the pivot from here on; a jolt on top would fight it.
            if (!directional || !_health.IsAlive)
            {
                return;
            }

            Vector3 local = _transform.InverseTransformDirection(push);
            var punch = new Vector3(local.z * _joltDegrees, 0f, -local.x * _joltDegrees);
            _modelPivot.DOKill();
            _modelPivot.DOPunchRotation(punch, _joltDuration, 1, _joltElasticity).SetLink(gameObject);
        }
    }
}
