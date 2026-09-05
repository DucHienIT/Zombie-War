using DG.Tweening;
using TMPro;
using UnityEngine;

namespace ZombieWar.UI
{
    public sealed class ScoreView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private TMP_Text _killsText;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private RectTransform _killsPunchTarget;

        [Header("Kill Punch")]
        [SerializeField] private float _punchScale = 0.2f;
        [SerializeField] private float _punchDuration = 0.25f;

        private int _shownKills = -1;

        private void Awake()
        {
            if (_killsText == null || _scoreText == null || _killsPunchTarget == null)
            {
                Debug.LogError($"{LogPrefix} ScoreView has an unassigned reference.", this);
            }
        }

        public void SetScore(int kills, int score)
        {
            _scoreText.SetText("{0}", score);
            if (kills == _shownKills)
            {
                return;
            }

            bool isFirstBind = _shownKills < 0;
            _shownKills = kills;
            _killsText.SetText("{0}", kills);
            if (!isFirstBind)
            {
                _killsPunchTarget.DOPunchScale(Vector3.one * _punchScale, _punchDuration, 1, 0f).SetLink(gameObject);
            }
        }
    }
}
