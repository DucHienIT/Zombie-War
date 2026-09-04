using UnityEngine;

namespace ZombieWar.Core
{
    public readonly struct DamageInfo
    {
        public readonly float Amount;
        public readonly Vector3 HitPoint;
        public readonly Vector3 Direction;
        public readonly float Force;
        public readonly DamageSource Source;

        public DamageInfo(float amount, Vector3 hitPoint, Vector3 direction, float force, DamageSource source)
        {
            Amount = amount;
            HitPoint = hitPoint;
            Direction = direction;
            Force = force;
            Source = source;
        }
    }
}
