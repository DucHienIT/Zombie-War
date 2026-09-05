using Cinemachine;
using UnityEngine;

namespace ZombieWar.Core
{
    // The one switch for camera shake. Guns and bombs raise impulses on their own sources,
    // but every impulse lands on this listener, so muting its gain silences all of them.
    public sealed class CameraShakeController : MonoBehaviour
    {
        private const string LogPrefix = "[Camera]";

        [SerializeField] private CinemachineImpulseListener _listener;
        // Listener gain while shake is on; "off" writes zero.
        [SerializeField] private float _enabledGain = 1f;

        private readonly SaveService _save = new SaveService();

        public bool IsEnabled { get; private set; }

        private void Awake()
        {
            if (_listener == null)
            {
                Debug.LogError($"{LogPrefix} CameraShakeController has no impulse listener.", this);
                return;
            }

            Apply(_save.CameraShakeEnabled);
        }

        public void SetEnabled(bool enabled)
        {
            if (enabled == IsEnabled)
            {
                return;
            }

            _save.SetCameraShakeEnabled(enabled);
            Apply(enabled);
        }

        private void Apply(bool enabled)
        {
            IsEnabled = enabled;
            _listener.m_Gain = enabled ? _enabledGain : 0f;
        }
    }
}
