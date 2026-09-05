using UnityEngine;

namespace ZombieWar.Core
{
    public sealed class SaveService
    {
        private const string UnlockedLevelKey = "zw_unlocked_level";
        private const string BestScoreKeyPrefix = "zw_best_score_";
        private const string MusicVolumeKey = "zw_music_volume";
        private const string SfxVolumeKey = "zw_sfx_volume";
        private const string HapticsKey = "zw_haptics";
        private const string CameraShakeKey = "zw_camera_shake";
        private const string PlayerLevelKey = "zw_player_level";
        private const string PlayerXpKey = "zw_player_xp";
        private const string CoinsKey = "zw_coins";
        private const string GunLevelKeyPrefix = "zw_gun_level_";
        private const string GunUnlockedKeyPrefix = "zw_gun_unlocked_";
        private const int FirstLevelIndex = 1;
        private const int FirstPlayerLevel = 1;

        public int PlayerLevel => PlayerPrefs.GetInt(PlayerLevelKey, FirstPlayerLevel);
        public int PlayerXp => PlayerPrefs.GetInt(PlayerXpKey, 0);
        public int GetCoins(int startingCoins) => PlayerPrefs.GetInt(CoinsKey, startingCoins);
        public int GetGunLevel(string gunId) => PlayerPrefs.GetInt(GunLevelKeyPrefix + gunId, 0);
        public bool IsGunUnlocked(string gunId, bool unlockedByDefault) => PlayerPrefs.GetInt(GunUnlockedKeyPrefix + gunId, unlockedByDefault ? 1 : 0) == 1;

        public int UnlockedLevel => PlayerPrefs.GetInt(UnlockedLevelKey, FirstLevelIndex);
        public float MusicVolume => PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        public float SfxVolume => PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        public bool HapticsEnabled => PlayerPrefs.GetInt(HapticsKey, 1) == 1;
        public bool CameraShakeEnabled => PlayerPrefs.GetInt(CameraShakeKey, 1) == 1;

        public bool IsLevelUnlocked(int levelIndex) => levelIndex <= UnlockedLevel;

        public int GetBestScore(int levelIndex) => PlayerPrefs.GetInt(BestScoreKeyPrefix + levelIndex, 0);

        public void UnlockLevel(int levelIndex)
        {
            if (levelIndex <= UnlockedLevel)
            {
                return;
            }

            PlayerPrefs.SetInt(UnlockedLevelKey, levelIndex);
            PlayerPrefs.Save();
        }

        public bool TrySubmitScore(int levelIndex, int score)
        {
            if (score <= GetBestScore(levelIndex))
            {
                return false;
            }

            PlayerPrefs.SetInt(BestScoreKeyPrefix + levelIndex, score);
            PlayerPrefs.Save();
            return true;
        }

        public void SetAudioSettings(float musicVolume, float sfxVolume, bool hapticsEnabled)
        {
            PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
            PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
            PlayerPrefs.SetInt(HapticsKey, hapticsEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetCameraShakeEnabled(bool enabled)
        {
            PlayerPrefs.SetInt(CameraShakeKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetProgress(int playerLevel, int playerXp, int coins)
        {
            PlayerPrefs.SetInt(PlayerLevelKey, playerLevel);
            PlayerPrefs.SetInt(PlayerXpKey, playerXp);
            PlayerPrefs.SetInt(CoinsKey, coins);
            PlayerPrefs.Save();
        }

        public void SetGunLevel(string gunId, int level)
        {
            PlayerPrefs.SetInt(GunLevelKeyPrefix + gunId, level);
            PlayerPrefs.Save();
        }

        public void SetGunUnlocked(string gunId, bool unlocked)
        {
            PlayerPrefs.SetInt(GunUnlockedKeyPrefix + gunId, unlocked ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
