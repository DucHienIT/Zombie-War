namespace ZombieWar.UI
{
    // What the settings sheet draws. The popup never sees SettingsService or CameraShakeController.
    public readonly struct SettingsData
    {
        public readonly bool Sound;
        public readonly bool Music;
        public readonly bool Haptics;
        public readonly bool CameraShake;

        public SettingsData(bool sound, bool music, bool haptics, bool cameraShake)
        {
            Sound = sound;
            Music = music;
            Haptics = haptics;
            CameraShake = cameraShake;
        }
    }
}
