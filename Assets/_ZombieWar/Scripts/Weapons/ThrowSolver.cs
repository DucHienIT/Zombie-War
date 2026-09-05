using UnityEngine;
using ZombieWar.Player;

namespace ZombieWar.Weapons
{
    // Ballistics shared by everything the soldier lobs: where a throw should land, and the
    // velocity that gets it there in a fixed time.
    public static class ThrowSolver
    {
        // Lands on the aim target clamped to range, or `range` ahead along the run direction
        // (the facing when standing still) so an unaimed throw still covers the way forward.
        public static Vector3 GroundTarget(Vector3 feet, PlayerAim aim, PlayerMotor motor, float range)
        {
            Vector3 offset;
            if (aim.HasTarget)
            {
                offset = aim.TargetPosition - feet;
                offset.y = 0f;
                if (offset.sqrMagnitude > range * range)
                {
                    offset = offset.normalized * range;
                }
            }
            else
            {
                Vector3 heading = motor.NormalizedSpeed > 0f ? motor.WorldMoveDirection : motor.Forward;
                heading.y = 0f;
                offset = heading.normalized * range;
            }

            return feet + offset;
        }

        public static Vector3 LaunchVelocity(Vector3 origin, Vector3 target, float flightTime)
        {
            Vector3 displacement = target - origin;
            return displacement / flightTime - 0.5f * Physics.gravity * flightTime;
        }
    }
}
