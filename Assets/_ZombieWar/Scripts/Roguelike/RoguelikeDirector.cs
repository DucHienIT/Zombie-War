using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieWar.Core;
using ZombieWar.Data;
using ZombieWar.Enemies;
using ZombieWar.Player;

namespace ZombieWar.Roguelike
{
    // Owns the progression inside one run: kills feed XP, a full bar owes the player a pick,
    // and a pick rebuilds the stat sheet. It never freezes the game itself - it announces the
    // choice and leaves what a modal moment means to the central state machine.
    public sealed class RoguelikeDirector : MonoBehaviour
    {
        private const string LogPrefix = "[Rogue]";

        [SerializeField] private RoguelikeSettingsSO _settings;
        [SerializeField] private GameFlowController _flow;
        [SerializeField] private ZombieManager _zombies;
        [SerializeField] private PlayerStatSheet _stats;
        [SerializeField] private PlayerHealth _playerHealth;

        private readonly Dictionary<PassiveSkillSO, int> _stacks = new Dictionary<PassiveSkillSO, int>(8);
        private BattleXpTracker _xp;
        private SkillDraft _draft;
        private PassiveSkillSO[] _offers;
        private int _offerCount;
        private int _pendingLevelUps;
        private bool _choiceOpen;

        public event Action<float, int> OnXpChanged;
        public event Action<PassiveSkillSO[], int, int> OnChoiceOffered;
        public event Action OnChoiceClosed;

        private void Awake()
        {
            bool missing = _settings == null || _flow == null || _zombies == null || _stats == null || _playerHealth == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} RoguelikeDirector has an unassigned reference.", this);
                return;
            }

            if (_settings.SkillPool == null || _settings.SkillPool.Length == 0)
            {
                Debug.LogError($"{LogPrefix} {_settings.name} has an empty skill pool - no level-up could offer anything.", this);
                return;
            }

            _draft = new SkillDraft(_settings.SkillPool);
            _offers = new PassiveSkillSO[_settings.OffersPerLevelUp];
            _xp = NewTracker();
        }

        private void OnEnable()
        {
            _flow.OnRunStarted += HandleRunStarted;
            _flow.OnLevelEnded += HandleLevelEnded;
            _zombies.OnZombieKilled += HandleZombieKilled;
        }

        private void OnDisable()
        {
            _flow.OnRunStarted -= HandleRunStarted;
            _flow.OnLevelEnded -= HandleLevelEnded;
            _zombies.OnZombieKilled -= HandleZombieKilled;
        }

        private void Update()
        {
            if (_choiceOpen || _pendingLevelUps <= 0 || _flow.State != GameState.Playing)
            {
                return;
            }

            TryOpenChoice();
        }

        // Handed to the UI when a choice opens; the card the player taps comes back here.
        public void ChooseOffer(int index)
        {
            if (!_choiceOpen)
            {
                return;
            }

            if (index >= 0 && index < _offerCount)
            {
                Grant(_offers[index]);
            }
            else
            {
                Debug.LogError($"{LogPrefix} the level-up closed on card {index} with nothing to grant.", this);
            }

            _pendingLevelUps = Mathf.Max(0, _pendingLevelUps - 1);
            _choiceOpen = false;

            // A double level-up hands over the next card set straight away rather than letting
            // the run breathe for a frame in between.
            if (_pendingLevelUps > 0 && TryOpenChoice())
            {
                return;
            }

            OnChoiceClosed?.Invoke();
        }

        public int StacksOf(PassiveSkillSO skill)
        {
            _stacks.TryGetValue(skill, out int owned);
            return owned;
        }

        private void HandleRunStarted(LevelDefinitionSO level)
        {
            _xp = NewTracker();
            _stacks.Clear();
            _pendingLevelUps = 0;
            _offerCount = 0;
            _choiceOpen = false;
            RebuildStats();
            OnXpChanged?.Invoke(_xp.Normalized, _xp.Level);
        }

        private void HandleLevelEnded(LevelResult result)
        {
            _pendingLevelUps = 0;
            _choiceOpen = false;
        }

        private void HandleZombieKilled(ZombieController zombie)
        {
            if (_flow.State != GameState.Playing)
            {
                return;
            }

            float heal = _stats.Additive(StatId.HealPerKill);
            if (heal > 0f)
            {
                _playerHealth.Heal(heal);
            }

            _pendingLevelUps += _xp.AddXp(zombie.Definition.XpReward * _stats.Multiplier(StatId.XpGain));
            OnXpChanged?.Invoke(_xp.Normalized, _xp.Level);
        }

        private bool TryOpenChoice()
        {
            _offerCount = _draft.Roll(_stacks, _offers, _settings.OffersPerLevelUp);
            if (_offerCount == 0)
            {
                // Every skill sits at its cap: stop banking picks nobody can spend.
                _pendingLevelUps = 0;
                return false;
            }

            _choiceOpen = true;
            OnChoiceOffered?.Invoke(_offers, _offerCount, _xp.Level);
            return true;
        }

        private void Grant(PassiveSkillSO skill)
        {
            _stacks.TryGetValue(skill, out int owned);
            _stacks[skill] = owned + 1;
            RebuildStats();
        }

        private void RebuildStats()
        {
            _stats.BeginRebuild();
            foreach (KeyValuePair<PassiveSkillSO, int> owned in _stacks)
            {
                _stats.Apply(owned.Key.Modifiers, owned.Value);
            }

            _stats.EndRebuild();
        }

        private BattleXpTracker NewTracker()
        {
            return new BattleXpTracker(_settings.BaseXpToLevel, _settings.XpGrowthPerLevel, _settings.MaxBattleLevel);
        }
    }
}
