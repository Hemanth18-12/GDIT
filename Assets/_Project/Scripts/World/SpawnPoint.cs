using UnityEngine;

namespace LegacyOfTemples.World
{
    [DisallowMultipleComponent]
    public class SpawnPoint : MonoBehaviour
    {
        [Tooltip("Named spawn identifier, e.g. player_start, robber_spawn, priest_spawn, villager_spawn")]
        [SerializeField] private string spawnId = "player_start";

        public string SpawnId => spawnId;

        public void SetSpawnId(string id)
        {
            spawnId = id;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.9f, 0.7f, 0.1f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, 0.4f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1.0f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position + Vector3.up * 0.2f, 0.25f);
            Gizmos.DrawRay(transform.position + Vector3.up * 0.2f, transform.forward * 1.5f);
        }
    }
}

