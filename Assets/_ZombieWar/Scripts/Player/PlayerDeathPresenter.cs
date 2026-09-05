using DG.Tweening;
using UnityEngine;

namespace ZombieWar.Player
{
    // Fallback death pose: the imported packs ship no soldier death clip, so the model pivot is tipped over by tween.
    public sealed class PlayerDeathPresenter : MonoBehaviour
    {
        private const string LogPrefix = "[Player]";

        [SerializeField] private PlayerHealth _health;
        [SerializeField] private Transform _modelPivot;

        [Header("Fall")]
        [SerializeField] private Vector3 _fallEulerAngles;
        [SerializeField] private float _fallDuration = 0.6f;
        [SerializeField] private Ease _fallEase = Ease.OutBounce;

        private void Awake()
        {
            if (_health == null || _modelPivot == null)
            {
                Debug.LogError($"{LogPrefix} PlayerDeathPresenter has an unassigned reference.", this);
            }
        }

        private void OnEnable()
        {
            _health.OnDied += HandleDied;
        }

        private void OnDisable()
        {
            _health.OnDied -= HandleDied;
        }

        private void HandleDied()
        {
            // A hit jolt may still be running on the pivot; the fall takes over completely.
            _modelPivot.DOKill();
            _modelPivot.DOLocalRotate(_fallEulerAngles, _fallDuration).SetEase(_fallEase).SetLink(gameObject);
        }
    }
}
