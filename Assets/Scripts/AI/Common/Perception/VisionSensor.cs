using UnityEngine;

namespace AI.Common.Perception
{
    public class VisionSensor : MonoBehaviour
    {
        [Header("Vision Settings")]
        public float viewRadius = 15f;
        [Range(0, 360)] public float viewAngle = 120f;
        public LayerMask targetMask;
        public LayerMask obstacleMask;

        [Header("Detection Points")]
        [Tooltip("Height offset for eye position")]
        public float eyeHeight = 1.5f;

        [Tooltip("Check multiple points on target for better detection")]
        public bool useMultipleCheckPoints = true;

        [Header("Chase Mode")]
        [Tooltip("Wider angle when actively chasing - AI is more alert")]
        public float chaseViewAngle = 180f;
        private bool isInChaseMode;

        // Cached values for performance
        private float sqrViewRadius;
        private float halfViewAngle;
        private float halfChaseViewAngle;

        private void Awake()
        {
            sqrViewRadius = viewRadius * viewRadius;
            halfViewAngle = viewAngle / 2f;
            halfChaseViewAngle = chaseViewAngle / 2f;
        }

        private void OnValidate()
        {
            sqrViewRadius = viewRadius * viewRadius;
            halfViewAngle = viewAngle / 2f;
            halfChaseViewAngle = chaseViewAngle / 2f;
        }

        /// <summary>
        /// Enable chase mode for wider field of view during pursuit
        /// </summary>
        public void SetChaseMode(bool chasing)
        {
            isInChaseMode = chasing;
        }

        public bool CanSeeTarget(Transform target)
        {
            if (target == null) return false;

            Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
            Vector3 targetPos = target.position + Vector3.up * 1f; // Target center mass

            // OPTIMIZATION: Check distance first (cheapest check)
            Vector3 toTarget = targetPos - eyePos;
            float sqrDistance = toTarget.sqrMagnitude;

            if (sqrDistance > sqrViewRadius)
                return false;

            // Check angle (moderately cheap) - use wider angle during chase
            Vector3 dirToTarget = toTarget.normalized;
            float angle = Vector3.Angle(transform.forward, dirToTarget);
            float currentHalfAngle = isInChaseMode ? halfChaseViewAngle : halfViewAngle;

            if (angle > currentHalfAngle)
                return false;

            float distance = Mathf.Sqrt(sqrDistance);

            // Raycast check (expensive - done last)
            if (useMultipleCheckPoints)
            {
                // Check center, head, and feet
                Vector3[] checkPoints = new Vector3[]
                {
                    target.position + Vector3.up * 1f,   // Center
                    target.position + Vector3.up * 1.7f, // Head
                    target.position + Vector3.up * 0.3f  // Feet
                };

                foreach (Vector3 point in checkPoints)
                {
                    Vector3 dir = (point - eyePos).normalized;
                    float dist = Vector3.Distance(eyePos, point);

                    if (!Physics.Raycast(eyePos, dir, dist, obstacleMask))
                    {
                        Debug.DrawRay(eyePos, dir * dist, Color.green);
                        return true;
                    }
                }

                Debug.DrawRay(eyePos, dirToTarget * distance, Color.red);
                return false;
            }
            else
            {
                // Single raycast
                if (!Physics.Raycast(eyePos, dirToTarget, distance, obstacleMask))
                {
                    Debug.DrawRay(eyePos, dirToTarget * distance, Color.green);
                    return true;
                }

                Debug.DrawRay(eyePos, dirToTarget * distance, Color.red);
                return false;
            }
        }

        /// <summary>
        /// Quick distance-only check (no angle or raycast) for broad-phase filtering
        /// </summary>
        public bool IsInRange(Transform target)
        {
            if (target == null) return false;
            float sqrDist = (target.position - transform.position).sqrMagnitude;
            return sqrDist <= sqrViewRadius;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, viewRadius);

            Vector3 leftBoundary = DirFromAngle(-viewAngle / 2, false);
            Vector3 rightBoundary = DirFromAngle(viewAngle / 2, false);

            Vector3 pos = transform.position + Vector3.up * eyeHeight;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(pos, pos + leftBoundary * viewRadius);
            Gizmos.DrawLine(pos, pos + rightBoundary * viewRadius);

            UnityEditor.Handles.color = new Color(1f, 0.5f, 0f, 0.2f);
            UnityEditor.Handles.DrawSolidArc(pos, Vector3.up, leftBoundary, viewAngle, viewRadius);
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
