using Unity.AI.Navigation;
using UnityEngine;

namespace ZombieWar.Level
{
    // Root of a level map prefab: geometry, obstacles, lighting, baked NavMesh and the player spawn point.
    public sealed class LevelMap : MonoBehaviour
    {
        private const string LogPrefix = "[Level]";

        [SerializeField] private Transform _playerSpawn;
        [SerializeField] private NavMeshSurface _navMeshSurface;

        public Transform PlayerSpawn => _playerSpawn;

        private void Awake()
        {
            if (_playerSpawn == null || _navMeshSurface == null)
            {
                Debug.LogError($"{LogPrefix} {name} is missing its player spawn or NavMeshSurface.", this);
                return;
            }

            if (_navMeshSurface.navMeshData == null)
            {
                Debug.LogError($"{LogPrefix} {name} has no baked NavMesh data; bake it in the prefab before playing.", this);
            }
        }
    }
}
