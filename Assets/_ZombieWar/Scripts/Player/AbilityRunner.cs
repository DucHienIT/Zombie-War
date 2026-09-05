using System.Collections.Generic;
using UnityEngine;
using ZombieWar.Core;
using ZombieWar.Data;
using ZombieWar.Weapons;

namespace ZombieWar.Player
{
    // Owns every auto-triggered ability the player has drafted, and the cooldown state those
    // abilities are not allowed to keep themselves. Ticks them from one place rather than
    // giving each ability its own Update.
    public sealed class AbilityRunner : MonoBehaviour
    {
        private const string LogPrefix = "[Ability]";
        private const int SlotCapacity = 8;

        [SerializeField] private GameFlowController _flow;
        [SerializeField] private PlayerHealth _health;
        [SerializeField] private BombThrower _bombs;
        [SerializeField] private OrbitBladesController _blades;
        [SerializeField] private MolotovThrower _molotovs;
        [SerializeField] private ChainLightningCaster _lightning;
        [SerializeField] private ShockwaveEmitter _shockwave;
        [SerializeField] private SentryDroneController _drone;

        private readonly List<Slot> _slots = new List<Slot>(SlotCapacity);
        private AbilityContext _context;

        private struct Slot
        {
            public ActiveSkillSO Skill;
            public int Stacks;
            public float CooldownRemaining;
            public bool Seen;
        }

        public int EquippedCount => _slots.Count;

        private void Awake()
        {
            bool missing = _flow == null || _health == null || _bombs == null || _blades == null || _molotovs == null
                           || _lightning == null || _shockwave == null || _drone == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} AbilityRunner has an unassigned reference.", this);
                return;
            }

            _context = new AbilityContext(transform, _bombs, _blades, _molotovs, _lightning, _shockwave, _drone);
        }

        // The three calls mirror PlayerStatSheet so the director can rebuild both from one loop.
        public void BeginRebuild()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                Slot slot = _slots[i];
                slot.Seen = false;
                _slots[i] = slot;
            }
        }

        public void Equip(ActiveSkillSO skill, int stacks)
        {
            if (skill == null || stacks <= 0)
            {
                return;
            }

            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].Skill != skill)
                {
                    continue;
                }

                // Already equipped: take the new stack count but leave the cooldown running, or
                // picking any other card would hand out a free trigger.
                Slot existing = _slots[i];
                existing.Stacks = stacks;
                existing.Seen = true;
                _slots[i] = existing;
                return;
            }

            _slots.Add(new Slot
            {
                Skill = skill,
                Stacks = stacks,
                Seen = true,
                CooldownRemaining = skill.CooldownFor(stacks),
            });
        }

        public void EndRebuild()
        {
            for (int i = _slots.Count - 1; i >= 0; i--)
            {
                if (_slots[i].Seen)
                {
                    continue;
                }

                _slots[i].Skill.OnUnequipped(_context);
                _slots.RemoveAt(i);
            }

            for (int i = 0; i < _slots.Count; i++)
            {
                _slots[i].Skill.OnEquipped(_context, _slots[i].Stacks);
            }
        }

        private void Update()
        {
            if (_flow.State != GameState.Playing || !_health.IsAlive)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            for (int i = 0; i < _slots.Count; i++)
            {
                Slot slot = _slots[i];
                float cooldown = slot.Skill.CooldownFor(slot.Stacks);
                if (cooldown <= 0f)
                {
                    continue;
                }

                slot.CooldownRemaining -= deltaTime;
                if (slot.CooldownRemaining > 0f)
                {
                    _slots[i] = slot;
                    continue;
                }

                // Ready but pointless right now (no mark, nobody in reach): hold at zero and ask
                // again next frame instead of spending the cooldown on nothing.
                if (!slot.Skill.CanTrigger(_context, slot.Stacks))
                {
                    slot.CooldownRemaining = 0f;
                    _slots[i] = slot;
                    continue;
                }

                // Written back before the trigger runs: an ability that reaches back into the
                // runner must not see a stale cooldown.
                slot.CooldownRemaining = cooldown;
                _slots[i] = slot;
                slot.Skill.Trigger(_context, slot.Stacks);
            }
        }
    }
}
