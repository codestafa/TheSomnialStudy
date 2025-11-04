using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NoiseGenerator))]
public class NoiseGenEditor : Editor
{
    NoiseGenerator noise;
    Editor noiseSettingsEditor;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);

        if (GUILayout.Button("Update"))
        {
            noise.ManualUpdate();
            EditorApplication.QueuePlayerLoopUpdate();
        }

        if (GUILayout.Button("Save"))
            Save();

        if (GUILayout.Button("Load"))
            Load();

        if (noise.ActiveSettings != null)
            DrawSettingsEditor(noise.ActiveSettings, ref noise.showSettingsEditor, ref noiseSettingsEditor);
    }

    void Save()
    {
        var saver = FindObjectOfType<Save3D>();
        if (saver == null)
        {
            Debug.LogWarning("No Save3D component found in scene.");
            return;
        }

        saver.Save(noise.shapeTexture, NoiseGenerator.shapeNoiseName);
        saver.Save(noise.detailTexture, NoiseGenerator.detailNoiseName);
    }

    void Load()
    {
        if (noise.shapeTexture == null || noise.detailTexture == null)
        {
            Debug.LogWarning("Textures not initialized; skipping Load.");
            return;
        }

        noise.Load(NoiseGenerator.shapeNoiseName, noise.shapeTexture);
        noise.Load(NoiseGenerator.detailNoiseName, noise.detailTexture);
        EditorApplication.QueuePlayerLoopUpdate();
    }

    void DrawSettingsEditor(Object settings, ref bool foldout, ref Editor editor)
    {
        if (settings == null)
            return;

        foldout = EditorGUILayout.InspectorTitlebar(foldout, settings);
        using (var check = new EditorGUI.ChangeCheckScope())
        {
            if (foldout)
            {
                CreateCachedEditor(settings, null, ref editor);
                editor.OnInspectorGUI();
            }

            if (check.changed)
                noise.ActiveNoiseSettingsChanged();
        }
    }

    void OnEnable()
    {
        noise = (NoiseGenerator)target;
    }
}
