using System.Collections.Generic;
using UnityEngine;
using ZombieWar.Audio;
using ZombieWar.Core;
using ZombieWar.Data;
using ZombieWar.Enemies;
using ZombieWar.Player;
using ZombieWar.Utils;
using ZombieWar.VFX;

namespace ZombieWar.Roguelike
{
    // Turns every kill into an orb on the ground and every pickup into experience. Kills no
    // longer pay out by themselves: the player has to walk over to what they earned, which
    // is what makes positioning matter in a crowd.
    public sealed class XpOrbManager : MonoBehaviour
    {
        private const string LogPrefix = "[Rogue]";
        private const float TwoPi = Mathf.PI * 2f;

        [SerializeField] private XpOrbDefinitionSO _definition;
        [SerializeField] private GameFlowController _flow;
        [SerializeField] private ZombieManager _zombies;
        [SerializeField] private RoguelikeDirector _rogue;
        [SerializeField] private PlayerHealth _playerHealth;
        // Read for position only.
        [SerializeField] private Transform _player;
        [SerializeField] private Transform _poolParent;
        [SerializeField] private VfxService _vfx;
        [SerializeField] private AudioService _audio;

        private ComponentPool<XpOrb> _pool;
        private List<XpOrb> _active;

        private void Awake()
        {
            bool missing = _definition == null || _flow == null || _zombies == null || _rogue == null || _playerHealth == null
                           || _player == null || _vfx == null || _audio == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} XpOrbManager has an unassigned reference.", this);
                return;
            }

            if (_definition.Prefab == null)
            {
                Debug.LogError($"{LogPrefix} {_definition.name} has no orb prefab.", this);
                return;
            }

            _pool = new ComponentPool<XpOrb>(_definition.Prefab, _poolParent, _definition.PoolSize);
            _active = new List<XpOrb>(_definition.PoolSize);
        }

        private void OnEnable()
        {
            _zombies.OnZombieKilled += HandleZombieKilled;
            _flow.OnRunStarted += HandleRunStarted;
            _flow.OnLevelEnded += HandleLevelEnded;
        }

        private void OnDisable()
        {
            _zombies.OnZombieKilled -= HandleZombieKilled;
            _flow.OnRunStarted -= HandleRunStarted;
            _flow.OnLevelEnded -= HandleLevelEnded;
        }

        private void HandleRunStarted(LevelDefinitionSO level) => DespawnAll();

        private void HandleLevelEnded(LevelResult result) => DespawnAll();

        private void HandleZombieKilled(ZombieController zombie)
        {
            if (_flow.State != GameState.Playing)
            {
                return;
            }

            Vector3 dropPoint = zombie.Position;
            Vector2 scatter = Random.insideUnitCircle * _definition.ScatterRadius;
            Vector3 restPoint = dropPoint + new Vector3(scatter.x, _definition.HoverHeight, scatter.y);
            XpOrb orb = _pool.Get(dropPoint, Quaternion.identity);
            orb.Drop(zombie.Definition.XpReward, dropPoint, restPoint);
            _active.Add(orb);
        }

        private void Update()
        {
            if (_active.Count == 0 || _flow.State != GameState.Playing)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            float spin = _definition.SpinDegreesPerSecond * deltaTime;
            Vector3 playerPoint = _player.position + Vector3.up * _definition.HoverHeight;
            bool canCollect = _playerHealth.IsAlive;
            float magnetSqr = _definition.MagnetRadius * _definition.MagnetRadius;
            float collectSqr = _definition.CollectRadius * _definition.CollectRadius;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                XpOrb orb = _active[i];
                orb.Age += deltaTime;
                orb.Spin(spin);

                if (orb.Attracted)
                {
                    if (Fly(orb, playerPoint, deltaTime, collectSqr))
                    {
                        Collect(i);
                    }

                    continue;
                }

                Settle(orb);
                if (canCollect && (orb.Position - playerPoint).sqrMagnitude <= magnetSqr)
                {
                    orb.Attracted = true;
                    orb.FlySpeed = _definition.FlySpeed;
                }
            }
        }

        // The pop is a short arc from the corpse to the rest point; after that the orb only bobs.
        private void Settle(XpOrb orb)
        {
            float popDuration = _definition.PopDuration;
            if (orb.Age < popDuration)
            {
                float t = orb.Age / popDuration;
                Vector3 point = Vector3.Lerp(orb.DropPoint, orb.RestPoint, t);
                point.y += _definition.PopHeight * 4f * t * (1f - t);
                orb.SetPosition(point);
                return;
            }

            Vector3 rest = orb.RestPoint;
            rest.y += Mathf.Sin((orb.Age - popDuration) * _definition.BobFrequency * TwoPi) * _definition.BobAmplitude;
            orb.SetPosition(rest);
        }

        // True once the orb has reached the player. Speed keeps climbing so a far pickup
        // still arrives fast instead of trailing a running player forever.
        private bool Fly(XpOrb orb, Vector3 target, float deltaTime, float collectSqr)
        {
            orb.FlySpeed += _definition.FlyAcceleration * deltaTime;
            Vector3 position = Vector3.MoveTowards(orb.Position, target, orb.FlySpeed * deltaTime);
            orb.SetPosition(position);
            return (position - target).sqrMagnitude <= collectSqr;
        }

        private void Collect(int index)
        {
            XpOrb orb = _active[index];
            Vector3 point = orb.Position;
            _rogue.CollectXp(orb.Value);
            if (_definition.CollectVfx != null)
            {
                _vfx.Play(_definition.CollectVfx, point, Quaternion.identity);
            }

            if (_definition.CollectClip != null)
            {
                _audio.PlayWorld(_definition.CollectClip, point);
            }

            Despawn(index);
        }

        private void Despawn(int index)
        {
            XpOrb orb = _active[index];
            _active.RemoveAtSwap(index);
            _pool.Release(orb);
        }

        private void DespawnAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Despawn(i);
            }
        }
    }
}
