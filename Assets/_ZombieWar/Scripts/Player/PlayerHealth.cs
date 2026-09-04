using System;
using UnityEngine;
using ZombieWar.Core;
using ZombieWar.Data;

namespace ZombieWar.Player
{
    public sealed class PlayerHealth : MonoBehaviour, IDamageable
    {
        private const string LogPrefix = "[Player]";

        [SerializeField] private PlayerDefinitionSO _definition;

        private float _current;
        private float _invulnerabilityTimer;

        public event Action<float, float> OnHealthChanged;
        public event Action<DamageInfo> OnDamaged;
        public event Action OnDied;

        public float Current => _current;
        public float Max => _definition.MaxHp;
        public float Normalized => _current / _definition.MaxHp;
        public float DamageTaken { get; private set; }
        public bool IsAlive => _current > 0f;

        private void Awake()
        {
            if (_definition == null)
            {
                Debug.LogError($"{LogPrefix} Player definition is missing.", this);
                return;
            }

            _current = _definition.MaxHp;
        }

        private void Start()
        {
            OnHealthChanged?.Invoke(_current, Max);
        }

        private void Update()
        {
            if (_invulnerabilityTimer > 0f)
            {
                _invulnerabilityTimer -= Time.deltaTime;
            }
        }

        public void TakeDamage(in DamageInfo info)
        {
            if (!IsAlive || _invulnerabilityTimer > 0f)
            {
                return;
            }

            _invulnerabilityTimer = _definition.HitInvulnerability;
            _current = Mathf.Max(0f, _current - info.Amount);
            DamageTaken += info.Amount;
            OnHealthChanged?.Invoke(_current, Max);
            OnDamaged?.Invoke(info);

            if (!IsAlive)
            {
                OnDied?.Invoke();
            }
        }
    }
}
