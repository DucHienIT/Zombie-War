using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ZombieWar.EditorTools
{
    // Android texture policy for the shipped APK. Max size comes from what the top-down camera can
    // actually show - the soldier never covers more than a few hundred pixels - not from the
    // resolution the source pack happened to ship at.
    public sealed class TextureImportRules : AssetPostprocessor
    {
        private const string AndroidPlatform = "Android";
        private const string FirstPartyRoot = "Assets/_ZombieWar/";
        private const string UiRoot = "Assets/_ZombieWar/Art/UI/";

        private const int CharacterMaxSize = 1024;
        private const int WeaponMaxSize = 512;
        private const int EnvironmentMaxSize = 1024;
        private const int VfxMaxSize = 256;
        private const int DefaultMaxSize = 512;

        [MenuItem("Tools/Zombie War/Assets/Reapply Texture Import Rules (Android)")]
        private static void ReapplyAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/_ZombieWar" });
            List<string> changed = new List<string>(guids.Length);
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null || !Apply(importer, path, true))
                    {
                        continue;
                    }

                    importer.SaveAndReimport();
                    changed.Add(path);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[Assets] Android texture rules reapplied to {changed.Count} of {guids.Length} textures.");
        }

        private void OnPreprocessTexture()
        {
            Apply((TextureImporter)assetImporter, assetPath, false);
        }

        // force rewrites a hand-tuned max size back to the table; the importer path leaves it alone so
        // one texture can be pinned larger without the next reimport undoing it.
        private static bool Apply(TextureImporter importer, string assetPath, bool force)
        {
            if (!ShouldEnforce(assetPath))
            {
                return false;
            }

            TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(AndroidPlatform);
            TextureImporterFormat format = FormatFor(assetPath, importer);

            // UI sprites are the source the sprite atlases pack from, so capping them here would cap
            // what lands in the atlas; their size is owned by the atlas asset instead.
            int maxSize = settings.maxTextureSize;
            if (!assetPath.StartsWith(UiRoot) && (force || !settings.overridden))
            {
                maxSize = MaxSizeFor(assetPath);
            }

            if (settings.overridden
                && settings.format == format
                && settings.maxTextureSize == maxSize
                && settings.textureCompression == TextureImporterCompression.Compressed)
            {
                return false;
            }

            settings.name = AndroidPlatform;
            settings.overridden = true;
            settings.format = format;
            settings.maxTextureSize = maxSize;
            settings.textureCompression = TextureImporterCompression.Compressed;
            importer.SetPlatformTextureSettings(settings);
            return true;
        }

        private static bool ShouldEnforce(string assetPath)
        {
            return assetPath.StartsWith(FirstPartyRoot) && !assetPath.Contains("/Fonts/");
        }

        private static TextureImporterFormat FormatFor(string assetPath, TextureImporter importer)
        {
            // 6x6 blocks band visibly on normal maps and on UI edges, so both get the finer block.
            if (importer.textureType == TextureImporterType.NormalMap || assetPath.StartsWith(UiRoot))
            {
                return TextureImporterFormat.ASTC_4x4;
            }

            return TextureImporterFormat.ASTC_6x6;
        }

        private static int MaxSizeFor(string assetPath)
        {
            if (assetPath.Contains("/Art/Textures/Characters/"))
            {
                return CharacterMaxSize;
            }

            if (assetPath.Contains("/Art/Textures/Weapons/"))
            {
                return WeaponMaxSize;
            }

            if (assetPath.Contains("/Art/Textures/Environment/"))
            {
                return EnvironmentMaxSize;
            }

            if (assetPath.Contains("/Art/Textures/VFX/"))
            {
                return VfxMaxSize;
            }

            return DefaultMaxSize;
        }
    }
}
