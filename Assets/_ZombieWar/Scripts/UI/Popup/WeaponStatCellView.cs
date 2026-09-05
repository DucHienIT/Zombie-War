using TMPro;
using UnityEngine;

namespace ZombieWar.UI
{
    // One cell of the weapon detail grid: a value and, when an upgrade is previewed, the next value.
    public sealed class WeaponStatCellView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private TMP_Text _valueText;
        [SerializeField] private TMP_Text _nextText;
        [SerializeField] private GameObject _nextGroup;

        private void Awake()
        {
            if (_valueText == null || _nextText == null || _nextGroup == null)
            {
                Debug.LogError($"{LogPrefix} WeaponStatCellView has an unassigned reference.", this);
            }
        }

        public void Set(string format, float current, float next, bool showNext)
        {
            _valueText.SetText(format, current);
            _nextGroup.SetActive(showNext);
            if (showNext)
            {
                _nextText.SetText(format, next);
            }
        }

        // Static stats (pellet count, fire type) never have a preview column.
        public void SetCount(int value)
        {
            _valueText.SetText("{0}", value);
            _nextGroup.SetActive(false);
        }

        public void SetLabel(string value)
        {
            _valueText.text = value;
            _nextGroup.SetActive(false);
        }
    }
}
