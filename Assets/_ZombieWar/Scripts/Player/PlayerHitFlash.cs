using UnityEngine;
using ZombieWar.Core;
using ZombieWar.Data;

namespace ZombieWar.Player
{
    public sealed class PlayerHitFlash : MonoBehaviour
    {
        private const string LogPrefix = "[Player]";

        [SerializeField] private PlayerHealth _health;
        [SerializeField] private FeedbackProfileSO _feedback;
        [SerializeField] private Renderer[] _renderers;
        [SerializeField] private string _colorPropertyName = "_BaseColor";
        [SerializeField] private Color _flashColor;

        private MaterialPropertyBlock _block;
        private Color[] _baseColors;
        private int _colorPropertyId;
        private float _timer;
        private bool _blockApplied;

        private void Awake()
        {
            if (_health == null || _feedback == null || _renderers == null || _renderers.Length == 0)
            {
                Debug.LogError($"{LogPrefix} PlayerHitFlash has an unassigned reference.", this);
                return;
            }

            _block = new MaterialPropertyBlock();
            _colorPropertyId = Shader.PropertyToID(_colorPropertyName);
            _baseColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                Material material = _renderers[i].sharedMaterial;
                if (!material.HasProperty(_colorPropertyId))
                {
                    Debug.LogError($"{LogPrefix} Renderer {_renderers[i].name} uses {material.shader.name} without {_colorPropertyName}; remove it from PlayerHitFlash.", this);
                    continue;
                }

                // Cache the authored color so the flash can restore it exactly.
                _baseColors[i] = material.GetColor(_colorPropertyId);
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

        private void Update()
        {
            if (_timer <= 0f)
            {
                return;
            }

            _timer -= Time.deltaTime;
            float blend = Mathf.Clamp01(_timer / _feedback.PlayerHitFlashDuration);
            Apply(blend);
        }

        private void HandleDamaged(DamageInfo info)
        {
            _timer = _feedback.PlayerHitFlashDuration;
            Apply(1f);
        }

        private void Apply(float blend)
        {
            // A renderer holding a property block is skipped by the SRP Batcher, and blend 0 resolves to the authored
            // color anyway, so the soldier drops his block once the flash is over.
            if (blend <= 0f)
            {
                ClearBlock();
                return;
            }

            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer renderer = _renderers[i];
                renderer.GetPropertyBlock(_block);
                _block.SetColor(_colorPropertyId, Color.Lerp(_baseColors[i], _flashColor, blend));
                renderer.SetPropertyBlock(_block);
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
