using UnityEngine;
using ZombieWar.Audio;
using ZombieWar.UI;

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
        // Shake keeps its own switch; this only routes the sheet's row to it.
        [SerializeField] private CameraShakeController _cameraShake;

        private readonly SaveService _save = new SaveService();

        public bool SoundEnabled { get; private set; }
        public bool MusicEnabled { get; private set; }
        // Stored and offered in the settings sheet, but nothing raises haptics yet.
        public bool HapticsEnabled { get; private set; }

        private void Awake()
        {
            if (_world == null || _music == null || _uiVoice == null || _cameraShake == null)
            {
                Debug.LogError($"{LogPrefix} SettingsService has an unassigned reference.", this);
                return;
            }

            ApplySound(_save.SoundEnabled);
            ApplyMusic(_save.MusicEnabled);
            HapticsEnabled = _save.HapticsEnabled;
        }

        public SettingsData Snapshot() =>
            new SettingsData(SoundEnabled, MusicEnabled, HapticsEnabled, _cameraShake.IsEnabled);

        // The sheet only says which row moved; which switch owns that row is decided here.
        public void Apply(SettingId id, bool enabled)
        {
            switch (id)
            {
                case SettingId.Sound:
                    SetSoundEnabled(enabled);
                    break;
                case SettingId.Music:
                    SetMusicEnabled(enabled);
                    break;
                case SettingId.Haptics:
                    SetHapticsEnabled(enabled);
                    break;
                case SettingId.CameraShake:
                    _cameraShake.SetEnabled(enabled);
                    break;
            }
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
