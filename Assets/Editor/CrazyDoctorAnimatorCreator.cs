using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public class CrazyDoctorAnimatorCreator
{
    [MenuItem("Tools/Create CrazyDoctor Animator (Idle Only)")]
    static void CreateAnimatorController()
    {
        // Folder where the controller will be stored
        string dir = "Assets/Assets/Zectorlab/CrazyDoctor/Art/Animations";
        string path = dir + "/AC_CrazyDoctor.controller";

        // Ensure folder exists
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
            Debug.Log("📂 Created missing folder: " + dir);
        }

        // Create controller
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        var rootStateMachine = controller.layers[0].stateMachine;

        // Try to load the Idle animation clip
        string idleClipPath = "Assets/Assets/Zectorlab/CrazyDoctor/Art/Animations/AS_CrazyDoctor_Idle.anim";
        AnimationClip idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(idleClipPath);

        if (idleClip == null)
        {
            Debug.LogWarning("⚠️ Idle clip not found at: " + idleClipPath);
        }

        // --- Create Idle state and assign motion ---
        var idleState = rootStateMachine.AddState("Idle");
        idleState.motion = idleClip;

        // Set as default
        rootStateMachine.defaultState = idleState;

        Debug.Log("✅ CrazyDoctor Animator Controller (Idle Only) created at: " + path);

        // --- Assign controller to SK_CrazyDoctor in the scene ---
        GameObject crazyDoctor = GameObject.Find("SK_CrazyDoctor");

        if (crazyDoctor != null)
        {
            Animator animator = crazyDoctor.GetComponent<Animator>();
            if (animator == null)
            {
                animator = crazyDoctor.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;

            Debug.Log("🎭 Assigned AC_CrazyDoctor.controller (Idle Only) to SK_CrazyDoctor in the scene.");
        }
        else
        {
            Debug.LogWarning("⚠️ Could not find SK_CrazyDoctor in the scene. Make sure it’s named exactly 'SK_CrazyDoctor'.");
        }
    }
}
