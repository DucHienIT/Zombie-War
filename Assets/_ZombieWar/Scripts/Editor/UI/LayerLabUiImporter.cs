using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace ZombieWar.EditorTools.UI
{
    // Copies the handful of Layer Lab GUI Pro assets the game actually uses into
    // Assets/_ZombieWar so the first-party UI never reaches into a third-party pack.
    // AssetDatabase.CopyAsset carries the importer settings across, which is what keeps
    // the authored 9-slice borders on buttons, frames and bars.
    public static class LayerLabUiImporter
    {
        private const string LogPrefix = "[UI Import]";
        private const string PackRoot = "Assets/ThirdParty/Layer Lab/GUI Pro-CasualGame/ResourcesData/";

        private static readonly string[] SpriteSourceFolders =
        {
            "Sprite/Component/Button/",
            "Sprite/Component/Frame/",
            "Sprite/Component/Popup/",
            "Sprite/Component/Slider/",
            "Sprite/Component/Label/",
            "Sprite/Component/Icon_PictoIcons(x2)/128/",
            "Sprite/Component/Icon_ItemIcons(x2)/128/",
            // The switch toggle only ships as demo art.
            "Sprite/Demo/Demo_UI/",
        };

        [MenuItem("Tools/Zombie War/UI/1. Import Layer Lab UI Assets", false, 100)]
        public static void Import()
        {
            int copied = 0;
            int skipped = 0;
            int failed = 0;

            AssetDatabase.StartAssetEditing();
            try
            {
                CopyGroup(UiSkin.ButtonSprites, UiSkin.ButtonFolder, ref copied, ref skipped, ref failed);
                CopyGroup(UiSkin.FrameSprites, UiSkin.FrameFolder, ref copied, ref skipped, ref failed);
                CopyGroup(UiSkin.PopupSprites, UiSkin.PopupFolder, ref copied, ref skipped, ref failed);
                CopyGroup(UiSkin.SliderSprites, UiSkin.SliderFolder, ref copied, ref skipped, ref failed);
                CopyGroup(UiSkin.LabelSprites, UiSkin.LabelFolder, ref copied, ref skipped, ref failed);
                CopyGroup(UiSkin.IconSprites, UiSkin.IconFolder, ref copied, ref skipped, ref failed);
                CopyGroup(UiSkin.ToggleSprites, UiSkin.ToggleFolder, ref copied, ref skipped, ref failed);
                CopyFonts(ref copied, ref skipped, ref failed);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            VerifyFonts();
            Debug.Log($"{LogPrefix} copied {copied}, already present {skipped}, failed {failed}. Destination: {UiSkin.Root}");
        }

        private static void CopyGroup(string[] fileNames, string destinationFolder, ref int copied, ref int skipped, ref int failed)
        {
            EnsureFolder(destinationFolder);
            for (int i = 0; i < fileNames.Length; i++)
            {
                string source = ResolveSource(fileNames[i]);
                string destination = destinationFolder + fileNames[i] + ".png";
                if (source == null)
                {
                    Debug.LogError($"{LogPrefix} {fileNames[i]} was not found in the Layer Lab pack.");
                    failed++;
                    continue;
                }

                if (File.Exists(destination))
                {
                    skipped++;
                    continue;
                }

                if (AssetDatabase.CopyAsset(source, destination))
                {
                    copied++;
                    continue;
                }

                Debug.LogError($"{LogPrefix} failed to copy {source} to {destination}.");
                failed++;
            }
        }

        private static void CopyFonts(ref int copied, ref int skipped, ref int failed)
        {
            EnsureFolder(UiSkin.FontFolder);
            for (int i = 0; i < UiSkin.FontAssets.Length; i++)
            {
                string source = PackRoot + "Fonts/" + UiSkin.FontAssets[i] + ".asset";
                string destination = UiSkin.FontFolder + UiSkin.FontAssets[i] + ".asset";
                if (File.Exists(destination))
                {
                    skipped++;
                    continue;
                }

                if (!File.Exists(source))
                {
                    Debug.LogError($"{LogPrefix} font {source} was not found.");
                    failed++;
                    continue;
                }

                if (AssetDatabase.CopyAsset(source, destination))
                {
                    copied++;
                    continue;
                }

                Debug.LogError($"{LogPrefix} failed to copy font {source}.");
                failed++;
            }
        }

        // A TMP font asset carries its atlas and material as sub-assets; a copy that lost them
        // renders every label blank, and that failure is silent at runtime.
        private static void VerifyFonts()
        {
            for (int i = 0; i < UiSkin.FontAssets.Length; i++)
            {
                string path = UiSkin.FontFolder + UiSkin.FontAssets[i] + ".asset";
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (font == null)
                {
                    Debug.LogError($"{LogPrefix} {path} did not import as a TMP font asset.");
                    continue;
                }

                if (font.material == null || font.material.mainTexture == null)
                {
                    Debug.LogError($"{LogPrefix} {path} lost its atlas texture in the copy - labels would render blank.");
                }
            }
        }

        // The pack mixes ".png" and ".Png" spellings, and Unity keeps asset paths case sensitive.
        private static string ResolveSource(string fileName)
        {
            for (int i = 0; i < SpriteSourceFolders.Length; i++)
            {
                string folder = PackRoot + SpriteSourceFolders[i];
                string lower = folder + fileName + ".png";
                if (File.Exists(lower))
                {
                    return lower;
                }

                string upper = folder + fileName + ".Png";
                if (File.Exists(upper))
                {
                    return upper;
                }
            }

            return null;
        }

        private static void EnsureFolder(string folder)
        {
            string trimmed = folder.TrimEnd('/');
            if (AssetDatabase.IsValidFolder(trimmed))
            {
                return;
            }

            string parent = Path.GetDirectoryName(trimmed).Replace('\\', '/');
            EnsureFolder(parent + "/");
            AssetDatabase.CreateFolder(parent, Path.GetFileName(trimmed));
        }
    }
}
