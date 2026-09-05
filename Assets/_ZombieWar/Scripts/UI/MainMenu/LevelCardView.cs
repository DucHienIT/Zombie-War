using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieWar.UI
{
    public sealed class LevelCardView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private Button _playButton;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _indexText;
        [SerializeField] private TMP_Text _bestScoreText;
        [SerializeField] private GameObject _lockedBadge;
        [SerializeField] private GameObject _bestScoreGroup;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Locked")]
        [SerializeField] private float _lockedAlpha = 0.55f;

        private Action _onPlayRequested;

        private void Awake()
        {
            bool missing = _playButton == null || _nameText == null || _indexText == null || _bestScoreText == null
                           || _lockedBadge == null || _bestScoreGroup == null || _canvasGroup == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} LevelCardView has an unassigned reference.", this);
                return;
            }

            _playButton.onClick.AddListener(HandlePlayClicked);
        }

        public void Bind(in LevelCardData data, Action onPlayRequested)
        {
            _onPlayRequested = onPlayRequested;
            _nameText.text = data.DisplayName;
            _indexText.SetText("{0}", data.Index);
            _bestScoreText.SetText("{0}", data.BestScore);
            _playButton.interactable = data.Unlocked;
            _lockedBadge.SetActive(!data.Unlocked);
            _bestScoreGroup.SetActive(data.Unlocked && data.BestScore > 0);
            _canvasGroup.alpha = data.Unlocked ? 1f : _lockedAlpha;
        }

        private void HandlePlayClicked() => _onPlayRequested?.Invoke();
    }
}
