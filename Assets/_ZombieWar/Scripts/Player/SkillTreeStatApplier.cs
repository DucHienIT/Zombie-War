using UnityEngine;
using ZombieWar.Core;

namespace ZombieWar.Player
{
    // Copies the ranks bought in the menu into the stat sheet as the layer every in-run rebuild
    // starts from, so a roguelike pick can never wipe a permanent upgrade. It only stages the
    // numbers: the rebuild that makes them live is the one the run start already triggers.
    public sealed class SkillTreeStatApplier : MonoBehaviour
    {
        private const string LogPrefix = "[Profile]";

        [SerializeField] private ProfileService _profile;
        [SerializeField] private PlayerStatSheet _stats;

        private void Awake()
        {
            if (_profile == null || _stats == null)
            {
                Debug.LogError($"{LogPrefix} SkillTreeStatApplier has an unassigned reference - bought skills would do nothing.", this);
            }
        }

        private void OnEnable()
        {
            _profile.OnChanged += Push;
            Push();
        }

        private void OnDisable()
        {
            _profile.OnChanged -= Push;
        }

        private void Push()
        {
            SkillTreeProgress skills = _profile.Skills;
            _stats.ClearPermanent();
            for (int i = 0; i < skills.Count; i++)
            {
                int rank = skills.RankAt(i);
                if (rank > 0)
                {
                    _stats.AddPermanent(skills.NodeAt(i).ModifiersPerRank, rank);
                }
            }
        }
    }
}
