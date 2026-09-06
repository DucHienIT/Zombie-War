using UnityEngine;

namespace ZombieWar.UI
{
    // Vertical list of the active skills the player has drafted this run, down the right edge
    // of the HUD. Slots beyond what is owned just sit hidden - nothing here ever grows the list.
    public sealed class ActiveSkillHudView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private ActiveSkillSlotView[] _slots;

        private void Awake()
        {
            if (_slots == null || _slots.Length == 0)
            {
                Debug.LogError($"{LogPrefix} ActiveSkillHudView has no slots assigned.", this);
            }
        }

        public void SetSkills(ActiveSkillHudEntry[] skills, int count)
        {
            if (count > _slots.Length)
            {
                Debug.LogError($"{LogPrefix} {count} active skills equipped but the HUD only has {_slots.Length} slots.", this);
                count = _slots.Length;
            }

            for (int i = 0; i < _slots.Length; i++)
            {
                bool active = i < count;
                _slots[i].gameObject.SetActive(active);
                if (active)
                {
                    _slots[i].Bind(skills[i]);
                }
            }
        }
    }
}
