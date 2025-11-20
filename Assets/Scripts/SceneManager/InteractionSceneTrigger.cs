using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SceneManagement
{
    /// <summary>
    /// Triggers scene loading when player interacts with an object
    /// Supports both old Input Manager and new Input System
    /// </summary>
    public class InteractionSceneTrigger : MonoBehaviour
    {
        public enum InteractionButton
        {
            E,
            F,
            Q,
            R,
            T,
            Space,
            LeftMouseButton,
            RightMouseButton,
            MiddleMouseButton,
            Tab,
            LeftShift,
            LeftCtrl,
            LeftAlt
        }

        [Header("Interaction Settings")]
        [SerializeField] private string targetTag = "Player";
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private bool requireLookAt = false;
        [SerializeField] private float lookAtAngle = 45f;

        [Header("Input Settings")]
        [SerializeField] private bool useNewInputSystem = false;
        [SerializeField] private InteractionButton interactionButton = InteractionButton.E;

        [Header("Scene Loader Reference")]
        [SerializeField] private SceneLoader sceneLoader;

        [Header("Optional: Direct Scene Settings")]
        [SerializeField] private bool useDirectLoading = false;
        [SerializeField] private SceneReference sceneToLoad;

        [Header("UI Settings")]
        [SerializeField] private bool showPrompt = true;
        [SerializeField] private string promptText = "Press E to interact";
        [SerializeField] private GameObject promptUI;

        [Header("Visual Settings")]
        [SerializeField] private bool highlightOnHover = false;
        [SerializeField] private Material highlightMaterial;

        private Transform player;
        private bool playerInRange = false;
        private Material originalMaterial;
        private Renderer objectRenderer;

        private void Awake()
        {
            // If no scene loader is assigned, try to find one on this GameObject
            if (sceneLoader == null)
            {
                sceneLoader = GetComponent<SceneLoader>();
            }

            if (highlightOnHover)
            {
                objectRenderer = GetComponent<Renderer>();
                if (objectRenderer != null)
                {
                    originalMaterial = objectRenderer.material;
                }
            }

            if (promptUI != null)
            {
                promptUI.SetActive(false);
            }

            // Update prompt text with selected button
            UpdatePromptText();
        }

        private void Update()
        {
            if (!playerInRange || player == null)
                return;

            // Check if player is looking at object (if required)
            if (requireLookAt && !IsPlayerLookingAtObject())
                return;

            // Check for interaction input
            bool interactionPressed = false;

            if (useNewInputSystem)
            {
                interactionPressed = CheckNewInputSystem();
            }
            else
            {
                interactionPressed = CheckOldInputSystem();
            }

            if (interactionPressed)
            {
                Interact();
            }
        }

        private bool CheckNewInputSystem()
        {
            if (Keyboard.current == null && Mouse.current == null)
                return false;

            switch (interactionButton)
            {
                case InteractionButton.E:
                    return Keyboard.current?.eKey.wasPressedThisFrame ?? false;
                case InteractionButton.F:
                    return Keyboard.current?.fKey.wasPressedThisFrame ?? false;
                case InteractionButton.Q:
                    return Keyboard.current?.qKey.wasPressedThisFrame ?? false;
                case InteractionButton.R:
                    return Keyboard.current?.rKey.wasPressedThisFrame ?? false;
                case InteractionButton.T:
                    return Keyboard.current?.tKey.wasPressedThisFrame ?? false;
                case InteractionButton.Space:
                    return Keyboard.current?.spaceKey.wasPressedThisFrame ?? false;
                case InteractionButton.LeftMouseButton:
                    return Mouse.current?.leftButton.wasPressedThisFrame ?? false;
                case InteractionButton.RightMouseButton:
                    return Mouse.current?.rightButton.wasPressedThisFrame ?? false;
                case InteractionButton.MiddleMouseButton:
                    return Mouse.current?.middleButton.wasPressedThisFrame ?? false;
                case InteractionButton.Tab:
                    return Keyboard.current?.tabKey.wasPressedThisFrame ?? false;
                case InteractionButton.LeftShift:
                    return Keyboard.current?.leftShiftKey.wasPressedThisFrame ?? false;
                case InteractionButton.LeftCtrl:
                    return Keyboard.current?.leftCtrlKey.wasPressedThisFrame ?? false;
                case InteractionButton.LeftAlt:
                    return Keyboard.current?.leftAltKey.wasPressedThisFrame ?? false;
                default:
                    return false;
            }
        }

        private bool CheckOldInputSystem()
        {
            switch (interactionButton)
            {
                case InteractionButton.E:
                    return Input.GetKeyDown(KeyCode.E);
                case InteractionButton.F:
                    return Input.GetKeyDown(KeyCode.F);
                case InteractionButton.Q:
                    return Input.GetKeyDown(KeyCode.Q);
                case InteractionButton.R:
                    return Input.GetKeyDown(KeyCode.R);
                case InteractionButton.T:
                    return Input.GetKeyDown(KeyCode.T);
                case InteractionButton.Space:
                    return Input.GetKeyDown(KeyCode.Space);
                case InteractionButton.LeftMouseButton:
                    return Input.GetMouseButtonDown(0);
                case InteractionButton.RightMouseButton:
                    return Input.GetMouseButtonDown(1);
                case InteractionButton.MiddleMouseButton:
                    return Input.GetMouseButtonDown(2);
                case InteractionButton.Tab:
                    return Input.GetKeyDown(KeyCode.Tab);
                case InteractionButton.LeftShift:
                    return Input.GetKeyDown(KeyCode.LeftShift);
                case InteractionButton.LeftCtrl:
                    return Input.GetKeyDown(KeyCode.LeftControl);
                case InteractionButton.LeftAlt:
                    return Input.GetKeyDown(KeyCode.LeftAlt);
                default:
                    return false;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(targetTag))
            {
                player = other.transform;
                playerInRange = true;
                ShowPrompt(true);

                if (highlightOnHover && objectRenderer != null && highlightMaterial != null)
                {
                    objectRenderer.material = highlightMaterial;
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(targetTag))
            {
                player = null;
                playerInRange = false;
                ShowPrompt(false);

                if (highlightOnHover && objectRenderer != null && originalMaterial != null)
                {
                    objectRenderer.material = originalMaterial;
                }
            }
        }

        /// <summary>
        /// Manual interaction method that can be called from UnityEvents
        /// </summary>
        public void Interact()
        {
            if (useDirectLoading && sceneToLoad.ScenePath != null)
            {
                SceneLoader.LoadSceneStatic(sceneToLoad.ScenePath);
            }
            else if (sceneLoader != null)
            {
                sceneLoader.LoadScene();
            }
            else
            {
                Debug.LogError($"No SceneLoader assigned and useDirectLoading is false on {gameObject.name}!");
            }

            ShowPrompt(false);
        }

        private bool IsPlayerLookingAtObject()
        {
            if (player == null) return false;

            Vector3 directionToObject = (transform.position - player.position).normalized;
            float angle = Vector3.Angle(player.forward, directionToObject);

            return angle <= lookAtAngle;
        }

        private void ShowPrompt(bool show)
        {
            if (!showPrompt) return;

            if (promptUI != null)
            {
                promptUI.SetActive(show);
            }
        }

        private void UpdatePromptText()
        {
            string buttonName = GetButtonDisplayName();
            promptText = $"Press {buttonName} to interact";
        }

        private string GetButtonDisplayName()
        {
            switch (interactionButton)
            {
                case InteractionButton.LeftMouseButton:
                    return "Left Click";
                case InteractionButton.RightMouseButton:
                    return "Right Click";
                case InteractionButton.MiddleMouseButton:
                    return "Middle Click";
                case InteractionButton.LeftShift:
                    return "Shift";
                case InteractionButton.LeftCtrl:
                    return "Ctrl";
                case InteractionButton.LeftAlt:
                    return "Alt";
                default:
                    return interactionButton.ToString();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRange);

            if (requireLookAt)
            {
                Gizmos.color = Color.cyan;
                Vector3 forward = transform.forward * interactionRange;
                Vector3 right = Quaternion.Euler(0, lookAtAngle, 0) * forward;
                Vector3 left = Quaternion.Euler(0, -lookAtAngle, 0) * forward;

                Gizmos.DrawLine(transform.position, transform.position + right);
                Gizmos.DrawLine(transform.position, transform.position + left);
            }
        }

        private void OnValidate()
        {
            // Add sphere collider if none exists
            SphereCollider sphereCollider = GetComponent<SphereCollider>();
            if (sphereCollider == null)
            {
                sphereCollider = gameObject.AddComponent<SphereCollider>();
                sphereCollider.isTrigger = true;
                sphereCollider.radius = interactionRange;
            }

            // Update prompt text when button changes
            UpdatePromptText();
        }
    }

    /// <summary>
    /// Serializable class that allows dragging scene assets in the inspector
    /// </summary>
    [System.Serializable]
    public class SceneReference
    {
        [SerializeField] private Object sceneAsset;

#if UNITY_EDITOR
        [SerializeField] private string scenePath = "";

        public string ScenePath
        {
            get
            {
                if (sceneAsset != null)
                {
                    scenePath = AssetDatabase.GetAssetPath(sceneAsset);
                    if (scenePath.Contains(".unity"))
                    {
                        scenePath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                    }
                }
                return scenePath;
            }
        }
#else
        [SerializeField] private string scenePath = "";
        public string ScenePath => scenePath;
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// Custom property drawer for SceneReference to show scene picker in inspector
    /// </summary>
    [CustomPropertyDrawer(typeof(SceneReference))]
    public class SceneReferencePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var sceneAssetProperty = property.FindPropertyRelative("sceneAsset");
            var scenePathProperty = property.FindPropertyRelative("scenePath");

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            EditorGUI.BeginChangeCheck();
            var newSceneAsset = EditorGUI.ObjectField(position, sceneAssetProperty.objectReferenceValue, typeof(SceneAsset), false);

            if (EditorGUI.EndChangeCheck())
            {
                sceneAssetProperty.objectReferenceValue = newSceneAsset;

                if (newSceneAsset != null)
                {
                    string path = AssetDatabase.GetAssetPath(newSceneAsset);
                    scenePathProperty.stringValue = System.IO.Path.GetFileNameWithoutExtension(path);
                }
                else
                {
                    scenePathProperty.stringValue = "";
                }
            }

            EditorGUI.EndProperty();
        }
    }
#endif
}