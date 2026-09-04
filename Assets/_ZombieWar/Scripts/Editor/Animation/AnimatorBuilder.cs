using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ZombieWar.EditorTools
{
    public static class AnimatorBuilder
    {
        private const string LogPrefix = "[Animator]";
        private const string RecipePath = "Assets/_ZombieWar/Animation/AnimatorBuildRecipe.asset";

        private const string SurvivalistClipFolder = "Assets/Survivalist/StarterAssets/ThirdPersonController/Character/Animations/";
        private const string ToonSoldierClipFolder = "Assets/ToonSoldiers_WW2_demo/animation/";

        private const float InstantTransition = 0.02f;
        private const float ShortTransition = 0.1f;
        private const float WalkBlendDistance = 0.5f;

        [MenuItem("Tools/Zombie War/Animation/Create Default Animator Recipe")]
        private static void CreateDefaultRecipe()
        {
            var recipe = AssetDatabase.LoadAssetAtPath<AnimatorBuildRecipeSO>(RecipePath);
            if (recipe == null)
            {
                recipe = ScriptableObject.CreateInstance<AnimatorBuildRecipeSO>();
                AssetDatabase.CreateAsset(recipe, RecipePath);
            }

            recipe.AssignDefaults(
                LoadClip(SurvivalistClipFolder + "Stand--Idle.anim.fbx", "Idle"),
                LoadClip(SurvivalistClipFolder + "Locomotion--Walk_N.anim.fbx", "Walk_N"),
                LoadClip(SurvivalistClipFolder + "Locomotion--Run_N.anim.fbx", "Run_N"),
                LoadClip(SurvivalistClipFolder + "Locomotion--Run_S.anim.fbx", "Run_S"),
                LoadClip(ToonSoldierClipFolder + "infantry_combat_idle.FBX", "infantry_combat_idle"),
                LoadClip(ToonSoldierClipFolder + "infantry_combat_shoot.FBX", "infantry_combat_shoot"));
            EditorUtility.SetDirty(recipe);
            AssetDatabase.SaveAssets();
            Debug.Log($"{LogPrefix} Recipe written to {RecipePath}.", recipe);
        }

        [MenuItem("Tools/Zombie War/Animation/Build Animators")]
        private static void BuildAnimators()
        {
            var recipe = AssetDatabase.LoadAssetAtPath<AnimatorBuildRecipeSO>(RecipePath);
            if (recipe == null)
            {
                Debug.LogError($"{LogPrefix} Recipe missing at {RecipePath}. Run Create Default Animator Recipe first.");
                return;
            }

            AvatarMask upperBodyMask = BuildUpperBodyMask(recipe.UpperBodyMaskPath);
            BuildSoldier(recipe, upperBodyMask);
            BuildZombie(recipe, upperBodyMask);
            AssetDatabase.SaveAssets();
            Debug.Log($"{LogPrefix} Built {recipe.SoldierControllerPath} and {recipe.ZombieControllerPath}.");
        }

        private static void BuildSoldier(AnimatorBuildRecipeSO recipe, AvatarMask upperBodyMask)
        {
            AnimatorController controller = PrepareController(recipe.SoldierControllerPath);
            controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
            controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Shoot", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Reload", AnimatorControllerParameterType.Bool);
            controller.AddParameter("WeaponType", AnimatorControllerParameterType.Int);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);

            BuildSoldierLocomotionLayer(controller, recipe);
            BuildSoldierCombatLayer(controller, recipe, upperBodyMask);
            BuildHitReactionLayer(controller, recipe.SoldierHit, upperBodyMask);
            BuildFullBodyLayer(controller, recipe.SoldierDeath);
        }

        private static void BuildSoldierLocomotionLayer(AnimatorController controller, AnimatorBuildRecipeSO recipe)
        {
            AnimatorStateMachine machine = AddLayer(controller, "Base Locomotion", null, AnimatorLayerBlendingMode.Override);
            AnimatorState locomotion = controller.CreateBlendTreeInController("Locomotion", out BlendTree tree, controller.layers.Length - 1);
            tree.blendType = BlendTreeType.FreeformDirectional2D;
            tree.blendParameter = "MoveX";
            tree.blendParameterY = "MoveY";
            tree.AddChild(recipe.SoldierIdle, Vector2.zero);
            tree.AddChild(recipe.SoldierWalkForward, new Vector2(0f, WalkBlendDistance));
            tree.AddChild(recipe.SoldierRunForward, Vector2.up);
            tree.AddChild(recipe.SoldierRunBackward, Vector2.down);
            // No strafe clips in the packs: reuse the forward run for sideways motion.
            tree.AddChild(recipe.SoldierRunForward, Vector2.right);
            tree.AddChild(recipe.SoldierRunForward, Vector2.left);
            machine.defaultState = locomotion;
        }

        private static void BuildSoldierCombatLayer(AnimatorController controller, AnimatorBuildRecipeSO recipe, AvatarMask mask)
        {
            AnimatorStateMachine machine = AddLayer(controller, "Upper Combat", mask, AnimatorLayerBlendingMode.Override);
            AnimatorState combatIdle = AddState(machine, "CombatIdle", recipe.SoldierCombatIdle);
            AnimatorState shoot = AddState(machine, "Shoot", recipe.SoldierShoot);
            AnimatorState reload = AddState(machine, "Reload", recipe.SoldierReload);
            machine.defaultState = combatIdle;

            AddTriggerTransition(combatIdle, shoot, "Shoot", InstantTransition);
            AddTriggerTransition(shoot, shoot, "Shoot", InstantTransition);
            AddExitTimeTransition(shoot, combatIdle, ShortTransition);
            AddBoolTransition(combatIdle, reload, "Reload", true, ShortTransition);
            AddBoolTransition(shoot, reload, "Reload", true, ShortTransition);
            AddBoolTransition(reload, combatIdle, "Reload", false, ShortTransition);
        }

        private static void BuildHitReactionLayer(AnimatorController controller, AnimationClip hitClip, AvatarMask mask)
        {
            AnimatorStateMachine machine = AddLayer(controller, "Hit Reaction", mask, AnimatorLayerBlendingMode.Additive);
            AnimatorState empty = AddState(machine, "Empty", null);
            AnimatorState hit = AddState(machine, "Hit", hitClip);
            machine.defaultState = empty;
            AddTriggerTransition(empty, hit, "Hit", InstantTransition);
            AddExitTimeTransition(hit, empty, ShortTransition);
        }

        private static void BuildFullBodyLayer(AnimatorController controller, AnimationClip deathClip)
        {
            AnimatorStateMachine machine = AddLayer(controller, "Full Body", null, AnimatorLayerBlendingMode.Override);
            AnimatorState empty = AddState(machine, "Empty", null);
            AnimatorState death = AddState(machine, "Death", deathClip);
            machine.defaultState = empty;
            AddTriggerTransition(empty, death, "Death", ShortTransition);
        }

        private static void BuildZombie(AnimatorBuildRecipeSO recipe, AvatarMask upperBodyMask)
        {
            AnimatorController controller = PrepareController(recipe.ZombieControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Knockback", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine machine = AddLayer(controller, "Base", null, AnimatorLayerBlendingMode.Override);
            AnimatorState locomotion = controller.CreateBlendTreeInController("Locomotion", out BlendTree tree, controller.layers.Length - 1);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            tree.AddChild(recipe.ZombieIdle, 0f);
            tree.AddChild(recipe.ZombieWalk, 1f);

            AnimatorState attack = AddState(machine, "Attack", recipe.ZombieAttack);
            AnimatorState knockback = AddState(machine, "Knockback", recipe.ZombieHit);
            AnimatorState death = AddState(machine, "Death", recipe.ZombieDeath);
            machine.defaultState = locomotion;

            AddTriggerTransition(locomotion, attack, "Attack", ShortTransition);
            AddExitTimeTransition(attack, locomotion, ShortTransition);
            AddBoolTransition(locomotion, knockback, "Knockback", true, ShortTransition);
            AddBoolTransition(attack, knockback, "Knockback", true, ShortTransition);
            AddBoolTransition(knockback, locomotion, "Knockback", false, ShortTransition);

            AnimatorStateTransition toDeath = machine.AddAnyStateTransition(death);
            toDeath.canTransitionToSelf = false;
            toDeath.hasExitTime = false;
            toDeath.duration = ShortTransition;
            toDeath.AddCondition(AnimatorConditionMode.If, 0f, "Death");

            BuildHitReactionLayer(controller, recipe.ZombieHit, upperBodyMask);
        }

        private static AnimatorController PrepareController(string path)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            }

            // Rebuild in place so prefabs keep their GUID reference; drop every stale sub-asset first.
            // A freshly created controller also carries an empty default layer that must go.
            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < subAssets.Length; i++)
            {
                if (subAssets[i] != controller && subAssets[i] != null)
                {
                    Object.DestroyImmediate(subAssets[i], true);
                }
            }

            controller.layers = new AnimatorControllerLayer[0];
            controller.parameters = new AnimatorControllerParameter[0];
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimatorStateMachine AddLayer(AnimatorController controller, string name, AvatarMask mask, AnimatorLayerBlendingMode blending)
        {
            var machine = new AnimatorStateMachine { name = name, hideFlags = HideFlags.HideInHierarchy };
            AssetDatabase.AddObjectToAsset(machine, controller);
            var layer = new AnimatorControllerLayer
            {
                name = name,
                stateMachine = machine,
                avatarMask = mask,
                blendingMode = blending,
                defaultWeight = 1f
            };
            controller.AddLayer(layer);
            return machine;
        }

        private static AnimatorState AddState(AnimatorStateMachine machine, string name, AnimationClip clip)
        {
            AnimatorState state = machine.AddState(name);
            state.motion = clip;
            if (clip == null)
            {
                Debug.Log($"{LogPrefix} State {name} in {machine.name} has no clip in the recipe; it stays empty until one is assigned.");
            }

            return state;
        }

        private static void AddTriggerTransition(AnimatorState from, AnimatorState to, string trigger, float duration)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = duration;
            transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        }

        private static void AddBoolTransition(AnimatorState from, AnimatorState to, string parameter, bool value, float duration)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = duration;
            transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);
        }

        private static void AddExitTimeTransition(AnimatorState from, AnimatorState to, float duration)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = true;
            transition.exitTime = 1f;
            transition.duration = duration;
        }

        private static AvatarMask BuildUpperBodyMask(string path)
        {
            var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(path);
            if (mask == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                mask = new AvatarMask();
                AssetDatabase.CreateAsset(mask, path);
            }

            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
            {
                mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
            }

            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
            EditorUtility.SetDirty(mask);
            return mask;
        }

        private static AnimationClip LoadClip(string fbxPath, string clipName)
        {
            Object[] representations = AssetDatabase.LoadAllAssetRepresentationsAtPath(fbxPath);
            for (int i = 0; i < representations.Length; i++)
            {
                if (representations[i] is AnimationClip clip && clip.name == clipName)
                {
                    return clip;
                }
            }

            Debug.LogError($"{LogPrefix} Clip {clipName} not found in {fbxPath}.");
            return null;
        }
    }
}
