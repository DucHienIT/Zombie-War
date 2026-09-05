using UnityEngine;
using ZombieWar.Weapons;

namespace ZombieWar.Player
{
    // Pins both hands to the grips of the current gun. The gun hangs off the body, not off a hand,
    // so the retargeted combat clip only decides the torso and the elbows - the barrel never bends.
    public sealed class WeaponHandIk : MonoBehaviour
    {
        private const string LogPrefix = "[Player]";

        private static readonly HumanBodyBones[] RightFingerBones =
        {
            HumanBodyBones.RightIndexProximal, HumanBodyBones.RightIndexIntermediate, HumanBodyBones.RightIndexDistal,
            HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightMiddleDistal,
            HumanBodyBones.RightRingProximal, HumanBodyBones.RightRingIntermediate, HumanBodyBones.RightRingDistal,
            HumanBodyBones.RightLittleProximal, HumanBodyBones.RightLittleIntermediate, HumanBodyBones.RightLittleDistal,
        };

        private static readonly HumanBodyBones[] LeftFingerBones =
        {
            HumanBodyBones.LeftIndexProximal, HumanBodyBones.LeftIndexIntermediate, HumanBodyBones.LeftIndexDistal,
            HumanBodyBones.LeftMiddleProximal, HumanBodyBones.LeftMiddleIntermediate, HumanBodyBones.LeftMiddleDistal,
            HumanBodyBones.LeftRingProximal, HumanBodyBones.LeftRingIntermediate, HumanBodyBones.LeftRingDistal,
            HumanBodyBones.LeftLittleProximal, HumanBodyBones.LeftLittleIntermediate, HumanBodyBones.LeftLittleDistal,
        };

        private static readonly HumanBodyBones[] RightThumbBones =
        {
            HumanBodyBones.RightThumbProximal, HumanBodyBones.RightThumbIntermediate, HumanBodyBones.RightThumbDistal,
        };

        private static readonly HumanBodyBones[] LeftThumbBones =
        {
            HumanBodyBones.LeftThumbProximal, HumanBodyBones.LeftThumbIntermediate, HumanBodyBones.LeftThumbDistal,
        };

        [SerializeField] private Animator _animator;
        [SerializeField] private WeaponController _weapons;
        [SerializeField] private Transform _rightElbowHint;
        [SerializeField] private Transform _leftElbowHint;

        [Header("Weights")]
        [SerializeField, Range(0f, 1f)] private float _handWeight = 1f;
        [SerializeField, Range(0f, 1f)] private float _elbowHintWeight = 0.6f;

        [Header("Finger Curl")]
        // Local euler added to every phalanx on top of the rest pose, so the open hand closes on the grip.
        [SerializeField] private Vector3 _rightFingerCurl;
        [SerializeField] private Vector3 _leftFingerCurl;
        [SerializeField] private Vector3 _rightThumbCurl;
        [SerializeField] private Vector3 _leftThumbCurl;

        private readonly Quaternion[] _rightFingerRest = new Quaternion[RightFingerBones.Length];
        private readonly Quaternion[] _leftFingerRest = new Quaternion[LeftFingerBones.Length];
        private readonly Quaternion[] _rightThumbRest = new Quaternion[RightThumbBones.Length];
        private readonly Quaternion[] _leftThumbRest = new Quaternion[LeftThumbBones.Length];

        private Quaternion _rightGoalOffset = Quaternion.identity;
        private Quaternion _leftGoalOffset = Quaternion.identity;
        private Quaternion _rightCalibrationGoal;
        private Quaternion _leftCalibrationGoal;
        private bool _calibrationRequested;
        private bool _offsetsCached;

        private void Awake()
        {
            if (_animator == null || _weapons == null || _rightElbowHint == null || _leftElbowHint == null)
            {
                Debug.LogError($"{LogPrefix} WeaponHandIk has an unassigned reference.", this);
                return;
            }

            // Runs before the first animator update, so these are the authored rest rotations.
            CacheRest(RightFingerBones, _rightFingerRest);
            CacheRest(LeftFingerBones, _leftFingerRest);
            CacheRest(RightThumbBones, _rightThumbRest);
            CacheRest(LeftThumbBones, _leftThumbRest);
        }

        private void CacheRest(HumanBodyBones[] bones, Quaternion[] rest)
        {
            for (int i = 0; i < bones.Length; i++)
            {
                Transform bone = _animator.GetBoneTransform(bones[i]);
                rest[i] = bone != null ? bone.localRotation : Quaternion.identity;
            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            Gun gun = _weapons.CurrentGun;
            if (gun == null)
            {
                Release();
                return;
            }

            _weapons.AlignBarrel();
            if (!_offsetsCached)
            {
                _rightCalibrationGoal = gun.RightHandGrip.rotation;
                _leftCalibrationGoal = gun.LeftHandGrip.rotation;
                _calibrationRequested = true;
            }

            Pin(AvatarIKGoal.RightHand, AvatarIKHint.RightElbow, gun.RightHandGrip, _rightElbowHint, _rightGoalOffset);
            Pin(AvatarIKGoal.LeftHand, AvatarIKHint.LeftElbow, gun.LeftHandGrip, _leftElbowHint, _leftGoalOffset);
            Curl(RightFingerBones, _rightFingerRest, _rightFingerCurl);
            Curl(LeftFingerBones, _leftFingerRest, _leftFingerCurl);
            Curl(RightThumbBones, _rightThumbRest, _rightThumbCurl);
            Curl(LeftThumbBones, _leftThumbRest, _leftThumbCurl);
        }

        // The IK goal frame is not the hand bone frame: the avatar defines a canonical hand
        // orientation, a constant twist away from the bone. Bone transforms are stale inside
        // OnAnimatorIK, so the twist is measured once here, on the solved pose of the first frame
        // that pinned the hands with the raw grip rotation, and undone on every later goal.
        private void LateUpdate()
        {
            if (_offsetsCached || !_calibrationRequested)
            {
                return;
            }

            _rightGoalOffset = GoalOffset(HumanBodyBones.RightHand, _rightCalibrationGoal);
            _leftGoalOffset = GoalOffset(HumanBodyBones.LeftHand, _leftCalibrationGoal);
            _offsetsCached = true;
        }

        private Quaternion GoalOffset(HumanBodyBones bone, Quaternion goalSet)
        {
            Quaternion boneRotation = _animator.GetBoneTransform(bone).rotation;
            Quaternion twist = Quaternion.Inverse(goalSet) * boneRotation;
            return Quaternion.Inverse(twist);
        }

        private void Pin(AvatarIKGoal goal, AvatarIKHint hint, Transform grip, Transform elbowHint, Quaternion goalOffset)
        {
            _animator.SetIKPositionWeight(goal, _handWeight);
            _animator.SetIKRotationWeight(goal, _handWeight);
            _animator.SetIKPosition(goal, grip.position);
            _animator.SetIKRotation(goal, grip.rotation * goalOffset);
            _animator.SetIKHintPositionWeight(hint, _elbowHintWeight);
            _animator.SetIKHintPosition(hint, elbowHint.position);
        }

        private void Curl(HumanBodyBones[] bones, Quaternion[] rest, Vector3 curlEuler)
        {
            Quaternion curl = Quaternion.Euler(curlEuler);
            for (int i = 0; i < bones.Length; i++)
            {
                _animator.SetBoneLocalRotation(bones[i], rest[i] * curl);
            }
        }

        private void Release()
        {
            _animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
            _animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
            _animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
        }
    }
}
