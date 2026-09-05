using UnityEngine;

namespace ZombieWar.EditorTools
{
    // Authoring recipe for the animator builder. Swap clips here and rebuild.
    [CreateAssetMenu(menuName = "Zombie War/Editor/Animator Build Recipe", fileName = "AnimatorBuildRecipe")]
    public sealed class AnimatorBuildRecipeSO : ScriptableObject
    {
        [Header("Output")]
        [SerializeField] private string _soldierControllerPath = "Assets/_ZombieWar/Animation/Soldier/SoldierAnimator.controller";
        [SerializeField] private string _upperBodyMaskPath = "Assets/_ZombieWar/Animation/Soldier/UpperBodyMask.mask";
        [SerializeField] private string _zombieControllerPath = "Assets/_ZombieWar/Animation/Zombie/ZombieAnimator.controller";

        [Header("Soldier Locomotion")]
        [SerializeField] private AnimationClip _soldierIdle;
        [SerializeField] private AnimationClip _soldierWalkForward;
        [SerializeField] private AnimationClip _soldierRunForward;
        [SerializeField] private AnimationClip _soldierRunBackward;

        [Header("Soldier Upper Body")]
        [SerializeField] private AnimationClip _soldierCombatIdle;
        [SerializeField] private AnimationClip _soldierShoot;
        [SerializeField] private AnimationClip _soldierReload;
        [SerializeField] private AnimationClip _soldierHit;
        [SerializeField] private AnimationClip _soldierDeath;

        [Header("Zombie Locomotion")]
        [SerializeField] private AnimationClip _zombieIdle;
        [SerializeField] private AnimationClip _zombieWalk;
        [SerializeField] private AnimationClip _zombieWalkFast;
        [SerializeField] private AnimationClip _zombieRun;
        // Blend thresholds in m/s: the Speed parameter carries the agent's real velocity.
        [SerializeField] private float _zombieWalkSpeed = 1.4f;
        [SerializeField] private float _zombieWalkFastSpeed = 2.3f;
        [SerializeField] private float _zombieRunSpeed = 3.6f;

        [Header("Zombie Actions")]
        [SerializeField] private AnimationClip _zombieAttack;
        [SerializeField] private AnimationClip _zombieHit;
        [SerializeField] private AnimationClip _zombieKnockback;
        [SerializeField] private AnimationClip _zombieDeath;
        // The pack's 2.33 s swing and flinch are far slower than the 1 s attack cadence; states play them faster.
        [SerializeField] private float _zombieAttackPlaybackSpeed = 2f;
        [SerializeField] private float _zombieHitPlaybackSpeed = 3f;

        public string SoldierControllerPath => _soldierControllerPath;
        public string UpperBodyMaskPath => _upperBodyMaskPath;
        public string ZombieControllerPath => _zombieControllerPath;
        public AnimationClip SoldierIdle => _soldierIdle;
        public AnimationClip SoldierWalkForward => _soldierWalkForward;
        public AnimationClip SoldierRunForward => _soldierRunForward;
        public AnimationClip SoldierRunBackward => _soldierRunBackward;
        public AnimationClip SoldierCombatIdle => _soldierCombatIdle;
        public AnimationClip SoldierShoot => _soldierShoot;
        public AnimationClip SoldierReload => _soldierReload;
        public AnimationClip SoldierHit => _soldierHit;
        public AnimationClip SoldierDeath => _soldierDeath;
        public AnimationClip ZombieIdle => _zombieIdle;
        public AnimationClip ZombieWalk => _zombieWalk;
        public AnimationClip ZombieWalkFast => _zombieWalkFast;
        public AnimationClip ZombieRun => _zombieRun;
        public float ZombieWalkSpeed => _zombieWalkSpeed;
        public float ZombieWalkFastSpeed => _zombieWalkFastSpeed;
        public float ZombieRunSpeed => _zombieRunSpeed;
        public AnimationClip ZombieAttack => _zombieAttack;
        public AnimationClip ZombieHit => _zombieHit;
        public AnimationClip ZombieKnockback => _zombieKnockback;
        public AnimationClip ZombieDeath => _zombieDeath;
        public float ZombieAttackPlaybackSpeed => _zombieAttackPlaybackSpeed;
        public float ZombieHitPlaybackSpeed => _zombieHitPlaybackSpeed;

        public void AssignSoldierDefaults(AnimationClip idle, AnimationClip walk, AnimationClip run, AnimationClip runBack,
            AnimationClip combatIdle, AnimationClip shoot)
        {
            _soldierIdle = idle;
            _soldierWalkForward = walk;
            _soldierRunForward = run;
            _soldierRunBackward = runBack;
            _soldierCombatIdle = combatIdle;
            _soldierShoot = shoot;
        }

        public void AssignZombieClips(AnimationClip idle, AnimationClip walk, AnimationClip walkFast, AnimationClip run,
            AnimationClip attack, AnimationClip hit, AnimationClip knockback, AnimationClip death)
        {
            _zombieIdle = idle;
            _zombieWalk = walk;
            _zombieWalkFast = walkFast;
            _zombieRun = run;
            _zombieAttack = attack;
            _zombieHit = hit;
            _zombieKnockback = knockback;
            _zombieDeath = death;
        }
    }
}
