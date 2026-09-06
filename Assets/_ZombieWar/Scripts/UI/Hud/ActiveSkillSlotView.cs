using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieWar.UI
{
    // One icon in the active-skill HUD list: the frame is always on, the icon and stack badge
    // are the only things that change.
    public sealed class ActiveSkillSlotView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private Image _icon;
        [SerializeField] private Image _cooldownOverlay;
        [SerializeField] private GameObject _stackBadge;
        [SerializeField] private TMP_Text _stackText;

        private void Awake()
        {
            if (_icon == null || _cooldownOverlay == null || _stackBadge == null || _stackText == null)
            {
                Debug.LogError($"{LogPrefix} ActiveSkillSlotView has an unassigned reference.", this);
            }
        }

        public void Bind(in ActiveSkillHudEntry entry)
        {
            _icon.sprite = entry.Icon;
            _cooldownOverlay.fillAmount = entry.CooldownFraction;
            bool showStack = entry.Stacks > 1;
            _stackBadge.SetActive(showStack);
            if (showStack)
            {
                _stackText.SetText("{0}", entry.Stacks);
            }
        }
    }
}
