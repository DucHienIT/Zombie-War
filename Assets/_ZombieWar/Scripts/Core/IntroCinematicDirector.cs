using Cinemachine;
using UnityEngine;

namespace ZombieWar.Core
{
    // Opening shot of every run: a second virtual camera sweeps around the soldier while the
    // flow ticks the intro, then drops out so the brain blends back into the top-down camera.
    // The flow owns the clock; this only reads its progress and places the camera.
    public sealed class IntroCinematicDirector : MonoBehaviour
    {
        private const string LogPrefix = "[Camera]";

        [SerializeField] private GameFlowController _flow;
        // Higher priority than the follow camera and inactive while idle: activating it is the cut in,
        // deactivating it is what starts the blend back out.
        [SerializeField] private CinemachineVirtualCamera _introCamera;
        [SerializeField] private Transform _player;

        [Header("Orbit")]
        // Yaw is measured from the soldier's facing: 0 is straight in front, negative is their left.
        [SerializeField] private float _startYaw = -70f;
        [SerializeField] private float _endYaw = 30f;
        [SerializeField] private float _startDistance = 3.2f;
        [SerializeField] private float _endDistance = 4.4f;
        [SerializeField] private float _startHeight = 0.8f;
        [SerializeField] private float _endHeight = 2.1f;
        [SerializeField] private float _lookAtHeight = 1.15f;
        [SerializeField] private AnimationCurve _progressCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Transform _cameraTransform;

        private void Awake()
        {
            if (_flow == null || _introCamera == null || _player == null)
            {
                Debug.LogError($"{LogPrefix} IntroCinematicDirector has an unassigned reference.", this);
                return;
            }

            _cameraTransform = _introCamera.transform;
        }

        private void OnEnable()
        {
            _flow.OnStateChanged += HandleStateChanged;
            _flow.OnIntroProgress += HandleIntroProgress;
        }

        private void OnDisable()
        {
            _flow.OnStateChanged -= HandleStateChanged;
            _flow.OnIntroProgress -= HandleIntroProgress;
        }

        private void HandleStateChanged(GameState state)
        {
            bool intro = state == GameState.Intro;
            if (intro)
            {
                // The soldier was just teleported to the spawn; pose the shot before the camera goes live
                // so the first frame is never a stale position from the previous run.
                PlaceCamera(0f);
                _introCamera.PreviousStateIsValid = false;
            }

            _introCamera.gameObject.SetActive(intro);
        }

        private void HandleIntroProgress(float normalized) => PlaceCamera(_progressCurve.Evaluate(normalized));

        private void PlaceCamera(float t)
        {
            Vector3 anchor = _player.position;
            float yaw = _player.eulerAngles.y + Mathf.Lerp(_startYaw, _endYaw, t);
            Vector3 around = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * Mathf.Lerp(_startDistance, _endDistance, t);
            Vector3 position = anchor + around + Vector3.up * Mathf.Lerp(_startHeight, _endHeight, t);
            Vector3 lookAt = anchor + Vector3.up * _lookAtHeight;
            _cameraTransform.SetPositionAndRotation(position, Quaternion.LookRotation(lookAt - position, Vector3.up));
        }
    }
}
