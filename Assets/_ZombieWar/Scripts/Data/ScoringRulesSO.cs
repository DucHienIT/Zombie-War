using UnityEngine;

namespace ZombieWar.Data
{
    [CreateAssetMenu(menuName = "Zombie War/Scoring Rules", fileName = "ScoringRules")]
    public sealed class ScoringRulesSO : ScriptableObject
    {
        [SerializeField] private float _multiKillWindow = 1f;
        [SerializeField] private int _multiKillBonus = 5;
        [SerializeField] private float _healthBonusPerPercent = 5f;

        public float MultiKillWindow => _multiKillWindow;
        public int MultiKillBonus => _multiKillBonus;
        public float HealthBonusPerPercent => _healthBonusPerPercent;
    }
}
