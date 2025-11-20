using UnityEngine;

namespace SceneManagement
{
    /// <summary>
    /// Triggers scene loading when player/object enters a proximity zone
    /// Attach to a GameObject with a Collider (set as Trigger)
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ProximitySceneTrigger : MonoBehaviour
    {
        [Header("Trigger Settings")]
        [SerializeField] private string targetTag = "Player";
        [SerializeField] private bool triggerOnce = true;
        [SerializeField] private float triggerDelay = 0f;

        [Header("Scene Loader Reference")]
        [SerializeField] private SceneLoader sceneLoader;

        [Header("Optional: Direct Scene Settings")]
        [SerializeField] private bool useDirectLoading = false;
        [SerializeField] private string sceneName;

        [Header("Visual Feedback")]
        [SerializeField] private bool showGizmo = true;
        [SerializeField] private Color gizmoColor = new Color(0, 1, 0, 0.3f);

        private bool hasTriggered = false;
        private Collider triggerCollider;

        private void Awake()
        {
            triggerCollider = GetComponent<Collider>();
            
            if (!triggerCollider.isTrigger)
            {
                Debug.LogWarning($"Collider on {gameObject.name} is not set as Trigger! Setting it now.");
                triggerCollider.isTrigger = true;
            }

            // If no scene loader is assigned, try to find one on this GameObject
            if (sceneLoader == null)
            {
                sceneLoader = GetComponent<SceneLoader>();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Check if already triggered
            if (triggerOnce && hasTriggered)
                return;

            // Check if correct tag
            if (!other.CompareTag(targetTag))
                return;

            hasTriggered = true;

            // Load scene
            if (triggerDelay > 0)
            {
                Invoke(nameof(LoadSceneDelayed), triggerDelay);
            }
            else
            {
                LoadSceneDelayed();
            }
        }

        private void LoadSceneDelayed()
        {
            if (useDirectLoading && !string.IsNullOrEmpty(sceneName))
            {
                SceneLoader.LoadSceneStatic(sceneName);
            }
            else if (sceneLoader != null)
            {
                sceneLoader.LoadScene();
            }
            else
            {
                Debug.LogError($"No SceneLoader assigned and useDirectLoading is false on {gameObject.name}!");
            }
        }

        /// <summary>
        /// Reset the trigger so it can be activated again
        /// </summary>
        public void ResetTrigger()
        {
            hasTriggered = false;
        }

        private void OnDrawGizmos()
        {
            if (!showGizmo) return;

            Collider col = GetComponent<Collider>();
            if (col == null) return;

            Gizmos.color = gizmoColor;

            if (col is BoxCollider boxCollider)
            {
                Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.matrix = rotationMatrix;
                Gizmos.DrawCube(boxCollider.center, boxCollider.size);
            }
            else if (col is SphereCollider sphereCollider)
            {
                Gizmos.DrawSphere(transform.position + sphereCollider.center, sphereCollider.radius * transform.lossyScale.x);
            }
            else if (col is CapsuleCollider capsuleCollider)
            {
                // Simplified capsule visualization
                Gizmos.DrawSphere(transform.position + capsuleCollider.center, capsuleCollider.radius * transform.lossyScale.x);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!showGizmo) return;

            Collider col = GetComponent<Collider>();
            if (col == null) return;

            Gizmos.color = Color.green;

            if (col is BoxCollider boxCollider)
            {
                Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.matrix = rotationMatrix;
                Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
            }
            else if (col is SphereCollider sphereCollider)
            {
                Gizmos.DrawWireSphere(transform.position + sphereCollider.center, sphereCollider.radius * transform.lossyScale.x);
            }
        }
    }
}
