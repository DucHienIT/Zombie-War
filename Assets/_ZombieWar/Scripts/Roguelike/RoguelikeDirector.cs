using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieWar.Core;
using ZombieWar.Data;
using ZombieWar.Enemies;
using ZombieWar.Player;

namespace ZombieWar.Roguelike
{
    // Owns the progression inside one run: collected orbs feed XP, a full bar owes the player a pick,
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
        [SerializeField] private AbilityRunner _abilities;

        private readonly Dictionary<SkillDefinitionSO, int> _stacks = new Dictionary<SkillDefinitionSO, int>(8);
        private BattleXpTracker _xp;
        private SkillDraft _draft;
        private SkillDraft _firstLevelDraft;
        private SkillDefinitionSO[] _offers;
        private bool _firstChoiceDone;
        private int _offerCount;
        private int _pendingLevelUps;
        private bool _choiceOpen;

        public event Action<float, int> OnXpChanged;
        public event Action<SkillDefinitionSO[], int, int> OnChoiceOffered;
        public event Action OnChoiceClosed;

        private void Awake()
        {
            bool missing = _settings == null || _flow == null || _zombies == null || _stats == null || _playerHealth == null
                           || _abilities == null;
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
            ActiveSkillSO[] firstLevelPool = _settings.FirstLevelPool;
            _firstLevelDraft = firstLevelPool != null && firstLevelPool.Length > 0 ? new SkillDraft(firstLevelPool) : null;
            _offers = new SkillDefinitionSO[_settings.OffersPerLevelUp];
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
            _firstChoiceDone = true;

            // A double level-up hands over the next card set straight away rather than letting
            // the run breathe for a frame in between.
            if (_pendingLevelUps > 0 && TryOpenChoice())
            {
                return;
            }

            OnChoiceClosed?.Invoke();
        }

        // Experience arrives from XpOrbManager when the player walks into an orb, not from
        // the kill itself: the drop has to be picked up to count.
        public void CollectXp(float baseXp)
        {
            if (_flow.State != GameState.Playing)
            {
                return;
            }

            _pendingLevelUps += _xp.AddXp(baseXp * _stats.Multiplier(StatId.XpGain));
            OnXpChanged?.Invoke(_xp.Normalized, _xp.Level);
        }

        public int StacksOf(SkillDefinitionSO skill)
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
            _firstChoiceDone = false;
            // The bomb is a spec feature, so it is owned from the start rather than drafted.
            // Seeding it here keeps the rebuild loop uniform and the card levels honest.
            if (_settings.StartingAbility != null)
            {
                _stacks[_settings.StartingAbility] = 1;
            }

            RebuildLoadout();
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
        }

        private bool TryOpenChoice()
        {
            // The very first pick is drawn from the ability pool so no run goes the whole way
            // without one; after that both kinds share a pool.
            bool useFirstLevelPool = !_firstChoiceDone && _firstLevelDraft != null;
            SkillDraft draft = useFirstLevelPool ? _firstLevelDraft : _draft;
            _offerCount = draft.Roll(_stacks, _offers, _settings.OffersPerLevelUp);
            if (_offerCount == 0 && useFirstLevelPool)
            {
                _offerCount = _draft.Roll(_stacks, _offers, _settings.OffersPerLevelUp);
            }

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

        private void Grant(SkillDefinitionSO skill)
        {
            _stacks.TryGetValue(skill, out int owned);
            _stacks[skill] = owned + 1;
            RebuildLoadout();
        }

        // One pass over everything owned. Each skill writes itself into the sheet or the runner,
        // so a new kind of skill needs no branch here.
        private void RebuildLoadout()
        {
            _stats.BeginRebuild();
            _abilities.BeginRebuild();

            var context = new SkillApplyContext(_stats, _abilities);
            foreach (KeyValuePair<SkillDefinitionSO, int> owned in _stacks)
            {
                owned.Key.Apply(context, owned.Value);
            }

            _stats.EndRebuild();
            _abilities.EndRebuild();
        }

        private BattleXpTracker NewTracker()
        {
            return new BattleXpTracker(_settings.BaseXpToLevel, _settings.XpGrowthPerLevel, _settings.MaxBattleLevel);
        }
    }
}
