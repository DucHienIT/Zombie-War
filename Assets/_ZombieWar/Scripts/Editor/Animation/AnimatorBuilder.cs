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

        private const string SurvivalistClipFolder = "Assets/ThirdParty/Survivalist/StarterAssets/ThirdPersonController/Character/Animations/";
        private const string ToonSoldierClipFolder = "Assets/ThirdParty/ToonSoldiers_WW2_demo/animation/";
        private const string ZombiePackClipFolder = "Assets/ThirdParty/Zombie_Animations/Animations/";

        private const float InstantTransition = 0.02f;
        private const float ShortTransition = 0.1f;
        private const float WalkBlendDistance = 0.5f;

        [MenuItem("Tools/Zombie War/Animation/1. Create Default Animator Recipe")]
        private static void CreateDefaultRecipe()
        {
            AnimatorBuildRecipeSO recipe = LoadOrCreateRecipe();
            recipe.AssignSoldierDefaults(
                LoadClip(SurvivalistClipFolder + "Stand--Idle.anim.fbx"),
                LoadClip(SurvivalistClipFolder + "Locomotion--Walk_N.anim.fbx"),
                LoadClip(SurvivalistClipFolder + "Locomotion--Run_N.anim.fbx"),
                LoadClip(SurvivalistClipFolder + "Locomotion--Run_S.anim.fbx"),
                LoadClip(ToonSoldierClipFolder + "infantry_combat_idle.FBX"),
                LoadClip(ToonSoldierClipFolder + "infantry_combat_shoot.FBX"));
            EditorUtility.SetDirty(recipe);
            AssetDatabase.SaveAssets();
            Debug.Log($"{LogPrefix} Soldier clips written to {RecipePath}.", recipe);
        }

        [MenuItem("Tools/Zombie War/Animation/2. Assign Zombie Animation Pack")]
        private static void AssignZombiePack()
        {
            AnimatorBuildRecipeSO recipe = LoadOrCreateRecipe();
            // One-shot actions must not loop; the pack imports everything as looping.
            SetLooping(ZombiePackClipFolder + "Zombie_Attack01.FBX", false);
            SetLooping(ZombiePackClipFolder + "Zombie_HitReact_Head.fbx", false);
            SetLooping(ZombiePackClipFolder + "Zombie_Idle_Death.fbx", false);

            recipe.AssignZombieClips(
                LoadClip(ZombiePackClipFolder + "Zombie_Idle_01.FBX"),
                LoadClip(ZombiePackClipFolder + "Zombie_Walk_01_Forward_InPlace.fbx"),
                LoadClip(ZombiePackClipFolder + "Zombie_Walk_Fast01_Forward_InPlace.fbx"),
                LoadClip(ZombiePackClipFolder + "Zombie_Run_01_Forward_InPlace.fbx"),
                LoadClip(ZombiePackClipFolder + "Zombie_Attack01.FBX"),
                LoadClip(ZombiePackClipFolder + "Zombie_HitReact_Head.fbx"),
                LoadClip(ZombiePackClipFolder + "Zombie_HitReact_Head.fbx"),
                LoadClip(ZombiePackClipFolder + "Zombie_Idle_Death.fbx"));
            EditorUtility.SetDirty(recipe);
            AssetDatabase.SaveAssets();
            Debug.Log($"{LogPrefix} Zombie clips written to {RecipePath}.", recipe);
        }

        [MenuItem("Tools/Zombie War/Animation/3. Build Animators")]
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

        private static AnimatorBuildRecipeSO LoadOrCreateRecipe()
        {
            var recipe = AssetDatabase.LoadAssetAtPath<AnimatorBuildRecipeSO>(RecipePath);
            if (recipe != null)
            {
                return recipe;
            }

            recipe = ScriptableObject.CreateInstance<AnimatorBuildRecipeSO>();
            AssetDatabase.CreateAsset(recipe, RecipePath);
            return recipe;
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
            BuildHitReactionLayer(controller, recipe.SoldierHit, upperBodyMask, 1f);
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
            AnimatorState combatIdle = AddState(machine, "CombatIdle", recipe.SoldierCombatIdle, 1f);
            AnimatorState shoot = AddState(machine, "Shoot", recipe.SoldierShoot, 1f);
            AnimatorState reload = AddState(machine, "Reload", recipe.SoldierReload, 1f);
            machine.defaultState = combatIdle;

            AddTriggerTransition(combatIdle, shoot, "Shoot", InstantTransition);
            AddTriggerTransition(shoot, shoot, "Shoot", InstantTransition);
            AddExitTimeTransition(shoot, combatIdle, ShortTransition);
            AddBoolTransition(combatIdle, reload, "Reload", true, ShortTransition);
            AddBoolTransition(shoot, reload, "Reload", true, ShortTransition);
            AddBoolTransition(reload, combatIdle, "Reload", false, ShortTransition);
        }

        private static void BuildHitReactionLayer(AnimatorController controller, AnimationClip hitClip, AvatarMask mask, float playbackSpeed)
        {
            // Override rather than additive: the packs ship full-pose flinches, not additive deltas.
            AnimatorStateMachine machine = AddLayer(controller, "Hit Reaction", mask, AnimatorLayerBlendingMode.Override);
            AnimatorState empty = AddState(machine, "Empty", null, 1f);
            AnimatorState hit = AddState(machine, "Hit", hitClip, playbackSpeed);
            machine.defaultState = empty;
            AddTriggerTransition(empty, hit, "Hit", InstantTransition);
            AddExitTimeTransition(hit, empty, ShortTransition);
        }

        private static void BuildFullBodyLayer(AnimatorController controller, AnimationClip deathClip)
        {
            AnimatorStateMachine machine = AddLayer(controller, "Full Body", null, AnimatorLayerBlendingMode.Override);
            AnimatorState empty = AddState(machine, "Empty", null, 1f);
            AnimatorState death = AddState(machine, "Death", deathClip, 1f);
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
            tree.AddChild(recipe.ZombieWalk, recipe.ZombieWalkSpeed);
            tree.AddChild(recipe.ZombieWalkFast, recipe.ZombieWalkFastSpeed);
            tree.AddChild(recipe.ZombieRun, recipe.ZombieRunSpeed);

            AnimatorState attack = AddState(machine, "Attack", recipe.ZombieAttack, recipe.ZombieAttackPlaybackSpeed);
            AnimatorState knockback = AddState(machine, "Knockback", recipe.ZombieKnockback, recipe.ZombieHitPlaybackSpeed);
            AnimatorState death = AddState(machine, "Death", recipe.ZombieDeath, 1f);
            machine.defaultState = locomotion;

            AddTriggerTransition(locomotion, attack, "Attack", ShortTransition);
            AddTriggerTransition(attack, attack, "Attack", ShortTransition);
            AddExitTimeTransition(attack, locomotion, ShortTransition);
            AddBoolTransition(locomotion, knockback, "Knockback", true, ShortTransition);
            AddBoolTransition(attack, knockback, "Knockback", true, ShortTransition);
            AddBoolTransition(knockback, locomotion, "Knockback", false, ShortTransition);

            AnimatorStateTransition toDeath = machine.AddAnyStateTransition(death);
            toDeath.canTransitionToSelf = false;
            toDeath.hasExitTime = false;
            toDeath.duration = ShortTransition;
            toDeath.AddCondition(AnimatorConditionMode.If, 0f, "Death");

            BuildHitReactionLayer(controller, recipe.ZombieHit, upperBodyMask, recipe.ZombieHitPlaybackSpeed);
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

        private static AnimatorState AddState(AnimatorStateMachine machine, string name, AnimationClip clip, float playbackSpeed)
        {
            AnimatorState state = machine.AddState(name);
            state.motion = clip;
            state.speed = playbackSpeed;
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

        private static void SetLooping(string fbxPath, bool looping)
        {
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"{LogPrefix} No model importer at {fbxPath}.");
                return;
            }

            ModelImporterClipAnimation[] clips = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;
            for (int i = 0; i < clips.Length; i++)
            {
                clips[i].loopTime = looping;
                clips[i].loopPose = false;
            }

            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        // Every source FBX in the packs carries exactly one clip, so the first representation is the clip.
        private static AnimationClip LoadClip(string fbxPath)
        {
            Object[] representations = AssetDatabase.LoadAllAssetRepresentationsAtPath(fbxPath);
            for (int i = 0; i < representations.Length; i++)
            {
                if (representations[i] is AnimationClip clip)
                {
                    return clip;
                }
            }

            Debug.LogError($"{LogPrefix} No animation clip found in {fbxPath}.");
            return null;
        }
    }
}
