using UnityEngine;
using ZombieWar.VFX;

namespace ZombieWar.Data
{
    [CreateAssetMenu(menuName = "Zombie War/Damage Text", fileName = "DamageText")]
    public sealed class DamageTextDefinitionSO : ScriptableObject
    {
        [Header("Pool")]
        [SerializeField] private DamageTextLabel _prefab;
        [SerializeField] private int _poolSize = 48;

        [Header("Placement")]
        // Above the zombie's aim point, so a Giant reads at its own height without a per-type number.
        [SerializeField] private float _heightOffset = 0.45f;
        // Sideways spread so two numbers on the same body do not land on top of each other.
        [SerializeField] private float _horizontalJitter = 0.3f;

        [Header("Motion")]
        [SerializeField] private float _lifetime = 0.7f;
        [SerializeField] private float _riseDistance = 1.1f;
        [SerializeField] private AnimationCurve _riseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve _scaleCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 1f);
        [SerializeField] private AnimationCurve _fadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [Header("Style")]
        [SerializeField] private float _minFontSize = 3.4f;
        [SerializeField] private float _maxFontSize = 7f;
        // Damage that reaches the largest font and the far end of the gradient; everything
        // above it reads the same, so one sniper round already looks like the heaviest hit.
        [SerializeField] private float _loudDamage = 60f;
        [SerializeField] private Gradient _damageColor;

        [Header("Merge")]
        // A second hit on the same zombie inside this window adds to the number already
        // floating instead of stacking a new one: nine shotgun pellets read as one total.
        [SerializeField] private float _mergeWindow = 0.35f;

        public DamageTextLabel Prefab => _prefab;
        public int PoolSize => _poolSize;
        public float HeightOffset => _heightOffset;
        public float HorizontalJitter => _horizontalJitter;
        public float Lifetime => _lifetime;
        public float RiseDistance => _riseDistance;
        public AnimationCurve RiseCurve => _riseCurve;
        public AnimationCurve ScaleCurve => _scaleCurve;
        public AnimationCurve FadeCurve => _fadeCurve;
        public float MinFontSize => _minFontSize;
        public float MaxFontSize => _maxFontSize;
        public float LoudDamage => _loudDamage;
        public Gradient DamageColor => _damageColor;
        public float MergeWindow => _mergeWindow;
    }
}
