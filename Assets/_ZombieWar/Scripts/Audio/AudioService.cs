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

        private readonly Dictionary<AudioClip, float> _lastPlayTime = new Dictionary<AudioClip, float>(32);
        private int _nextVoice;

        private void Awake()
        {
            if (_worldVoices == null || _worldVoices.Length == 0)
            {
                Debug.LogError($"{LogPrefix} No world voices assigned.", this);
            }
        }

        public void PlayWorld(AudioClip clip, Vector3 position)
        {
            if (clip == null || IsMerged(clip))
            {
                return;
            }

            AudioSource voice = AcquireVoice();
            voice.transform.position = position;
            voice.pitch = 1f + Random.Range(-_pitchJitter, _pitchJitter);
            voice.PlayOneShot(clip);
        }

        public void PlayWorldRandom(AudioClip[] clips, Vector3 position)
        {
            if (clips == null || clips.Length == 0)
            {
                return;
            }

            PlayWorld(clips[Random.Range(0, clips.Length)], position);
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

        private AudioSource AcquireVoice()
        {
            int count = _worldVoices.Length;
            for (int i = 0; i < count; i++)
            {
                AudioSource candidate = _worldVoices[(_nextVoice + i) % count];
                if (candidate.isPlaying)
                {
                    continue;
                }

                _nextVoice = (_nextVoice + i + 1) % count;
                return candidate;
            }

            // Every voice is busy: steal the oldest in round-robin order.
            AudioSource stolen = _worldVoices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % count;
            return stolen;
        }
    }
}
