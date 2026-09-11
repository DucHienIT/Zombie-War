using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ZombieWar.EditorTools
{
    // One menu item = one correct APK. Everything ApplyOwnedSettings touches is rewritten on every
    // build, so an Inspector edit or a stale ProjectSettings diff can never reach a shipped package.
    public static class AndroidBuilder
    {
        private const string LogPrefix = "[Build]";

        private const string OutputRoot = "Builds/Android";
        private const string ReleaseDirectory = OutputRoot + "/Release";
        private const string DevelopmentDirectory = OutputRoot + "/Development";
        private const string ApkFilePrefix = "ZombieWar";

        private const string LoadingScenePath = "Assets/_ZombieWar/Scenes/Loading.unity";
        private const string GameplayScenePath = "Assets/_ZombieWar/Scenes/Gameplay.unity";

        private const string ProductName = "Zombie War";
        private const string CompanyName = "DucHien";
        private const string ApplicationIdentifier = "com.duchien.zombiewar";
        private const AndroidSdkVersions MinSdkVersion = AndroidSdkVersions.AndroidApiLevel25;

        private const string KeystorePathVariable = "ZW_ANDROID_KEYSTORE";
        private const string KeystorePassVariable = "ZW_ANDROID_KEYSTORE_PASS";
        private const string KeyaliasNameVariable = "ZW_ANDROID_KEYALIAS";
        private const string KeyaliasPassVariable = "ZW_ANDROID_KEYALIAS_PASS";

        private const string OutputArgument = "-zwOutput";
        private const int LargestAssetsLogged = 15;
        private const float BytesPerMegabyte = 1024f * 1024f;

        [MenuItem("Tools/Zombie War/Build/Android APK (Release)", false, 0)]
        public static void BuildRelease()
        {
            Build(ReleaseDirectory, false, false);
        }

        [MenuItem("Tools/Zombie War/Build/Android APK (Development)", false, 1)]
        public static void BuildDevelopment()
        {
            Build(DevelopmentDirectory, true, false);
        }

        [MenuItem("Tools/Zombie War/Build/Android APK And Run (Development)", false, 2)]
        public static void BuildAndRunDevelopment()
        {
            Build(DevelopmentDirectory, true, true);
        }

        [MenuItem("Tools/Zombie War/Build/Apply Android Player Settings", false, 20)]
        public static void ApplyPlayerSettings()
        {
            ApplyOwnedSettings(false);
            Debug.Log($"{LogPrefix} Applied the Android player settings owned by AndroidBuilder.");
        }

        [MenuItem("Tools/Zombie War/Build/Open Build Folder", false, 21)]
        public static void OpenBuildFolder()
        {
            EditorUtility.RevealInFinder(Path.GetFullPath(OutputRoot) + Path.DirectorySeparatorChar);
        }

        [MenuItem("Tools/Zombie War/Build/Open Build Folder", true)]
        private static bool CanOpenBuildFolder()
        {
            return Directory.Exists(OutputRoot);
        }

        // Entry points for: Unity -batchmode -quit -buildTarget Android -executeMethod <this> [-zwOutput <dir>]
        public static void BuildReleaseFromCommandLine()
        {
            RunCommandLineBuild(ReleaseDirectory, false);
        }

        public static void BuildDevelopmentFromCommandLine()
        {
            RunCommandLineBuild(DevelopmentDirectory, true);
        }

        private static void RunCommandLineBuild(string defaultDirectory, bool development)
        {
            string directory = ReadArgument(OutputArgument) ?? defaultDirectory;
            EditorApplication.Exit(Build(directory, development, false) ? 0 : 1);
        }

        private static bool Build(string directory, bool development, bool autoRun)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Fail("Stop Play mode before building.");
                return false;
            }

            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Fail($"Android Build Support is not installed for Unity {Application.unityVersion}. Add the module in Unity Hub, including SDK, NDK and OpenJDK.");
                return false;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Fail("Build cancelled: the open scenes still have unsaved changes.");
                return false;
            }

            string[] scenes = ResolveScenes();
            if (scenes == null)
            {
                return false;
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android
                && !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Fail("Could not switch the active build target to Android.");
                return false;
            }

            ApplyOwnedSettings(development);
            BumpVersion();

            SigningState previousSigning;
            if (!TryApplySigning(development, out previousSigning))
            {
                return false;
            }

            Directory.CreateDirectory(directory);

            BuildOptions options = BuildOptions.None;
            if (development)
            {
                options |= BuildOptions.Development | BuildOptions.ConnectWithProfiler;
            }

            if (autoRun)
            {
                options |= BuildOptions.AutoRunPlayer;
            }

            BuildReport report;
            try
            {
                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = Path.Combine(directory, ApkFileName(development)),
                    target = BuildTarget.Android,
                    targetGroup = BuildTargetGroup.Android,
                    options = options
                });
            }
            finally
            {
                // Keystore credentials are machine-local secrets, and keystoreName is serialized into
                // ProjectSettings.asset - leaving it applied hands the next commit a diff nobody wants.
                RestoreSigning(previousSigning);
            }

            LogReport(report);

            bool succeeded = report.summary.result == BuildResult.Succeeded;
            if (succeeded && !Application.isBatchMode)
            {
                EditorUtility.RevealInFinder(report.summary.outputPath);
            }

            return succeeded;
        }

        private static string[] ResolveScenes()
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Debug.LogWarning($"{LogPrefix} Build Settings lists no enabled scene; falling back to Loading + Gameplay.");
                return new[] { LoadingScenePath, GameplayScenePath };
            }

            // LoadingSceneController is what loads Gameplay, so a build whose first scene is anything
            // else boots straight into an empty game with no menu and no progress bar.
            if (scenes[0] != LoadingScenePath || Array.IndexOf(scenes, GameplayScenePath) < 0)
            {
                Fail($"Build Settings must start with {LoadingScenePath} and include {GameplayScenePath}.");
                return null;
            }

            return scenes;
        }

        // Every build ships under a fresh version: the patch component of bundleVersion and
        // bundleVersionCode both advance by one, so two APKs can never share a name or a version code.
        // Runs only after every pre-build check has passed, so a cancelled build does not burn a number.
        private static void BumpVersion()
        {
            string previousVersion = PlayerSettings.bundleVersion;
            int previousCode = PlayerSettings.Android.bundleVersionCode;

            PlayerSettings.bundleVersion = IncrementPatch(previousVersion);
            PlayerSettings.Android.bundleVersionCode = previousCode + 1;
            AssetDatabase.SaveAssets();

            Debug.Log($"{LogPrefix} Version {previousVersion} (vc{previousCode}) -> {PlayerSettings.bundleVersion} (vc{PlayerSettings.Android.bundleVersionCode}).");
        }

        private static string IncrementPatch(string version)
        {
            string[] parts = string.IsNullOrEmpty(version) ? new string[0] : version.Split('.');
            if (parts.Length == 0)
            {
                return "1.0.1";
            }

            int patch;
            if (!int.TryParse(parts[parts.Length - 1], out patch))
            {
                Debug.LogWarning($"{LogPrefix} bundleVersion '{version}' has no numeric patch component; appending '.1'.");
                return version + ".1";
            }

            parts[parts.Length - 1] = (patch + 1).ToString();
            return string.Join(".", parts);
        }

        private static void ApplyOwnedSettings(bool development)
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ApplicationIdentifier);

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Android, Il2CppCompilerConfiguration.Release);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Low);
            PlayerSettings.stripEngineCode = true;

            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = MinSdkVersion;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            // SafeAreaFitter insets the UI itself, so the game may draw behind notches and cutouts.
            PlayerSettings.Android.renderOutsideSafeArea = true;

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // Textures whose Android override says "Automatic" follow this subtarget; leaving it at
            // Generic is what silently ships ETC1 instead of the ASTC that TextureImportRules assumes.
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;

            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.development = development;
            EditorUserBuildSettings.androidBuildType = development ? AndroidBuildType.Development : AndroidBuildType.Release;
            EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Disabled;
        }

        private static bool TryApplySigning(bool development, out SigningState previous)
        {
            previous = default(SigningState);

            string keystorePath = Environment.GetEnvironmentVariable(KeystorePathVariable);
            if (string.IsNullOrEmpty(keystorePath))
            {
                // No environment keystore: leave whatever signing the project already has, so a keystore
                // configured by hand in the Inspector still gets used.
                if (!development && !PlayerSettings.Android.useCustomKeystore)
                {
                    Debug.LogWarning($"{LogPrefix} {KeystorePathVariable} is not set, so the release APK is signed with the Unity debug keystore. That installs and runs fine for QA and side-loading, but the Play Store rejects it.");
                }

                return true;
            }

            if (!File.Exists(keystorePath))
            {
                Fail($"{KeystorePathVariable} points at {keystorePath}, which does not exist.");
                return false;
            }

            previous = new SigningState
            {
                Applied = true,
                UseCustomKeystore = PlayerSettings.Android.useCustomKeystore,
                KeystoreName = PlayerSettings.Android.keystoreName,
                KeyaliasName = PlayerSettings.Android.keyaliasName
            };

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystorePath;
            PlayerSettings.Android.keystorePass = Environment.GetEnvironmentVariable(KeystorePassVariable);
            PlayerSettings.Android.keyaliasName = Environment.GetEnvironmentVariable(KeyaliasNameVariable);
            PlayerSettings.Android.keyaliasPass = Environment.GetEnvironmentVariable(KeyaliasPassVariable);
            return true;
        }

        private static void RestoreSigning(SigningState previous)
        {
            if (!previous.Applied)
            {
                return;
            }

            PlayerSettings.Android.keystorePass = string.Empty;
            PlayerSettings.Android.keyaliasPass = string.Empty;
            PlayerSettings.Android.keyaliasName = previous.KeyaliasName;
            PlayerSettings.Android.keystoreName = previous.KeystoreName;
            PlayerSettings.Android.useCustomKeystore = previous.UseCustomKeystore;
        }

        private static string ApkFileName(bool development)
        {
            string suffix = development ? "-dev" : string.Empty;
            return $"{ApkFilePrefix}-{PlayerSettings.bundleVersion}-vc{PlayerSettings.Android.bundleVersionCode}{suffix}.apk";
        }

        private static void LogReport(BuildReport report)
        {
            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"{LogPrefix} Android build {summary.result} after {summary.totalTime:hh\\:mm\\:ss} with {summary.totalErrors} error(s).");
                return;
            }

            float megabytes = summary.totalSize / BytesPerMegabyte;
            Debug.Log($"{LogPrefix} Android build succeeded in {summary.totalTime:hh\\:mm\\:ss} - {megabytes:F1} MB -> {summary.outputPath}");
            LogLargestAssets(report);
        }

        // Total APK size never says what to cut; the per-asset table does.
        private static void LogLargestAssets(BuildReport report)
        {
            List<PackedAssetInfo> contents = new List<PackedAssetInfo>(256);
            foreach (PackedAssets packedAsset in report.packedAssets)
            {
                contents.AddRange(packedAsset.contents);
            }

            if (contents.Count == 0)
            {
                return;
            }

            contents.Sort((left, right) => right.packedSize.CompareTo(left.packedSize));

            StringBuilder builder = new StringBuilder();
            builder.Append(LogPrefix).Append(" Largest packed assets:");
            int count = Mathf.Min(LargestAssetsLogged, contents.Count);
            for (int i = 0; i < count; i++)
            {
                PackedAssetInfo info = contents[i];
                builder.AppendLine();
                builder.Append((info.packedSize / BytesPerMegabyte).ToString("F2")).Append(" MB  ").Append(info.sourceAssetPath);
            }

            Debug.Log(builder.ToString());
        }

        private static string ReadArgument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        private static void Fail(string message)
        {
            Debug.LogError($"{LogPrefix} {message}");
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Zombie War - Android build", message, "OK");
            }
        }

        private struct SigningState
        {
            public bool Applied;
            public bool UseCustomKeystore;
            public string KeystoreName;
            public string KeyaliasName;
        }
    }
}
