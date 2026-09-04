using TMPro;
using UnityEngine;
using ZombieWar.Core;

namespace ZombieWar.UI
{
    public sealed class ScoreView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private GameFlowController _flow;
        [SerializeField] private TMP_Text _killsText;
        [SerializeField] private TMP_Text _scoreText;

        private void Awake()
        {
            if (_flow == null || _killsText == null || _scoreText == null)
            {
                Debug.LogError($"{LogPrefix} ScoreView has an unassigned reference.", this);
            }
        }

        private void OnEnable()
        {
            _flow.OnScoreChanged += HandleScoreChanged;
        }

        private void OnDisable()
        {
            _flow.OnScoreChanged -= HandleScoreChanged;
        }

        private void HandleScoreChanged(int kills, int score)
        {
            _killsText.SetText("{0}", kills);
            _scoreText.SetText("{0}", score);
        }
    }
}
