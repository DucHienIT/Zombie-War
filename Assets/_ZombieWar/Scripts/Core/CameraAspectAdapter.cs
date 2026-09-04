using Cinemachine;
using UnityEngine;

namespace ZombieWar.Core
{
    // Portrait game: keeps the horizontal field of view constant so every aspect shows the same lane width.
    public sealed class CameraAspectAdapter : MonoBehaviour
    {
        private const string LogPrefix = "[Camera]";

        [SerializeField] private CinemachineVirtualCamera _virtualCamera;

        [Header("Lens")]
        [SerializeField] private float _horizontalFov = 34f;
        [SerializeField] private float _minVerticalFov = 45f;
        [SerializeField] private float _maxVerticalFov = 70f;

        private float _appliedAspect;

        private void Awake()
        {
            if (_virtualCamera == null)
            {
                Debug.LogError($"{LogPrefix} Virtual camera is missing.", this);
            }
        }

        private void Update()
        {
            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 1f;
            if (Mathf.Approximately(aspect, _appliedAspect))
            {
                return;
            }

            _appliedAspect = aspect;
            float halfHorizontalTan = Mathf.Tan(_horizontalFov * 0.5f * Mathf.Deg2Rad);
            float verticalFov = 2f * Mathf.Atan(halfHorizontalTan / aspect) * Mathf.Rad2Deg;
            _virtualCamera.m_Lens.FieldOfView = Mathf.Clamp(verticalFov, _minVerticalFov, _maxVerticalFov);
        }
    }
}
