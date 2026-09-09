using System.Collections.Generic;
using UnityEngine;

namespace ZombieWar.Audio
{
    public sealed class AudioService : MonoBehaviour
    {
        private const string LogPrefix = "[Audio]";

        // Voices are authored as child AudioSources; their count is the world voice limit.
        [SerializeField] private AudioSource[] _worldVoices;

        [Header("Mix")]
        [SerializeField] private float _pitchJitter = 0.03f;
        // Identical clips requested inside this window are merged into one voice (shotgun pellets, crowd hits).
        [SerializeField] private float _sameClipMergeWindow = 0.03f;
        // Copies of one clip allowed to overlap; a crowd hitting the same grunt past this is dropped instead of stacked.
        [SerializeField] private int _maxVoicesPerClip = 3;

        private readonly Dictionary<AudioClip, float> _lastPlayTime = new Dictionary<AudioClip, float>(32);
        private float[] _voiceStartTime;
        private int _nextVoice;

        private void Awake()
        {
            if (_worldVoices == null || _worldVoices.Length == 0)
            {
                Debug.LogError($"{LogPrefix} No world voices assigned.", this);
                return;
            }

            _voiceStartTime = new float[_worldVoices.Length];
        }

        // Every world sound goes through these voices, so muting them mutes gameplay audio.
        public void SetMuted(bool muted)
        {
            for (int i = 0; i < _worldVoices.Length; i++)
            {
                _worldVoices[i].mute = muted;
            }
        }

        public void PlayWorld(AudioClip clip, Vector3 position)
        {
            PlayWorld(clip, position, 1f);
        }

        public void PlayWorld(AudioClip clip, Vector3 position, float volume)
        {
            if (clip == null || IsMerged(clip) || IsClipSaturated(clip))
            {
                return;
            }

            int index = AcquireVoice();
            AudioSource voice = _worldVoices[index];
            voice.transform.position = position;
            voice.pitch = 1f + Random.Range(-_pitchJitter, _pitchJitter);
            voice.volume = volume;
            // Play (not PlayOneShot) so a stolen voice replaces its old sound instead of layering on top of it.
            voice.clip = clip;
            voice.Play();
            _voiceStartTime[index] = Time.unscaledTime;
        }

        public void PlayWorldRandom(AudioClip[] clips, Vector3 position)
        {
            PlayWorldRandom(clips, position, 1f);
        }

        public void PlayWorldRandom(AudioClip[] clips, Vector3 position, float volume)
        {
            if (clips == null || clips.Length == 0)
            {
                return;
            }

            PlayWorld(clips[Random.Range(0, clips.Length)], position, volume);
        }

        private bool IsMerged(AudioClip clip)
        {
            float now = Time.unscaledTime;
            if (_lastPlayTime.TryGetValue(clip, out float last) && now - last < _sameClipMergeWindow)
            {
                return true;
            }

            _lastPlayTime[clip] = now;
            return false;
        }

        private bool IsClipSaturated(AudioClip clip)
        {
            int playing = 0;
            for (int i = 0; i < _worldVoices.Length; i++)
            {
                AudioSource voice = _worldVoices[i];
                if (voice.clip == clip && voice.isPlaying)
                {
                    playing++;
                }
            }

            return playing >= _maxVoicesPerClip;
        }

        private int AcquireVoice()
        {
            int count = _worldVoices.Length;
            for (int i = 0; i < count; i++)
            {
                int index = (_nextVoice + i) % count;
                if (_worldVoices[index].isPlaying)
                {
                    continue;
                }

                _nextVoice = (index + 1) % count;
                return index;
            }

            // Every voice is busy: steal the one that has been playing the longest.
            int oldest = 0;
            for (int i = 1; i < count; i++)
            {
                if (_voiceStartTime[i] < _voiceStartTime[oldest])
                {
                    oldest = i;
                }
            }

            _nextVoice = (oldest + 1) % count;
            return oldest;
        }
    }
}
