using UnityEngine;

namespace AI.Common.Perception
{
    public class VisionSensor : MonoBehaviour
    {
        [Header("Vision Settings")]
        public float viewRadius = 10f;
        [Range(0, 360)] public float viewAngle = 90f;
        public LayerMask targetMask;
        public LayerMask obstacleMask;

        public bool CanSeeTarget(Transform target)
        {
            if (target == null) return false;

            Vector3 dirToTarget = (target.position - transform.position).normalized;
            float distanceToTarget = Vector3.Distance(transform.position, target.position);

            // Check if target is within field of view
            if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2f)
            {
                // Check if something blocks the view
                if (!Physics.Raycast(transform.position + Vector3.up * 1.5f, dirToTarget, distanceToTarget, obstacleMask))
                {
                    return distanceToTarget <= viewRadius;
                }
                else
                {
                    Debug.DrawRay(transform.position + Vector3.up * 1.5f, dirToTarget * distanceToTarget, Color.gray);
                    // Debug.Log($"{name} can't see player — obstacle blocking view.");
                }
            }

            return false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, viewRadius);

            Vector3 leftBoundary = DirFromAngle(-viewAngle / 2, false);
            Vector3 rightBoundary = DirFromAngle(viewAngle / 2, false);

            Vector3 pos = transform.position;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(pos, pos + leftBoundary * viewRadius);
            Gizmos.DrawLine(pos, pos + rightBoundary * viewRadius);

#if UNITY_EDITOR
            UnityEditor.Handles.color = new Color(1f, 0.5f, 0f, 0.2f);
            UnityEditor.Handles.DrawSolidArc(pos, Vector3.up, leftBoundary, viewAngle, viewRadius);
#endif
        }

        private Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
        {
            if (!angleIsGlobal)
                angleInDegrees += transform.eulerAngles.y;

            return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
        }
#endif
    }
}
