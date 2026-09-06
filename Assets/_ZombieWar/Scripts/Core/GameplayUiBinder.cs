using System;
using UnityEngine;
using ZombieWar.Data;
using ZombieWar.Player;
using ZombieWar.Roguelike;
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
        [SerializeField] private CameraShakeController _cameraShake;
        [SerializeField] private RoguelikeDirector _rogue;
        [SerializeField] private AbilityRunner _abilities;

        private SkillCardData[] _skillCards;
        private Action<int> _onSkillPicked;
        private readonly AbilityRunner.EquippedSkill[] _equippedBuffer = new AbilityRunner.EquippedSkill[AbilityRunner.MaxEquipped];
        private readonly ActiveSkillHudEntry[] _activeSkillsHud = new ActiveSkillHudEntry[AbilityRunner.MaxEquipped];

        private void Awake()
        {
            bool missing = _ui == null || _flow == null || _playerHealth == null || _weapons == null
                           || _cameraShake == null || _rogue == null || _abilities == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} GameplayUiBinder has an unassigned reference - the HUD would never update.", this);
                return;
            }

            // Cached once: turning a method group into a delegate allocates on every level-up.
            _onSkillPicked = _rogue.ChooseOffer;
            _ui.BindGameplayCommands(_flow.Pause);
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
            _rogue.OnXpChanged += HandleXpChanged;
            _rogue.OnChoiceOffered += HandleChoiceOffered;
            _rogue.OnChoiceClosed += HandleChoiceClosed;
            _abilities.OnLoadoutChanged += HandleLoadoutChanged;
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
            _rogue.OnXpChanged -= HandleXpChanged;
            _rogue.OnChoiceOffered -= HandleChoiceOffered;
            _rogue.OnChoiceClosed -= HandleChoiceClosed;
            _abilities.OnLoadoutChanged -= HandleLoadoutChanged;
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

        private void HandleXpChanged(float normalized, int battleLevel) => _ui.SetXp(normalized, battleLevel);

        // Cooldowns only tick while the runner itself ticks them, so this only needs to run
        // during Playing - the loadout-changed push below covers every other state.
        private void Update()
        {
            if (_flow.State == GameState.Playing)
            {
                RefreshActiveSkills();
            }
        }

        // A structural change (drafted a new one, fresh run) has to land immediately even while
        // paused for the level-up popup, which is why this is not folded into Update.
        private void HandleLoadoutChanged() => RefreshActiveSkills();

        // The runner only knows ActiveSkillSO; flattening to icon + stack + cooldown fraction
        // here is what keeps that asset type out of UIRoot.prefab.
        private void RefreshActiveSkills()
        {
            int count = _abilities.CopyEquipped(_equippedBuffer);
            for (int i = 0; i < count; i++)
            {
                AbilityRunner.EquippedSkill entry = _equippedBuffer[i];
                _activeSkillsHud[i] = new ActiveSkillHudEntry(entry.Skill.Icon, entry.Stacks, entry.CooldownFraction);
            }

            _ui.SetActiveSkills(_activeSkillsHud, count);
        }

        // The draft hands over gameplay assets; flattening them here is what keeps the popup
        // ignorant of PassiveSkillSO, the same way the result panel never sees a LevelResult.
        private void HandleChoiceOffered(SkillDefinitionSO[] offers, int count, int battleLevel)
        {
            if (_skillCards == null || _skillCards.Length < count)
            {
                _skillCards = new SkillCardData[count];
            }

            for (int i = 0; i < count; i++)
            {
                SkillDefinitionSO skill = offers[i];
                _skillCards[i] = new SkillCardData(
                    skill.DisplayName,
                    skill.Description,
                    skill.Icon,
                    skill.AccentColor,
                    _rogue.StacksOf(skill) + 1,
                    skill.MaxStacks,
                    skill.Kind == SkillKind.Active);
            }

            _flow.PauseForLevelUp();
            _ui.ShowSkillChoicePopup(_skillCards, count, battleLevel, _onSkillPicked);
        }

        private void HandleChoiceClosed() => _flow.ResumeFromLevelUp();

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
                hasNextLevel,
                result.CoinsEarned,
                result.XpEarned);
            _ui.ShowResultPopup(data, _flow.Retry, _flow.GoToNextLevel, _flow.GoToMenu);
        }
    }
}
