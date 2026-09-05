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
        [SerializeField] private PlayerStatSheet _stats;

        private float _current;
        private float _invulnerabilityTimer;
        private float _maxHpBonus;

        public event Action<float, float> OnHealthChanged;
        public event Action<DamageInfo> OnDamaged;
        public event Action OnDied;

        public float Current => _current;
        public float Max => _definition.MaxHp + _maxHpBonus;
        public float Normalized => _current / Max;
        public float DamageTaken { get; private set; }
        public bool IsAlive => _current > 0f;

        private void Awake()
        {
            if (_definition == null || _stats == null)
            {
                Debug.LogError($"{LogPrefix} PlayerHealth has an unassigned reference.", this);
                return;
            }

            _current = Max;
        }

        private void OnEnable()
        {
            _stats.OnChanged += HandleStatsChanged;
        }

        private void OnDisable()
        {
            _stats.OnChanged -= HandleStatsChanged;
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

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f)
            {
                return;
            }

            float healed = Mathf.Min(amount, Max - _current);
            if (healed <= 0f)
            {
                return;
            }

            _current += healed;
            OnHealthChanged?.Invoke(_current, Max);
        }

        // Extra max health has to hand over the hit points as well, or the upgrade only
        // widens the empty end of the bar.
        private void HandleStatsChanged()
        {
            float bonus = _stats.Additive(StatId.MaxHealth);
            float delta = bonus - _maxHpBonus;
            _maxHpBonus = bonus;
            _current = Mathf.Clamp(_current + delta, 0f, Max);
            OnHealthChanged?.Invoke(_current, Max);
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
