using UnityEngine;

namespace ZombieWar.Enemies
{
    public sealed class ZombieMaterialFx : MonoBehaviour
    {
        private static readonly int HitAmountId = Shader.PropertyToID("_HitAmount");
        private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");

        [SerializeField] private Renderer[] _renderers;

        private MaterialPropertyBlock _block;
        private float _hitTimer;
        private float _hitDuration;
        private float _dissolveAmount;
        private bool _blockApplied;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
        }

        public void FlashHit(float duration)
        {
            _hitDuration = duration;
            _hitTimer = duration;
            Apply(1f);
        }

        public void SetDissolve(float amount)
        {
            _dissolveAmount = amount;
            Apply(CurrentHitAmount());
        }

        public void ResetAll()
        {
            _hitTimer = 0f;
            _dissolveAmount = 0f;
            Apply(0f);
        }

        public void Tick(float deltaTime)
        {
            if (_hitTimer <= 0f)
            {
                return;
            }

            _hitTimer -= deltaTime;
            Apply(CurrentHitAmount());
        }

        private float CurrentHitAmount()
        {
            return _hitDuration > 0f ? Mathf.Clamp01(_hitTimer / _hitDuration) : 0f;
        }

        private void Apply(float hitAmount)
        {
            // A renderer holding a property block is skipped by the SRP Batcher, and both amounts rest at 0 in the
            // material itself, so a zombie that is neither flashing nor dissolving drops its block instead.
            if (hitAmount <= 0f && _dissolveAmount <= 0f)
            {
                ClearBlock();
                return;
            }

            _block.SetFloat(HitAmountId, hitAmount);
            _block.SetFloat(DissolveAmountId, _dissolveAmount);
            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].SetPropertyBlock(_block);
            }

            _blockApplied = true;
        }

        private void ClearBlock()
        {
            if (!_blockApplied)
            {
                return;
            }

            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].SetPropertyBlock(null);
            }

            _blockApplied = false;
        }
    }
}
