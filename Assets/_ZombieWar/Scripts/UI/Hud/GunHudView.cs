using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ZombieWar.UI
{
    // Shows the gun in hand and doubles as the switch button: the weapon-select popup only
    // picks the starting gun, tapping this cycles to the next one mid-match.
    public sealed class GunHudView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private Button _switchButton;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameText;

        private void Awake()
        {
            if (_switchButton == null || _icon == null || _nameText == null)
            {
                Debug.LogError($"{LogPrefix} GunHudView has an unassigned reference.", this);
            }
        }

        public void Init(UnityAction onSwitchRequested) => _switchButton.onClick.AddListener(onSwitchRequested);

        public void SetGun(Sprite icon, string displayName)
        {
            _icon.sprite = icon;
            _nameText.text = displayName;
        }
    }
}
