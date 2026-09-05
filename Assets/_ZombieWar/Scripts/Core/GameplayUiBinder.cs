using UnityEngine;
using ZombieWar.Player;
using ZombieWar.UI;
using ZombieWar.Weapons;

namespace ZombieWar.Core
{
    // The intermediary the UI never sees. It owns every reference that crosses the
    // gameplay/UI boundary, which is what keeps UIRoot.prefab free of scene references
    // and lets the same prefab serve the menu scene as well.
    public sealed class GameplayUiBinder : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private UIManager _ui;
        [SerializeField] private GameFlowController _flow;
        [SerializeField] private PlayerHealth _playerHealth;
        [SerializeField] private WeaponController _weapons;
        [SerializeField] private BombThrower _bombs;
        [SerializeField] private CameraShakeController _cameraShake;

        private void Awake()
        {
            bool missing = _ui == null || _flow == null || _playerHealth == null || _weapons == null || _bombs == null
                           || _cameraShake == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} GameplayUiBinder has an unassigned reference - the HUD would never update.", this);
                return;
            }

            _ui.BindGameplayCommands(_flow.Pause, _weapons.RequestSwitch, _bombs.RequestThrow);
        }

        private void OnEnable()
        {
            _flow.OnStateChanged += HandleStateChanged;
            _flow.OnCountdownChanged += HandleCountdownChanged;
            _flow.OnRemainingTimeChanged += HandleRemainingTimeChanged;
            _flow.OnScoreChanged += HandleScoreChanged;
            _flow.OnLevelEnded += HandleLevelEnded;
            _playerHealth.OnHealthChanged += HandleHealthChanged;
            _weapons.OnGunChanged += HandleGunChanged;
            _weapons.OnAmmoChanged += HandleAmmoChanged;
            _weapons.OnReloadStarted += HandleReloadStarted;
            _weapons.OnReloadProgress += HandleReloadProgress;
            _weapons.OnReloadEnded += HandleReloadEnded;
            _bombs.OnChargesChanged += HandleBombChargesChanged;
            _bombs.OnCooldownProgress += HandleBombCooldownProgress;
        }

        private void OnDisable()
        {
            _flow.OnStateChanged -= HandleStateChanged;
            _flow.OnCountdownChanged -= HandleCountdownChanged;
            _flow.OnRemainingTimeChanged -= HandleRemainingTimeChanged;
            _flow.OnScoreChanged -= HandleScoreChanged;
            _flow.OnLevelEnded -= HandleLevelEnded;
            _playerHealth.OnHealthChanged -= HandleHealthChanged;
            _weapons.OnGunChanged -= HandleGunChanged;
            _weapons.OnAmmoChanged -= HandleAmmoChanged;
            _weapons.OnReloadStarted -= HandleReloadStarted;
            _weapons.OnReloadProgress -= HandleReloadProgress;
            _weapons.OnReloadEnded -= HandleReloadEnded;
            _bombs.OnChargesChanged -= HandleBombChargesChanged;
            _bombs.OnCooldownProgress -= HandleBombCooldownProgress;
        }

        private void HandleStateChanged(GameState state)
        {
            // MenuUiBinder owns that screen; touching the HUD here would fight it.
            if (state == GameState.Menu)
            {
                return;
            }

            if (state == GameState.Countdown)
            {
                _ui.ShowGameplayScreen();
            }

            _ui.SetPauseButtonInteractable(state == GameState.Playing);
            _ui.ShowCountdown(state == GameState.Countdown);
            // The run is over: nothing on the HUD is actionable behind the result panel.
            _ui.SetHudVisible(state != GameState.Won && state != GameState.Lost);

            if (state == GameState.Paused)
            {
                _ui.ShowPausePopup(_flow.Resume, _flow.Retry, _flow.GoToMenu, _cameraShake.IsEnabled, _cameraShake.SetEnabled);
            }
        }

        private void HandleCountdownChanged(int seconds) => _ui.SetCountdownSeconds(seconds);

        private void HandleRemainingTimeChanged(float remaining) => _ui.SetRemainingTime(remaining);

        private void HandleScoreChanged(int kills, int score) => _ui.SetScore(kills, score);

        private void HandleHealthChanged(float current, float max) => _ui.SetHealth(max > 0f ? current / max : 0f);

        private void HandleGunChanged(Gun gun) => _ui.SetGun(gun.Definition.Icon, gun.Definition.DisplayName);

        private void HandleAmmoChanged(int ammo, int magazine) => _ui.SetAmmo(ammo, magazine);

        private void HandleReloadStarted() => _ui.SetReloadProgress(0f);

        private void HandleReloadProgress(float progress) => _ui.SetReloadProgress(progress);

        private void HandleReloadEnded() => _ui.SetReloadProgress(0f);

        private void HandleBombChargesChanged(int charges) => _ui.SetBombCharges(charges);

        private void HandleBombCooldownProgress(float progress) => _ui.SetBombCooldown(progress);

        // The flow banks the score before this fires, so the panel only draws what the player owns.
        private void HandleLevelEnded(LevelResult result)
        {
            bool hasNextLevel = _flow.Level != null && _flow.Level.NextLevel != null;
            var data = new ResultData(
                result.Won,
                result.Kills,
                result.Score,
                result.HealthBonus,
                Mathf.RoundToInt(result.DamageTaken),
                result.TotalScore,
                result.IsNewBest,
                hasNextLevel);
            _ui.ShowResultPopup(data, _flow.Retry, _flow.GoToNextLevel, _flow.GoToMenu);
        }
    }
}
