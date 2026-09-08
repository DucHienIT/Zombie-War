using Cinemachine;
using UnityEngine;
using ZombieWar.Data;

namespace ZombieWar.Level
{
    // Builds the world for one run. Nothing happens until the flow starts a run, so the
    // menu overlay sits in an empty scene with the player switched off.
    public sealed class LevelMapLoader : MonoBehaviour
    {
        private const string LogPrefix = "[Level]";

        [SerializeField] private Transform _mapParent;
        [SerializeField] private Rigidbody _playerBody;
        [SerializeField] private CinemachineVirtualCamera _virtualCamera;

        public LevelDefinitionSO Level { get; private set; }
        public LevelMap Map { get; private set; }

        private void Awake()
        {
            if (_mapParent == null || _playerBody == null || _virtualCamera == null)
            {
                Debug.LogError($"{LogPrefix} LevelMapLoader has an unassigned reference.", this);
                return;
            }

            _playerBody.gameObject.SetActive(false);
        }

        public void Load(LevelDefinitionSO level)
        {
            if (level.MapPrefab == null)
            {
                Debug.LogError($"{LogPrefix} {level.name} has no map prefab assigned.", this);
                return;
            }

            // Destroy is deferred to the end of the frame, so the old map is switched off first:
            // that is what makes its NavMeshSurface drop its data before the new one adds its own.
            if (Map != null)
            {
                Map.gameObject.SetActive(false);
                Destroy(Map.gameObject);
            }

            Level = level;
            Map = Instantiate(level.MapPrefab, _mapParent);

            Transform spawn = Map.PlayerSpawn;
            _playerBody.gameObject.SetActive(true);
            _playerBody.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
            _playerBody.position = spawn.position;
            _playerBody.rotation = spawn.rotation;
            _playerBody.velocity = Vector3.zero;
            _playerBody.angularVelocity = Vector3.zero;

            // The follow target just teleported across the map; without this the camera
            // would glide in from wherever it idled during the menu.
            _virtualCamera.PreviousStateIsValid = false;
        }

        // Back to the menu: the world goes away and the player is parked, leaving the scene in
        // exactly the state it boots into.
        public void Unload()
        {
            if (Map != null)
            {
                Map.gameObject.SetActive(false);
                Destroy(Map.gameObject);
            }

            Map = null;
            Level = null;
            _playerBody.velocity = Vector3.zero;
            _playerBody.angularVelocity = Vector3.zero;
            _playerBody.gameObject.SetActive(false);
        }
    }
}
