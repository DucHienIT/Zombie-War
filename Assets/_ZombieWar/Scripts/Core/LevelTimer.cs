using UnityEngine;

namespace ZombieWar.Core
{
    public sealed class LevelTimer
    {
        private readonly float _duration;

        public LevelTimer(float duration)
        {
            _duration = duration;
            Remaining = duration;
        }

        public float Remaining { get; private set; }
        public float Elapsed => _duration - Remaining;
        public bool IsFinished => Remaining <= 0f;

        public void Tick(float deltaTime)
        {
            if (IsFinished)
            {
                return;
            }

            Remaining = Mathf.Max(0f, Remaining - deltaTime);
        }
    }
}
