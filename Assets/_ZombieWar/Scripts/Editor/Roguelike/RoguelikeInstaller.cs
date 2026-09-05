using UnityEditor;
using UnityEngine;
using ZombieWar.Core;
using ZombieWar.Data;
using ZombieWar.EditorTools.UI;
using ZombieWar.Enemies;
using ZombieWar.Player;
using ZombieWar.Roguelike;
using ZombieWar.Weapons;

namespace ZombieWar.EditorTools.Roguelike
{
    // Puts the two roguelike components into GameplayRoot.prefab and wires what they need.
    // Scaffolding only: once the prefab holds them, the prefab is the source of truth.
    public static class RoguelikeInstaller
    {
        private const string LogPrefix = "[Rogue Build]";
        private const string GameplayPrefabPath = "Assets/_ZombieWar/Prefabs/GameplayRoot.prefab";
        private const string SettingsPath = "Assets/_ZombieWar/Data/Roguelike/RoguelikeSettings.asset";

        [MenuItem("Tools/Zombie War/Roguelike/2. Install Into Gameplay Root", false, 201)]
        public static void Install()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Install Roguelike Into Gameplay Root",
                "This adds PlayerStatSheet to Player and RoguelikeDirector to Systems/Managers in GameplayRoot.prefab, then wires the director.",
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
            var settings = AssetDatabase.LoadAssetAtPath<RoguelikeSettingsSO>(SettingsPath);
            if (settings == null)
            {
                Debug.LogError($"{LogPrefix} {SettingsPath} is missing - run step 1 first.");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(GameplayPrefabPath);
            try
            {
                Transform managers = root.transform.Find("Systems/Managers");
                Transform player = root.transform.Find("Player");
                if (managers == null || player == null)
                {
                    Debug.LogError($"{LogPrefix} GameplayRoot is missing Systems/Managers or Player.");
                    return;
                }

                var stats = player.GetComponent<PlayerStatSheet>();
                if (stats == null)
                {
                    stats = player.gameObject.AddComponent<PlayerStatSheet>();
                }

                var director = managers.GetComponent<RoguelikeDirector>();
                if (director == null)
                {
                    director = managers.gameObject.AddComponent<RoguelikeDirector>();
                }

                var playerHealth = player.GetComponent<PlayerHealth>();
                UiBuildUtility.Bind(director,
                    "_settings", settings,
                    "_flow", managers.GetComponent<GameFlowController>(),
                    "_zombies", managers.GetComponent<ZombieManager>(),
                    "_stats", stats,
                    "_playerHealth", playerHealth);

                // Every system that a passive skill can move reads the same sheet.
                UiBuildUtility.Bind(playerHealth, "_stats", stats);
                UiBuildUtility.Bind(player.GetComponent<PlayerMotor>(), "_stats", stats);
                UiBuildUtility.Bind(player.GetComponent<WeaponController>(), "_stats", stats);
                UiBuildUtility.Bind(player.GetComponent<BombThrower>(), "_stats", stats);
                UiBuildUtility.Bind(managers.GetComponent<GameplayUiBinder>(), "_rogue", director);

                PrefabUtility.SaveAsPrefabAsset(root, GameplayPrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"{LogPrefix} installed the roguelike director into {GameplayPrefabPath}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
