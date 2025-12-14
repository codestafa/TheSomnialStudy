#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor tool to fix reflection probes that are set to "Every Frame" refresh mode.
/// This causes massive performance issues, especially on scene load.
/// </summary>
public class FixReflectionProbes
{
    // Refresh mode values (from Unity internals)
    // 0 = OnAwake, 1 = ViaScripting, 2 = EveryFrame
    private const int REFRESH_ON_AWAKE = 0;
    private const int REFRESH_EVERY_FRAME = 2;

    [MenuItem("Tools/Fix Reflection Probes (Every Frame to On Awake)")]
    public static void FixAllReflectionProbes()
    {
        ReflectionProbe[] probes = Object.FindObjectsOfType<ReflectionProbe>();
        int fixedCount = 0;

        foreach (var probe in probes)
        {
            SerializedObject so = new SerializedObject(probe);
            SerializedProperty refreshModeProp = so.FindProperty("m_RefreshMode");

            if (refreshModeProp != null && refreshModeProp.intValue == REFRESH_EVERY_FRAME)
            {
                Undo.RecordObject(probe, "Fix Reflection Probe");
                refreshModeProp.intValue = REFRESH_ON_AWAKE;
                so.ApplyModifiedProperties();
                fixedCount++;
                Debug.Log("Fixed: " + probe.gameObject.name + " - Changed to OnAwake", probe.gameObject);
            }
        }

        if (fixedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("Fixed " + fixedCount + " reflection probes! Remember to save the scene (Ctrl+S).");
        }
        else
        {
            Debug.Log("No reflection probes with 'Every Frame' refresh mode found in this scene.");
        }
    }

    [MenuItem("Tools/List All Reflection Probes")]
    public static void ListAllReflectionProbes()
    {
        ReflectionProbe[] probes = Object.FindObjectsOfType<ReflectionProbe>();

        Debug.Log("=== Found " + probes.Length + " Reflection Probes ===");

        int everyFrameCount = 0;
        foreach (var probe in probes)
        {
            SerializedObject so = new SerializedObject(probe);
            SerializedProperty refreshModeProp = so.FindProperty("m_RefreshMode");
            int mode = refreshModeProp != null ? refreshModeProp.intValue : -1;

            string modeName = mode == 0 ? "OnAwake" : mode == 1 ? "ViaScripting" : mode == 2 ? "EveryFrame" : "Unknown";
            string warning = mode == REFRESH_EVERY_FRAME ? " [EXPENSIVE!]" : "";

            Debug.Log(probe.gameObject.name + ": " + modeName + warning, probe.gameObject);

            if (mode == REFRESH_EVERY_FRAME)
                everyFrameCount++;
        }

        if (everyFrameCount > 0)
        {
            Debug.LogWarning(everyFrameCount + " probes are set to 'Every Frame' - this causes performance issues! Run 'Tools > Fix Reflection Probes' to fix.");
        }
    }
}
#endif
