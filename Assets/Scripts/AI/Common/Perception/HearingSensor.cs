using UnityEngine;

namespace AI.Common.Perception
{
    public class HearingSensor : MonoBehaviour
    {
        [Header("Hearing Settings")]
        public float hearingRadius = 8f;
        public LayerMask soundMask;

        public bool CanHearSound(Vector3 soundPosition)
        {
            float distance = Vector3.Distance(transform.position, soundPosition);
            return distance <= hearingRadius;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, hearingRadius);
        }
#endif
    }
}
