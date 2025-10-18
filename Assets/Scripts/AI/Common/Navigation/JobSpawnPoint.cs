using UnityEngine;

namespace AI.Common.Navigation
{
    [DisallowMultipleComponent]
    public class JobSpawnPoint : MonoBehaviour
    {
        [Header("Job Metadata")]
        public SpawnType spawnType;

        [Tooltip("Optional orientation for the AI when spawned.")]
        public Vector3 facingDirection = Vector3.forward;

        [Tooltip("Optional: Voice cue or animation to play when AI spawns here.")]
        public string voiceCue;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.25f);
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, spawnType.ToString());
        }
#endif
    }
}
