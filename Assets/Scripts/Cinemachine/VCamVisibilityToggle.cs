using UnityEngine;
using Unity.Cinemachine;

public class VCamCullingMaskSwitcher : MonoBehaviour
{
    [System.Serializable]
    public class VCamMaskRule
    {
        public string vcamName;       // MUST match the vcam's Name exactly
        public LayerMask cullingMask; // Mask to use when that vcam is live
    }

    [SerializeField] private Camera mainCamera;
    [SerializeField] private VCamMaskRule[] rules;
    [SerializeField] private bool debugLog = false;

    private CinemachineBrain brain;
    private ICinemachineCamera lastCam;
    private LayerMask defaultMask;

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("[VCamCullingMaskSwitcher] No mainCamera assigned!");
            enabled = false;
            return;
        }

        defaultMask = mainCamera.cullingMask;

        brain = FindFirstObjectByType<CinemachineBrain>();
        if (brain == null)
        {
            Debug.LogError("[VCamCullingMaskSwitcher] No CinemachineBrain found in scene!");
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        if (brain == null) return;

        var currentCam = brain.ActiveVirtualCamera;
        if (currentCam != lastCam)
        {
            if (debugLog)
                Debug.Log($"[VCamCullingMaskSwitcher] Active vcam changed to: {(currentCam != null ? currentCam.Name : "null")}");

            ApplyMaskFor(currentCam);
            lastCam = currentCam;
        }
    }

    private void ApplyMaskFor(ICinemachineCamera cam)
    {
        if (cam == null)
        {
            mainCamera.cullingMask = defaultMask;
            return;
        }

        // Try to find a rule that matches this vcam
        foreach (var rule in rules)
        {
            if (cam.Name == rule.vcamName)
            {
                mainCamera.cullingMask = rule.cullingMask;
                if (debugLog)
                    Debug.Log($"[VCamCullingMaskSwitcher] Applied mask '{rule.cullingMask.value}' for vcam '{rule.vcamName}'");
                return;
            }
        }

        // No rule for this vcam → fallback to default
        mainCamera.cullingMask = defaultMask;
        if (debugLog)
            Debug.Log($"[VCamCullingMaskSwitcher] No rule for '{cam.Name}', using default mask.");
    }
}
