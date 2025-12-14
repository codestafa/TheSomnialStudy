using UnityEngine;

namespace AI.Common.Perception
{
    public class HearingSensor : MonoBehaviour
    {
        [Header("Hearing Settings")]
        [Tooltip("Maximum distance AI can hear sounds from")]
        public float hearingRadius = 18f;

        [Tooltip("Multiplier for player noise - lower values make AI less sensitive")]
        [Range(0.5f, 2f)]
        public float hearingSensitivity = 1.2f;

        [Tooltip("Layer mask for obstacles that block sound")]
        public LayerMask obstacleMask;

        [Header("Sound Occlusion")]
        [Tooltip("Whether walls reduce sound")]
        public bool useSoundOcclusion = true;

        [Tooltip("How much walls reduce hearing range (0-1)")]
        [Range(0f, 1f)]
        public float occlusionReduction = 0.5f;

        [Header("Proximity Detection")]
        [Tooltip("AI can sense presence within this range regardless of noise")]
        public float proximityDetectionRadius = 4f;

        /// <summary>
        /// Check if AI can hear the player based on their noise level
        /// </summary>
        public bool CanHearSound(Vector3 soundPosition)
        {
            float distance = Vector3.Distance(transform.position, soundPosition);

            // Proximity detection - AI can sense very close targets regardless of noise
            // (footsteps, breathing, body heat, etc.)
            if (distance <= proximityDetectionRadius)
            {
                // Only blocked by walls
                if (useSoundOcclusion && obstacleMask != 0)
                {
                    Vector3 dirToSound = (soundPosition - transform.position).normalized;
                    if (Physics.Raycast(transform.position, dirToSound, distance, obstacleMask))
                        return false;
                }
                return true;
            }

            // Check if PlayerNoise system exists for sound-based detection
            if (PlayerNoise.Instance == null || !PlayerNoise.Instance.IsMakingNoise)
                return false;

            float effectiveNoiseRadius = PlayerNoise.Instance.CurrentNoiseRadius * hearingSensitivity;

            // Check if player's noise reaches this AI
            if (distance > effectiveNoiseRadius)
                return false;

            // Check if player's noise is within AI's hearing range
            if (distance > hearingRadius)
                return false;

            // Sound occlusion check
            if (useSoundOcclusion && obstacleMask != 0)
            {
                Vector3 dirToSound = (soundPosition - transform.position).normalized;
                if (Physics.Raycast(transform.position, dirToSound, distance, obstacleMask))
                {
                    // Wall between AI and player - reduce effective hearing
                    effectiveNoiseRadius *= (1f - occlusionReduction);
                    return distance <= effectiveNoiseRadius;
                }
            }

            return true;
        }

        /// <summary>
        /// Check if AI can hear a specific noise event (not player-based)
        /// </summary>
        public bool CanHearNoiseAt(Vector3 noisePosition, float noiseRadius)
        {
            float distance = Vector3.Distance(transform.position, noisePosition);
            float effectiveRadius = noiseRadius * hearingSensitivity;

            if (distance > effectiveRadius || distance > hearingRadius)
                return false;

            // Sound occlusion check
            if (useSoundOcclusion && obstacleMask != 0)
            {
                Vector3 dirToSound = (noisePosition - transform.position).normalized;
                if (Physics.Raycast(transform.position, dirToSound, distance, obstacleMask))
                {
                    effectiveRadius *= (1f - occlusionReduction);
                    return distance <= effectiveRadius;
                }
            }

            return true;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw hearing radius
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, hearingRadius);

            UnityEditor.Handles.color = new Color(0f, 1f, 1f, 0.1f);
            UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.up, hearingRadius);
        }
#endif
    }
}
