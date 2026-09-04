using UnityEngine;
using ZombieWar.Data;

namespace ZombieWar.Level
{
    // Runs before every other gameplay Awake so the map, NavMesh and player spawn exist when systems initialise.
    [DefaultExecutionOrder(-100)]
    public sealed class LevelMapLoader : MonoBehaviour
    {
        private const string LogPrefix = "[Level]";

        [SerializeField] private LevelSelectionSO _selection;
        // Used when the gameplay scene is opened directly without passing through the menu.
        [SerializeField] private LevelDefinitionSO _fallbackLevel;
        [SerializeField] private Transform _mapParent;
        [SerializeField] private Rigidbody _playerBody;

        public LevelDefinitionSO Level { get; private set; }
        public LevelMap Map { get; private set; }

        private void Awake()
        {
            if (_selection == null || _fallbackLevel == null || _mapParent == null || _playerBody == null)
            {
                Debug.LogError($"{LogPrefix} LevelMapLoader has an unassigned reference.", this);
                return;
            }

            Level = _selection.Selected != null ? _selection.Selected : _fallbackLevel;
            if (Level.MapPrefab == null)
            {
                Debug.LogError($"{LogPrefix} {Level.name} has no map prefab assigned.", this);
                return;
            }

            Map = Instantiate(Level.MapPrefab, _mapParent);
            Transform spawn = Map.PlayerSpawn;
            _playerBody.position = spawn.position;
            _playerBody.rotation = spawn.rotation;
            _playerBody.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
        }
    }
}
