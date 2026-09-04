using UnityEngine;

namespace ZombieWar.EditorTools
{
    // Authoring recipe for the animator builder. Swap clips here (for example Mixamo zombie clips) and rebuild.
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

        [Header("Zombie")]
        [SerializeField] private AnimationClip _zombieIdle;
        [SerializeField] private AnimationClip _zombieWalk;
        [SerializeField] private AnimationClip _zombieAttack;
        [SerializeField] private AnimationClip _zombieHit;
        [SerializeField] private AnimationClip _zombieDeath;

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
        public AnimationClip ZombieAttack => _zombieAttack;
        public AnimationClip ZombieHit => _zombieHit;
        public AnimationClip ZombieDeath => _zombieDeath;

        public void AssignDefaults(AnimationClip idle, AnimationClip walk, AnimationClip run, AnimationClip runBack,
            AnimationClip combatIdle, AnimationClip shoot)
        {
            _soldierIdle = idle;
            _soldierWalkForward = walk;
            _soldierRunForward = run;
            _soldierRunBackward = runBack;
            _soldierCombatIdle = combatIdle;
            _soldierShoot = shoot;
            // No zombie clips ship with the imported packs; the humanoid soldier clips retarget as placeholders.
            _zombieIdle = idle;
            _zombieWalk = walk;
        }
    }
}
