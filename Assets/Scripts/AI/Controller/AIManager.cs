// AIManager.cs - Final
using System.Collections.Generic;
using UnityEngine;
using AI.Common.Navigation;

namespace AI.Controller
{
    [System.Serializable]
    public class EnemySpawnEntry
    {
        [Tooltip("Scene enemy instance, not prefab.")]
        public GameObject enemyInstance;
        [Tooltip("Possible spawn points for this enemy.")]
        public JobSpawnPoint[] spawnPoints;
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

            JobSpawnPoint chosenSpawnPoint = entry.spawnPoints[Random.Range(0, entry.spawnPoints.Length)];

            entry.enemyInstance.transform.position = chosenSpawnPoint.transform.position;
            entry.enemyInstance.transform.rotation = Quaternion.identity;

            AIController aiController = entry.enemyInstance.GetComponent<AIController>();
            if (aiController != null)
            {
                aiController.SetSpawnContext(chosenSpawnPoint.spawnType);
            }
        }
    }
}
