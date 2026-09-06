using System.Collections.Generic;
using UnityEngine;
using ZombieWar.Audio;
using ZombieWar.Core;
using ZombieWar.Data;
using ZombieWar.Enemies;
using ZombieWar.Level;
using ZombieWar.Player;
using ZombieWar.Utils;

namespace ZombieWar.Weapons
{
    // Lobs bottles and runs the fire patches they leave behind. Pacing belongs to the Molotov
    // skill; this only decides where a bottle lands and what its fire does to whoever stands in
    // it. The soldier's own fire never burns the soldier: it is thrown ahead every few seconds
    // and the player would be running through it constantly.
    public sealed class MolotovThrower : MonoBehaviour
    {
        private const string LogPrefix = "[Molotov]";
        private const int BurnBufferSize = 64;
        // A shattered bottle burns at once; the zone's telegraph phase belongs to level hazards.
        private const float NoTelegraph = 0f;

        [SerializeField] private MolotovDefinitionSO _definition;
        [SerializeField] private Molotov _prefab;
        [SerializeField] private Transform _bottlePoolParent;
        [SerializeField] private Transform _firePoolParent;
        [SerializeField] private Transform _throwOrigin;
        [SerializeField] private PlayerAim _aim;
        [SerializeField] private PlayerMotor _motor;
        [SerializeField] private PlayerHealth _health;
        [SerializeField] private GameFlowController _flow;
        [SerializeField] private ZombieManager _zombies;
        [SerializeField] private AudioService _audio;
        [SerializeField] private LayerMask _burnMask;

        private readonly Collider[] _burnBuffer = new Collider[BurnBufferSize];
        private ComponentPool<Molotov> _bottlePool;
        private ComponentPool<FireZone> _firePool;
        private List<Flight> _flights;
        private List<Fire> _fires;
        private Transform _transform;

        private struct Flight
        {
            public Molotov Bottle;
            public MolotovBurn Burn;
        }

        private struct Fire
        {
            public FireZone Zone;
            public MolotovBurn Burn;
        }

        private void Awake()
        {
            _transform = transform;
            bool missing = _definition == null || _prefab == null || _bottlePoolParent == null || _firePoolParent == null
                           || _throwOrigin == null || _aim == null || _motor == null || _health == null || _flow == null
                           || _zombies == null || _audio == null;
            if (missing || _definition.FirePrefab == null)
            {
                Debug.LogError($"{LogPrefix} MolotovThrower has an unassigned reference.", this);
                return;
            }

            _bottlePool = new ComponentPool<Molotov>(_prefab, _bottlePoolParent, _definition.BottlePoolSize);
            _firePool = new ComponentPool<FireZone>(_definition.FirePrefab, _firePoolParent, _definition.FirePoolSize);
            _flights = new List<Flight>(_definition.BottlePoolSize);
            _fires = new List<Fire>(_definition.FirePoolSize);
        }

        // One bottle per ability trigger. The only refusal is the run not being playable.
        public void Throw(in MolotovBurn burn)
        {
            if (_flow.State != GameState.Playing || !_health.IsAlive)
            {
                return;
            }

            Vector3 origin = _throwOrigin.position;
            Vector3 target = ThrowSolver.GroundTarget(_transform.position, _aim, _motor, _definition.ThrowRange);
            Molotov bottle = _bottlePool.Get(origin, Quaternion.identity);
            bottle.Launch(ThrowSolver.LaunchVelocity(origin, target, _definition.FlightTime), _definition.FlightTime);
            _flights.Add(new Flight { Bottle = bottle, Burn = burn });
            _audio.PlayWorld(_definition.ThrowClip, origin);
        }

        // Keeps running through the death slow-motion, like the bombs do, so a bottle already in
        // the air still lands and a lit patch keeps burning instead of freezing mid-frame.
        private void Update()
        {
            GameState state = _flow.State;
            if (state != GameState.Playing && state != GameState.Lost)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            TickFlights(deltaTime);
            TickFires(deltaTime);
        }

        private void TickFlights(float deltaTime)
        {
            for (int i = _flights.Count - 1; i >= 0; i--)
            {
                Flight flight = _flights[i];
                if (!flight.Bottle.TickFlight(deltaTime))
                {
                    continue;
                }

                Ignite(flight.Bottle.Position, flight.Burn);
                _flights.RemoveAtSwap(i);
                _bottlePool.Release(flight.Bottle);
            }
        }

        private void Ignite(Vector3 position, in MolotovBurn burn)
        {
            FireZone zone = _firePool.Get(position, Quaternion.identity);
            zone.Ignite(burn.Radius, NoTelegraph, burn.Duration, burn.TickInterval);
            _fires.Add(new Fire { Zone = zone, Burn = burn });
            _audio.PlayWorld(_definition.IgniteClip, position);
        }

        private void TickFires(float deltaTime)
        {
            for (int i = _fires.Count - 1; i >= 0; i--)
            {
                Fire fire = _fires[i];
                if (fire.Zone.Tick(deltaTime))
                {
                    BurnTick(fire.Zone.Position, fire.Burn);
                }

                if (fire.Zone.CurrentPhase != FireZone.Phase.Finished)
                {
                    continue;
                }

                _fires.RemoveAtSwap(i);
                _firePool.Release(fire.Zone);
            }
        }

        private void BurnTick(Vector3 center, in MolotovBurn burn)
        {
            int count = Physics.OverlapSphereNonAlloc(center, burn.Radius, _burnBuffer, _burnMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (!_zombies.TryGetZombie(_burnBuffer[i], out ZombieController zombie) || !zombie.IsTargetable)
                {
                    continue;
                }

                Vector3 direction = zombie.Position - center;
                direction.y = 0f;
                zombie.TakeDamage(new DamageInfo(burn.DamagePerTick, zombie.Position, direction.normalized, 0f, DamageSource.Fire));
            }
        }

    }
}
