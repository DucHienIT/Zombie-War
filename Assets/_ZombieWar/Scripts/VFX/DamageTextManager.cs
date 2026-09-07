using System.Collections.Generic;
using UnityEngine;
using ZombieWar.Core;
using ZombieWar.Data;
using ZombieWar.Enemies;
using ZombieWar.Utils;

namespace ZombieWar.VFX
{
    // Turns every hit on a zombie into a floating number. It listens to the manager rather than
    // to each body, so pooled zombies never need to hand out subscriptions.
    public sealed class DamageTextManager : MonoBehaviour
    {
        private const string LogPrefix = "[VFX]";

        [SerializeField] private DamageTextDefinitionSO _definition;
        [SerializeField] private GameFlowController _flow;
        [SerializeField] private ZombieManager _zombies;
        [SerializeField] private Transform _poolParent;
        // Labels are flat meshes turned to face the camera. Its rotation is fixed for the whole
        // run, so it is read once per spawn instead of billboarded every frame.
        [SerializeField] private Transform _camera;

        private ComponentPool<DamageTextLabel> _pool;
        private List<DamageTextLabel> _active;
        private Dictionary<ZombieController, DamageTextLabel> _labelByZombie;

        private void Awake()
        {
            bool missing = _definition == null || _flow == null || _zombies == null || _poolParent == null || _camera == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} DamageTextManager has an unassigned reference.", this);
                return;
            }

            if (_definition.Prefab == null)
            {
                Debug.LogError($"{LogPrefix} {_definition.name} has no label prefab.", this);
                return;
            }

            _pool = new ComponentPool<DamageTextLabel>(_definition.Prefab, _poolParent, _definition.PoolSize);
            _active = new List<DamageTextLabel>(_definition.PoolSize);
            _labelByZombie = new Dictionary<ZombieController, DamageTextLabel>(_definition.PoolSize);
        }

        private void OnEnable()
        {
            _zombies.OnZombieDamaged += HandleZombieDamaged;
            _flow.OnRunStarted += HandleRunStarted;
            _flow.OnLevelEnded += HandleLevelEnded;
        }

        private void OnDisable()
        {
            _zombies.OnZombieDamaged -= HandleZombieDamaged;
            _flow.OnRunStarted -= HandleRunStarted;
            _flow.OnLevelEnded -= HandleLevelEnded;
        }

        private void HandleRunStarted(LevelDefinitionSO level) => DespawnAll();

        private void HandleLevelEnded(LevelResult result) => DespawnAll();

        private void HandleZombieDamaged(ZombieController zombie, float amount)
        {
            if (_pool == null || amount <= 0f || _flow.State != GameState.Playing)
            {
                return;
            }

            if (_labelByZombie.TryGetValue(zombie, out DamageTextLabel merged) && merged.Age <= _definition.MergeWindow)
            {
                merged.Add(amount);
                ApplyStyle(merged);
                return;
            }

            Vector3 anchor = zombie.AimPoint.position;
            anchor.y += _definition.HeightOffset;
            anchor.x += Random.Range(-_definition.HorizontalJitter, _definition.HorizontalJitter);

            DamageTextLabel label = _pool.Get(anchor, _camera.rotation);
            label.Show(zombie, amount, anchor);
            ApplyStyle(label);
            label.SetScale(_definition.ScaleCurve.Evaluate(0f));
            _active.Add(label);
            _labelByZombie[zombie] = label;
        }

        private void Update()
        {
            if (_active == null || _active.Count == 0)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            float lifetime = _definition.Lifetime;
            float riseDistance = _definition.RiseDistance;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                DamageTextLabel label = _active[i];
                label.Age += deltaTime;
                float t = label.Age / lifetime;
                if (t >= 1f)
                {
                    Despawn(i);
                    continue;
                }

                Vector3 point = label.Anchor;
                point.y += riseDistance * _definition.RiseCurve.Evaluate(t);
                label.SetPosition(point);
                label.SetScale(_definition.ScaleCurve.Evaluate(t));
                label.SetAlpha(_definition.FadeCurve.Evaluate(t));
            }
        }

        // Font size and colour follow the running total, so a merged shotgun blast grows into
        // the loud end of the gradient instead of staying at the size of its first pellet.
        private void ApplyStyle(DamageTextLabel label)
        {
            float loudness = Mathf.Clamp01(label.Amount / _definition.LoudDamage);
            float fontSize = Mathf.Lerp(_definition.MinFontSize, _definition.MaxFontSize, loudness);
            label.SetStyle(fontSize, _definition.DamageColor.Evaluate(loudness));
        }

        private void Despawn(int index)
        {
            DamageTextLabel label = _active[index];
            if (label.Owner != null && _labelByZombie.TryGetValue(label.Owner, out DamageTextLabel current) && current == label)
            {
                _labelByZombie.Remove(label.Owner);
            }

            _active.RemoveAtSwap(index);
            _pool.Release(label);
        }

        private void DespawnAll()
        {
            if (_active == null)
            {
                return;
            }

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Despawn(i);
            }

            _labelByZombie.Clear();
        }
    }
}
