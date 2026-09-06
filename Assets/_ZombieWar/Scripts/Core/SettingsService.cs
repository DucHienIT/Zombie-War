using UnityEngine;
using ZombieWar.Audio;

namespace ZombieWar.Core
{
    // The one switch for sound, music and haptics, the way CameraShakeController is the one
    // switch for shake. It owns the saved flags and applies them to the sources that make noise.
    public sealed class SettingsService : MonoBehaviour
    {
        private const string LogPrefix = "[Settings]";

        [SerializeField] private AudioService _world;
        [SerializeField] private AudioSource _music;
        // The UI plays its taps through its own source inside the UI prefab.
        [SerializeField] private AudioSource _uiVoice;

        private readonly SaveService _save = new SaveService();

        public bool SoundEnabled { get; private set; }
        public bool MusicEnabled { get; private set; }
        // Stored and offered in the settings sheet, but nothing raises haptics yet.
        public bool HapticsEnabled { get; private set; }

        private void Awake()
        {
            if (_world == null || _music == null || _uiVoice == null)
            {
                Debug.LogError($"{LogPrefix} SettingsService has an unassigned reference.", this);
                return;
            }

            ApplySound(_save.SoundEnabled);
            ApplyMusic(_save.MusicEnabled);
            HapticsEnabled = _save.HapticsEnabled;
        }

        public void SetSoundEnabled(bool enabled)
        {
            if (enabled == SoundEnabled)
            {
                return;
            }

            _save.SetSoundEnabled(enabled);
            ApplySound(enabled);
        }

        public void SetMusicEnabled(bool enabled)
        {
            if (enabled == MusicEnabled)
            {
                return;
            }

            _save.SetMusicEnabled(enabled);
            ApplyMusic(enabled);
        }

        public void SetHapticsEnabled(bool enabled)
        {
            if (enabled == HapticsEnabled)
            {
                return;
            }

            _save.SetHapticsEnabled(enabled);
            HapticsEnabled = enabled;
        }

        private void ApplySound(bool enabled)
        {
            SoundEnabled = enabled;
            _world.SetMuted(!enabled);
            _uiVoice.mute = !enabled;
        }

        private void ApplyMusic(bool enabled)
        {
            MusicEnabled = enabled;
            _music.mute = !enabled;
        }
    }
}
