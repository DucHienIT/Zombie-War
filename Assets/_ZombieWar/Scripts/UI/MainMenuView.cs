using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieWar.Core;
using ZombieWar.Data;

namespace ZombieWar.UI
{
    public sealed class MainMenuView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";
        private const int TargetFrameRate = 60;

        [Serializable]
        private struct LevelEntry
        {
            public LevelDefinitionSO Level;
            public Button PlayButton;
            public GameObject LockedBadge;
            public TMP_Text BestScoreText;
        }

        [SerializeField] private LevelLoader _levelLoader;
        [SerializeField] private LevelEntry[] _entries;

        private readonly SaveService _save = new SaveService();

        private void Awake()
        {
            Application.targetFrameRate = TargetFrameRate;
            Time.timeScale = 1f;
            if (_levelLoader == null || _entries == null || _entries.Length == 0)
            {
                Debug.LogError($"{LogPrefix} MainMenuView has an unassigned reference.", this);
                return;
            }

            for (int i = 0; i < _entries.Length; i++)
            {
                BindEntry(_entries[i]);
            }
        }

        private void BindEntry(LevelEntry entry)
        {
            bool unlocked = _save.IsLevelUnlocked(entry.Level.LevelIndex);
            entry.PlayButton.interactable = unlocked;
            entry.LockedBadge.SetActive(!unlocked);
            entry.BestScoreText.SetText("{0}", _save.GetBestScore(entry.Level.LevelIndex));

            LevelDefinitionSO level = entry.Level;
            entry.PlayButton.onClick.AddListener(() => _levelLoader.LoadLevel(level));
        }
    }
}
