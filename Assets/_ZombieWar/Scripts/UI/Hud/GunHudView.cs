using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieWar.UI
{
    // Purely informational: the run's gun is locked in on the weapon-select popup before the
    // countdown starts, so there is nothing left to click on here mid-match.
    public sealed class GunHudView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameText;

        private void Awake()
        {
            if (_icon == null || _nameText == null)
            {
                Debug.LogError($"{LogPrefix} GunHudView has an unassigned reference.", this);
            }
        }

        public void SetGun(Sprite icon, string displayName)
        {
            _icon.sprite = icon;
            _nameText.text = displayName;
        }
    }
}
