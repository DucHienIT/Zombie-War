using UnityEngine;

namespace ZombieWar.Core
{
    public sealed class ScoreTracker
    {
        private readonly float _multiKillWindow;
        private readonly int _multiKillBonus;
        private readonly float _healthBonusPerPercent;
        private float _lastKillTime = float.NegativeInfinity;

        public ScoreTracker(float multiKillWindow, int multiKillBonus, float healthBonusPerPercent)
        {
            _multiKillWindow = multiKillWindow;
            _multiKillBonus = multiKillBonus;
            _healthBonusPerPercent = healthBonusPerPercent;
        }

        public int Kills { get; private set; }
        public int Score { get; private set; }

        public void RegisterKill(int reward, float time)
        {
            Kills++;
            bool isMultiKill = time - _lastKillTime <= _multiKillWindow;
            Score += isMultiKill ? reward + _multiKillBonus : reward;
            _lastKillTime = time;
        }

        public int ComputeHealthBonus(float healthNormalized)
        {
            return Mathf.RoundToInt(Mathf.Clamp01(healthNormalized) * 100f * _healthBonusPerPercent);
        }
    }
}
