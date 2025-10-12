using System.Collections.Generic;
using UnityEngine;

namespace AI.Controller
{
    [System.Serializable]
    public class EnemySpawnEntry
    {
        public GameObject enemyInstance;      // Scene enemy, not prefab
        public JobSpawnPoint[] spawnPoints;   // Possible spawn points
    }

    public class AIManager : MonoBehaviour
    {
        [Header("Enemy Spawn Entries (Scene References)")]
        public List<EnemySpawnEntry> enemySpawnEntries = new List<EnemySpawnEntry>();

        private void Start()
        {
            foreach (var entry in enemySpawnEntries)
            {
                AssignEnemyToRandomJobPoint(entry);
            }
        }

        private void AssignEnemyToRandomJobPoint(EnemySpawnEntry entry)
        {
            if (entry == null || entry.enemyInstance == null || entry.spawnPoints == null || entry.spawnPoints.Length == 0)
            {
                Debug.LogWarning("EnemySpawnEntry is not set up properly.");
                return;
            }

            // Pick a random spawn point
            JobSpawnPoint chosenSpawnPoint = entry.spawnPoints[Random.Range(0, entry.spawnPoints.Length)];

            // Move the existing enemy to that position
            entry.enemyInstance.transform.position = chosenSpawnPoint.transform.position;
            entry.enemyInstance.transform.rotation = Quaternion.identity;

            // Ensure AIController knows its job
            AIController aiController = entry.enemyInstance.GetComponent<AIController>();
            if (aiController != null)
            {
                aiController.SetJob(chosenSpawnPoint.jobType);
            }
            else
            {
                Debug.LogWarning($"Enemy '{entry.enemyInstance.name}' is missing an AIController component.");
            }
        }
    }
}
