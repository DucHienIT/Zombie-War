using UnityEngine;
using UnityEngine.AI;
using ZombieWar.Data;

namespace ZombieWar.Level
{
    public sealed class SpawnPointResolver
    {
        private readonly LevelDefinitionSO _level;
        private readonly Camera _camera;

        public SpawnPointResolver(LevelDefinitionSO level, Camera camera)
        {
            _level = level;
            _camera = camera;
        }

        public bool TryResolve(Vector3 playerPosition, out Vector3 result)
        {
            for (int attempt = 0; attempt < _level.SpawnAttempts; attempt++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float radius = Random.Range(_level.SpawnRingInner, _level.SpawnRingOuter);
                Vector3 candidate = playerPosition + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

                if (IsInsideViewport(candidate))
                {
                    continue;
                }

                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, _level.NavMeshSampleRadius, NavMesh.AllAreas))
                {
                    continue;
                }

                Vector3 offset = hit.position - playerPosition;
                offset.y = 0f;
                if (offset.sqrMagnitude < _level.MinSpawnDistance * _level.MinSpawnDistance)
                {
                    continue;
                }

                result = hit.position;
                return true;
            }

            result = default;
            return false;
        }

        private bool IsInsideViewport(Vector3 worldPosition)
        {
            Vector3 viewport = _camera.WorldToViewportPoint(worldPosition);
            float margin = _level.ViewportMargin;
            bool insideX = viewport.x > -margin && viewport.x < 1f + margin;
            bool insideY = viewport.y > -margin && viewport.y < 1f + margin;
            return viewport.z > 0f && insideX && insideY;
        }
    }
}
