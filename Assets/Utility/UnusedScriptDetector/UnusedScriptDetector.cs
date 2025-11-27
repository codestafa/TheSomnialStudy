using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

/// <summary>
/// Unity Editor tool to detect unused C# scripts in the project.
/// Helps identify technical debt by finding scripts that aren't referenced anywhere.
/// Includes code dependency analysis to detect inheritance and code references.
/// </summary>
public class UnusedScriptDetector : EditorWindow
{
    private Vector2 scrollPosition;
    private List<MonoScript> unusedScripts = new List<MonoScript>();
    private List<MonoScript> usedScripts = new List<MonoScript>();
    private bool isScanning = false;
    private bool showUsedScripts = false;
    private string searchFilter = "";
    private Dictionary<string, HashSet<string>> scriptDependencies = new Dictionary<string, HashSet<string>>();

    [MenuItem("Tools/Unused Script Detector")]
    public static void ShowWindow()
    {
        GetWindow<UnusedScriptDetector>("Unused Script Detector");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Unused Script Detector", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This tool scans your project for C# scripts that aren't used in:\n" +
            "• Scenes (in build settings and in project)\n" +
            "• Prefabs\n" +
            "• ScriptableObjects\n" +
            "• Other assets\n" +
            "• Other scripts (inheritance, references, etc.)",
            MessageType.Info
        );

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Scan Project for Unused Scripts", GUILayout.Height(30)))
        {
            ScanForUnusedScripts();
        }

        if (isScanning)
        {
            EditorGUILayout.LabelField("Scanning... Please wait.");
            return;
        }

        if (unusedScripts.Count > 0 || usedScripts.Count > 0)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Toggle(!showUsedScripts, "Unused Scripts", "Button"))
                showUsedScripts = false;
            
            if (GUILayout.Toggle(showUsedScripts, "Used Scripts", "Button"))
                showUsedScripts = true;
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            searchFilter = EditorGUILayout.TextField("Search:", searchFilter);

            EditorGUILayout.Space(10);

            var scriptsToShow = showUsedScripts ? usedScripts : unusedScripts;
            var filteredScripts = string.IsNullOrEmpty(searchFilter)
                ? scriptsToShow
                : scriptsToShow.Where(s => s.name.ToLower().Contains(searchFilter.ToLower())).ToList();

            EditorGUILayout.LabelField(
                $"{(showUsedScripts ? "Used" : "Unused")} Scripts: {filteredScripts.Count} / {scriptsToShow.Count}",
                EditorStyles.boldLabel
            );

            if (!showUsedScripts && unusedScripts.Count > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Select All Unused", GUILayout.Height(25)))
                {
                    Selection.objects = filteredScripts.Cast<Object>().ToArray();
                }
                
                if (GUILayout.Button("Delete All Unused (BE CAREFUL!)", GUILayout.Height(25)))
                {
                    if (EditorUtility.DisplayDialog(
                        "Delete Unused Scripts",
                        $"Are you sure you want to delete {filteredScripts.Count} unused scripts? This cannot be undone!",
                        "Delete",
                        "Cancel"))
                    {
                        DeleteScripts(filteredScripts);
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(5);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (var script in filteredScripts)
            {
                if (script == null) continue;

                EditorGUILayout.BeginHorizontal();
                
                EditorGUILayout.ObjectField(script, typeof(MonoScript), false);
                
                if (GUILayout.Button("Ping", GUILayout.Width(50)))
                {
                    EditorGUIUtility.PingObject(script);
                }
                
                if (!showUsedScripts && GUILayout.Button("Delete", GUILayout.Width(60)))
                {
                    if (EditorUtility.DisplayDialog(
                        "Delete Script",
                        $"Are you sure you want to delete {script.name}?",
                        "Delete",
                        "Cancel"))
                    {
                        DeleteScripts(new List<MonoScript> { script });
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void ScanForUnusedScripts()
    {
        isScanning = true;
        unusedScripts.Clear();
        usedScripts.Clear();
        scriptDependencies.Clear();

        try
        {
            // Get all MonoScripts in the project
            var allScripts = AssetDatabase.FindAssets("t:MonoScript")
                .Select(guid => AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(script => script != null && script.GetClass() != null)
                .ToList();

            Debug.Log($"Found {allScripts.Count} scripts in project. Analyzing dependencies...");

            // Build script name to path mapping
            var scriptNameToPath = new Dictionary<string, string>();
            foreach (var script in allScripts)
            {
                var scriptPath = AssetDatabase.GetAssetPath(script);
                var scriptClass = script.GetClass();
                if (scriptClass != null)
                {
                    scriptNameToPath[scriptClass.Name] = scriptPath;
                }
            }

            // Step 1: Analyze code dependencies for all scripts
            EditorUtility.DisplayProgressBar("Analyzing Scripts", "Parsing code dependencies...", 0f);
            
            int processedScripts = 0;
            foreach (var script in allScripts)
            {
                processedScripts++;
                if (processedScripts % 50 == 0)
                {
                    EditorUtility.DisplayProgressBar(
                        "Analyzing Scripts",
                        $"Analyzing code dependencies {processedScripts}/{allScripts.Count}",
                        (float)processedScripts / allScripts.Count * 0.3f
                    );
                }

                var scriptPath = AssetDatabase.GetAssetPath(script);
                var dependencies = AnalyzeScriptDependencies(scriptPath, scriptNameToPath);
                scriptDependencies[scriptPath] = dependencies;
            }

            // Step 2: Get all assets that could reference scripts
            var allAssets = AssetDatabase.GetAllAssetPaths()
                .Where(path => 
                    path.StartsWith("Assets/") && 
                    !path.EndsWith(".cs") &&
                    (path.EndsWith(".prefab") || 
                     path.EndsWith(".unity") || 
                     path.EndsWith(".asset") ||
                     path.EndsWith(".mat") ||
                     path.EndsWith(".controller") ||
                     path.EndsWith(".overrideController")))
                .ToList();

            // Build a set of directly used script paths
            var directlyUsedScriptPaths = new HashSet<string>();

            int processedAssets = 0;
            foreach (var assetPath in allAssets)
            {
                processedAssets++;
                if (processedAssets % 100 == 0)
                {
                    if (EditorUtility.DisplayCancelableProgressBar(
                        "Scanning Assets",
                        $"Processing {processedAssets}/{allAssets.Count}: {Path.GetFileName(assetPath)}",
                        0.3f + ((float)processedAssets / allAssets.Count) * 0.4f))
                    {
                        EditorUtility.ClearProgressBar();
                        isScanning = false;
                        return;
                    }
                }

                // Get all dependencies of this asset
                var dependencies = AssetDatabase.GetDependencies(assetPath, false);
                foreach (var dependency in dependencies)
                {
                    if (dependency.EndsWith(".cs"))
                    {
                        directlyUsedScriptPaths.Add(dependency);
                    }
                }
            }

            // Step 3: Expand used scripts to include their dependencies
            // Only scripts referenced by directly-used scripts become used
            EditorUtility.DisplayProgressBar("Analyzing Usage", "Expanding code dependencies...", 0.7f);
            
            var allUsedScriptPaths = new HashSet<string>(directlyUsedScriptPaths);
            var scriptsToProcess = new Queue<string>(directlyUsedScriptPaths);

            while (scriptsToProcess.Count > 0)
            {
                var currentScript = scriptsToProcess.Dequeue();
                
                if (scriptDependencies.ContainsKey(currentScript))
                {
                    foreach (var dependency in scriptDependencies[currentScript])
                    {
                        // Add this dependency as used because it's referenced by a used script
                        if (!allUsedScriptPaths.Contains(dependency))
                        {
                            allUsedScriptPaths.Add(dependency);
                            // Also process this dependency's dependencies recursively
                            scriptsToProcess.Enqueue(dependency);
                        }
                    }
                }
            }

            EditorUtility.ClearProgressBar();

            // Step 4: Categorize scripts
            foreach (var script in allScripts)
            {
                var scriptPath = AssetDatabase.GetAssetPath(script);

                // Skip Editor scripts (they're not typically attached to GameObjects)
                if (scriptPath.Contains("/Editor/") || scriptPath.Contains("\\Editor\\"))
                {
                    continue;
                }

                // Check if this script is an Editor class
                var scriptClass = script.GetClass();
                if (scriptClass != null && 
                    (scriptClass.IsSubclassOf(typeof(Editor)) || 
                     scriptClass.IsSubclassOf(typeof(EditorWindow)) ||
                     scriptClass.IsSubclassOf(typeof(PropertyDrawer))))
                {
                    continue;
                }

                if (allUsedScriptPaths.Contains(scriptPath))
                {
                    usedScripts.Add(script);
                }
                else
                {
                    unusedScripts.Add(script);
                }
            }

            Debug.Log($"Scan complete! Found {unusedScripts.Count} unused scripts and {usedScripts.Count} used scripts (including code dependencies).");
        }
        finally
        {
            isScanning = false;
            EditorUtility.ClearProgressBar();
            Repaint();
        }
    }

    private HashSet<string> AnalyzeScriptDependencies(string scriptPath, Dictionary<string, string> scriptNameToPath)
    {
        var dependencies = new HashSet<string>();

        try
        {
            var content = File.ReadAllText(scriptPath);

            // Remove comments to avoid false positives
            content = Regex.Replace(content, @"//.*$", "", RegexOptions.Multiline);
            content = Regex.Replace(content, @"/\*.*?\*/", "", RegexOptions.Singleline);

            // Find all class/interface names referenced in the code
            // This includes: inheritance, field types, method parameters, generic types, etc.
            
            // Pattern for class inheritance
            var inheritanceMatches = Regex.Matches(content, @"class\s+\w+\s*:\s*([\w\s,<>]+)");
            foreach (Match match in inheritanceMatches)
            {
                var baseTypes = match.Groups[1].Value.Split(',');
                foreach (var baseType in baseTypes)
                {
                    var cleanType = CleanTypeName(baseType);
                    if (scriptNameToPath.ContainsKey(cleanType))
                    {
                        dependencies.Add(scriptNameToPath[cleanType]);
                    }
                }
            }

            // Pattern for field declarations, method parameters, and local variables
            var typeReferencePattern = @"\b([A-Z]\w+)(?:<[^>]+>)?\s+\w+\s*[=;,\)]";
            var typeMatches = Regex.Matches(content, typeReferencePattern);
            foreach (Match match in typeMatches)
            {
                var typeName = CleanTypeName(match.Groups[1].Value);
                if (scriptNameToPath.ContainsKey(typeName))
                {
                    dependencies.Add(scriptNameToPath[typeName]);
                }
            }

            // Pattern for generic type parameters
            var genericPattern = @"<([A-Z]\w+)>";
            var genericMatches = Regex.Matches(content, genericPattern);
            foreach (Match match in genericMatches)
            {
                var typeName = CleanTypeName(match.Groups[1].Value);
                if (scriptNameToPath.ContainsKey(typeName))
                {
                    dependencies.Add(scriptNameToPath[typeName]);
                }
            }

            // Pattern for typeof, GetComponent, AddComponent, and similar Unity patterns
            var unityPatterns = new[]
            {
                @"typeof\(([A-Z]\w+)\)",
                @"GetComponent(?:InChildren|InParent)?<([A-Z]\w+)>",
                @"AddComponent<([A-Z]\w+)>",
                @"RequireComponent\(typeof\(([A-Z]\w+)\)\)",
                @"FindObjectOfType<([A-Z]\w+)>",
                @"ScriptableObject\.CreateInstance<([A-Z]\w+)>"
            };

            foreach (var pattern in unityPatterns)
            {
                var matches = Regex.Matches(content, pattern);
                foreach (Match match in matches)
                {
                    var typeName = CleanTypeName(match.Groups[1].Value);
                    if (scriptNameToPath.ContainsKey(typeName))
                    {
                        dependencies.Add(scriptNameToPath[typeName]);
                    }
                }
            }

            // Pattern for casting
            var castPattern = @"\(([A-Z]\w+)\)";
            var castMatches = Regex.Matches(content, castPattern);
            foreach (Match match in castMatches)
            {
                var typeName = CleanTypeName(match.Groups[1].Value);
                if (scriptNameToPath.ContainsKey(typeName))
                {
                    dependencies.Add(scriptNameToPath[typeName]);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Error analyzing dependencies for {scriptPath}: {ex.Message}");
        }

        return dependencies;
    }

    private string CleanTypeName(string typeName)
    {
        // Remove whitespace and generic parameters
        typeName = typeName.Trim();
        var genericIndex = typeName.IndexOf('<');
        if (genericIndex > 0)
        {
            typeName = typeName.Substring(0, genericIndex);
        }
        return typeName;
    }

    private void DeleteScripts(List<MonoScript> scripts)
    {
        int deletedCount = 0;
        
        foreach (var script in scripts)
        {
            if (script == null) continue;
            
            var path = AssetDatabase.GetAssetPath(script);
            if (AssetDatabase.DeleteAsset(path))
            {
                deletedCount++;
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"Deleted {deletedCount} scripts.");
        
        // Re-scan after deletion
        ScanForUnusedScripts();
    }
}
