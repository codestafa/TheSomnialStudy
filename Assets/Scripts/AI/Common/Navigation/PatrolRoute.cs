using UnityEngine;

namespace AI.Common.Navigation
{
    public class PatrolRoute : MonoBehaviour
    {
        [Tooltip("Assign waypoints for this AI to patrol between.")]
        public Transform[] waypoints;

        private int currentIndex = 0;

        public Vector3 GetNextPoint()
        {
            if (waypoints == null || waypoints.Length == 0)
                return transform.position;

            currentIndex = (currentIndex + 1) % waypoints.Length;
            return waypoints[currentIndex].position;
        }

        public void ResetRoute()
        {
            currentIndex = 0;
        }

        public bool HasRoute()
        {
            return waypoints != null && waypoints.Length > 0;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (waypoints == null || waypoints.Length == 0) return;

            Gizmos.color = Color.cyan;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null) continue;

                Gizmos.DrawSphere(waypoints[i].position, 0.2f);
                Gizmos.DrawLine(
                    waypoints[i].position,
                    waypoints[(i + 1) % waypoints.Length].position
                );
            }
        }
#endif
    }
}
