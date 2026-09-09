using UnityEngine;
using UnityEngine.UI;

namespace ZombieWar.UI
{
    // One icon in the active-skill HUD list: the frame is always on, the icon and cooldown
    // sweep are the only things that change.
    public sealed class ActiveSkillSlotView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private Image _icon;
        [SerializeField] private Image _cooldownOverlay;

        private void Awake()
        {
            if (_icon == null || _cooldownOverlay == null)
            {
                Debug.LogError($"{LogPrefix} ActiveSkillSlotView has an unassigned reference.", this);
            }
        }

        public void Bind(in ActiveSkillHudEntry entry)
        {
            _icon.sprite = entry.Icon;
            _cooldownOverlay.fillAmount = entry.CooldownFraction;
        }
    }
}
