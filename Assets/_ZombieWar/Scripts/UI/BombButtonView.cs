using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieWar.Audio;
using ZombieWar.Weapons;

namespace ZombieWar.UI
{
    public sealed class BombButtonView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private BombThrower _thrower;
        [SerializeField] private AudioService _audio;
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _chargesText;
        // Radial fill that empties on throw and refills over the cooldown.
        [SerializeField] private Image _cooldownFill;
        [SerializeField] private AudioClip _readyClip;

        private bool _wasReady = true;

        private void Awake()
        {
            if (_thrower == null || _audio == null || _button == null || _chargesText == null || _cooldownFill == null)
            {
                Debug.LogError($"{LogPrefix} BombButtonView has an unassigned reference.", this);
                return;
            }

            _button.onClick.AddListener(_thrower.RequestThrow);
        }

        private void OnEnable()
        {
            _thrower.OnChargesChanged += HandleChargesChanged;
            _thrower.OnCooldownProgress += HandleCooldownProgress;
        }

        private void OnDisable()
        {
            _thrower.OnChargesChanged -= HandleChargesChanged;
            _thrower.OnCooldownProgress -= HandleCooldownProgress;
        }

        private void HandleChargesChanged(int charges)
        {
            _chargesText.SetText("{0}", charges);
            _button.interactable = charges > 0;
        }

        private void HandleCooldownProgress(float progress)
        {
            _cooldownFill.fillAmount = progress;
            bool ready = progress >= 1f;
            if (ready && !_wasReady && _thrower.Charges > 0)
            {
                _audio.PlayUi(_readyClip);
            }

            _wasReady = ready;
        }
    }
}
