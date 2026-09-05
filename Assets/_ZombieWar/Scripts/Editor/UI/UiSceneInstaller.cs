using System.Collections.Generic;
using Cinemachine;
using UnityEditor;
using UnityEngine;
using ZombieWar.Core;
using ZombieWar.Data;
using ZombieWar.Level;
using ZombieWar.Player;
using ZombieWar.Roguelike;
using ZombieWar.UI;
using ZombieWar.Weapons;

namespace ZombieWar.EditorTools.UI
{
    // Drops the shared UIRoot.prefab into GameplayRoot and wires the two binders that own
    // the gameplay side of every reference. Nothing here writes into the UI prefab.
    public static class UiSceneInstaller
    {
        private const string LogPrefix = "[UI Build]";
        private const string GameplayPrefabPath = "Assets/_ZombieWar/Prefabs/GameplayRoot.prefab";
        private const string UiInstanceName = "UIRoot";
        private const string ProgressionRulesPath = "Assets/_ZombieWar/Data/Rules/ProgressionRules.asset";

        [MenuItem("Tools/Zombie War/UI/3. Install UI Into Gameplay Root", false, 102)]
        public static void Install()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Install UI Into Gameplay Root",
                "This replaces the UI branch of GameplayRoot.prefab with an instance of UIRoot.prefab and rewires GameplayUiBinder and MenuUiBinder.",
                "Install",
                "Cancel");
            if (!confirmed)
            {
                return;
            }

            InstallWithoutPrompt();
        }

        public static void InstallWithoutPrompt()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiRootPrefabBuilder.PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"{LogPrefix} {UiRootPrefabBuilder.PrefabPath} is missing - run step 2 first.");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(GameplayPrefabPath);
            try
            {
                DestroyChild(root.transform, "UI");
                DestroyChild(root.transform, UiInstanceName);

                // The UI plays its own clicks now, so the shared voice on AudioService is dead.
                Transform uiVoice = root.transform.Find("Systems/AudioService/UiVoice");
                if (uiVoice != null)
                {
                    Object.DestroyImmediate(uiVoice.gameObject);
                }

                var uiInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
                uiInstance.name = UiInstanceName;
                var ui = uiInstance.GetComponent<UIManager>();

                Transform managers = root.transform.Find("Systems/Managers");
                Transform player = root.transform.Find("Player");
                Transform cameraRig = root.transform.Find("CameraRig");
                if (managers == null || player == null || cameraRig == null || ui == null)
                {
                    Debug.LogError($"{LogPrefix} GameplayRoot is missing Systems/Managers, Player, CameraRig or the UIManager.");
                    return;
                }

                var cameraShake = cameraRig.GetComponentInChildren<CameraShakeController>(true);
                var virtualCamera = cameraRig.GetComponentInChildren<CinemachineVirtualCamera>(true);
                if (cameraShake == null || virtualCamera == null)
                {
                    Debug.LogError($"{LogPrefix} CameraRig has no CameraShakeController or virtual camera.");
                    return;
                }

                var flow = managers.GetComponent<GameFlowController>();
                ProfileService profile = BindProfileService(managers);
                BindGameplayBinder(managers, ui, flow, player, cameraShake, managers.GetComponent<RoguelikeDirector>());
                BindMenuBinder(managers, ui, flow, profile, cameraShake);
                UiBuildUtility.Bind(managers.GetComponent<LevelMapLoader>(), "_virtualCamera", virtualCamera);
                UiBuildUtility.Bind(flow, "_profile", profile);
                UiBuildUtility.Bind(player.GetComponent<WeaponController>(), "_profile", profile);

                // The world only exists during a run; the menu overlay sits in an empty scene.
                player.gameObject.SetActive(false);

                PrefabUtility.SaveAsPrefabAsset(root, GameplayPrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"{LogPrefix} installed the UI root into {GameplayPrefabPath}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // The director is null until the roguelike step has run; installing in either order
        // ends up with the same wiring because that step binds this field as well.
        private static void BindGameplayBinder(Transform managers, UIManager ui, GameFlowController flow, Transform player,
            CameraShakeController cameraShake, RoguelikeDirector rogue)
        {
            var binder = managers.GetComponent<GameplayUiBinder>();
            if (binder == null)
            {
                binder = managers.gameObject.AddComponent<GameplayUiBinder>();
            }

            UiBuildUtility.Bind(binder,
                "_ui", ui,
                "_flow", flow,
                "_playerHealth", player.GetComponent<PlayerHealth>(),
                "_weapons", player.GetComponent<WeaponController>(),
                "_bombs", player.GetComponent<BombThrower>(),
                "_cameraShake", cameraShake,
                "_rogue", rogue);
        }

        private static void BindMenuBinder(Transform managers, UIManager ui, GameFlowController flow, ProfileService profile,
            CameraShakeController cameraShake)
        {
            var binder = managers.GetComponent<MenuUiBinder>();
            if (binder == null)
            {
                binder = managers.gameObject.AddComponent<MenuUiBinder>();
            }

            UiBuildUtility.Bind(binder,
                "_ui", ui,
                "_flow", flow,
                "_profile", profile,
                "_cameraShake", cameraShake,
                "_levels", LoadLevels());
        }

        // The profile is gameplay-side state the menu reads; it lives with the other managers.
        private static ProfileService BindProfileService(Transform managers)
        {
            var profile = managers.GetComponent<ProfileService>();
            if (profile == null)
            {
                profile = managers.gameObject.AddComponent<ProfileService>();
            }

            var rules = AssetDatabase.LoadAssetAtPath<ProgressionRulesSO>(ProgressionRulesPath);
            if (rules == null)
            {
                rules = ScriptableObject.CreateInstance<ProgressionRulesSO>();
                AssetDatabase.CreateAsset(rules, ProgressionRulesPath);
                Debug.Log($"{LogPrefix} created {ProgressionRulesPath} with default values.");
            }

            UiBuildUtility.Bind(profile, "_rules", rules, "_guns", LoadGuns());
            return profile;
        }

        // Display order of the weapon page: by asset name, which puts the rifle before the shotgun.
        private static List<Object> LoadGuns()
        {
            string[] guids = AssetDatabase.FindAssets("t:GunDefinitionSO", new[] { "Assets/_ZombieWar/Data/Weapons" });
            var paths = new List<string>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guids[i]));
            }

            paths.Sort(string.CompareOrdinal);
            var result = new List<Object>(paths.Count);
            for (int i = 0; i < paths.Count; i++)
            {
                var gun = AssetDatabase.LoadAssetAtPath<GunDefinitionSO>(paths[i]);
                if (gun != null)
                {
                    result.Add(gun);
                }
            }

            return result;
        }

        private static void DestroyChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        // Sorted by level index so slot order in the prefab matches the cards on screen.
        private static List<Object> LoadLevels()
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelDefinitionSO", new[] { "Assets/_ZombieWar/Data/Levels" });
            var levels = new List<LevelDefinitionSO>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                var level = AssetDatabase.LoadAssetAtPath<LevelDefinitionSO>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (level != null)
                {
                    levels.Add(level);
                }
            }

            levels.Sort((a, b) => a.LevelIndex.CompareTo(b.LevelIndex));
            var result = new List<Object>(levels.Count);
            for (int i = 0; i < levels.Count; i++)
            {
                result.Add(levels[i]);
            }

            return result;
        }
    }
}
