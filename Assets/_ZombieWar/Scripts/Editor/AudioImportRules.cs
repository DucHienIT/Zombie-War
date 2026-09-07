using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ZombieWar.EditorTools
{
    // Every clip in the game is a short positional one-shot (gunfire, zombie voice, UI tap), so they
    // ship as mono Vorbis and are decoded once at load - rifle fire at five shots a second cannot
    // afford a decode per play, and mono is what a 3D AudioSource collapses them to anyway.
    public sealed class AudioImportRules : AssetPostprocessor
    {
        private const string FirstPartyAudioRoot = "Assets/_ZombieWar/Audio/";
        private const float VorbisQuality = 0.7f;

        [MenuItem("Tools/Zombie War/Assets/Reapply Audio Import Rules")]
        private static void ReapplyAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/_ZombieWar" });
            List<string> changed = new List<string>(guids.Length);
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
                    if (importer == null || !Apply(importer, path))
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

            Debug.Log($"[Assets] Audio import rules reapplied to {changed.Count} of {guids.Length} clips.");
        }

        private void OnPreprocessAudio()
        {
            Apply((AudioImporter)assetImporter, assetPath);
        }

        private static bool Apply(AudioImporter importer, string assetPath)
        {
            if (!assetPath.StartsWith(FirstPartyAudioRoot))
            {
                return false;
            }

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            bool alreadyCorrect = importer.forceToMono
                && !importer.loadInBackground
                && settings.loadType == AudioClipLoadType.DecompressOnLoad
                && settings.compressionFormat == AudioCompressionFormat.Vorbis
                && settings.preloadAudioData
                && Mathf.Approximately(settings.quality, VorbisQuality);
            if (alreadyCorrect)
            {
                return false;
            }

            importer.forceToMono = true;
            importer.loadInBackground = false;

            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = VorbisQuality;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            return true;
        }
    }
}
