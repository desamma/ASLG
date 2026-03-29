using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Editor utility to automatically add animation parameters from StateAnimator attributes to Animator Controllers
/// </summary>
public class AnimatorParameterSetup : EditorWindow
{
    private AnimatorController targetAnimator;
    private MonoScript stateScript; // optional: scan only this C# script file
    private Vector2 scrollPosition;
    private List<StateParameterInfo> foundParameters = new List<StateParameterInfo>();

    private class StateParameterInfo
    {
        public string enumTypeName;
        public string enumValueName;
        public string parameterName;
        public bool exists;
        public bool selected; // whether the missing parameter is selected to be added
    }

    [MenuItem("Tools/Setup Animator Parameters")]
    public static void ShowWindow()
    {
        GetWindow<AnimatorParameterSetup>("Animator Parameter Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Animator Parameter Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "This tool scans all enums with [StateAnimator] attributes and adds missing parameters to the selected Animator Controller.\n\nYou can select a C# script to scan only that file.",
            MessageType.Info);

        EditorGUILayout.Space();

        // Animator Controller selection
        targetAnimator = (AnimatorController)EditorGUILayout.ObjectField(
            "Target Animator",
            targetAnimator,
            typeof(AnimatorController),
            false);

        // Optional state script selection
        stateScript = (MonoScript)EditorGUILayout.ObjectField(
            "State Script",
            stateScript,
            typeof(MonoScript),
            false);

        EditorGUILayout.Space();

        // Scan button
        if (GUILayout.Button("Scan for Parameters", GUILayout.Height(30)))
        {
            ScanForParameters();
        }

        EditorGUILayout.Space();

        // Display found parameters
        if (foundParameters.Count > 0)
        {
            EditorGUILayout.LabelField($"Found {foundParameters.Count} parameters:", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (var param in foundParameters)
            {
                EditorGUILayout.BeginHorizontal("box");

                // Status icon
                if (param.exists)
                {
                    GUILayout.Label("\u2713", GUILayout.Width(20));
                    GUI.color = Color.green;
                }
                else
                {
                    GUILayout.Label("\u26A0", GUILayout.Width(20));
                    GUI.color = Color.yellow;
                }

                // Selection toggle (disabled for existing parameters)
                EditorGUI.BeginDisabledGroup(param.exists);
                param.selected = EditorGUILayout.Toggle(param.selected, GUILayout.Width(20));
                EditorGUI.EndDisabledGroup();

                // Parameter info
                EditorGUILayout.LabelField(param.parameterName, GUILayout.Width(200));
                EditorGUILayout.LabelField($"({param.enumTypeName}.{param.enumValueName})", EditorStyles.miniLabel);

                GUI.color = Color.white;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            int missingCount = foundParameters.Count(p => !p.exists);
            int selectedCount = foundParameters.Count(p => !p.exists && p.selected);

            if (missingCount > 0)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Select All", GUILayout.Height(24)))
                {
                    foreach (var p in foundParameters.Where(p => !p.exists)) p.selected = true;
                }
                if (GUILayout.Button("Deselect All", GUILayout.Height(24)))
                {
                    foreach (var p in foundParameters.Where(p => !p.exists)) p.selected = false;
                }
                EditorGUILayout.EndHorizontal();

                if (selectedCount > 0)
                {
                    if (GUILayout.Button($"Add {selectedCount} Selected Parameters", GUILayout.Height(30)))
                    {
                        AddMissingParameters();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("No missing parameters selected.", MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("All parameters already exist in the Animator Controller!", MessageType.Info);
            }
        }
    }

    private void ScanForParameters()
    {
        foundParameters.Clear();

        if (targetAnimator == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select an Animator Controller first!", "OK");
            return;
        }

        if (stateScript != null)
        {
            // Scan only the selected script file
            string path = AssetDatabase.GetAssetPath(stateScript);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                EditorUtility.DisplayDialog("Error", "Selected script file not found on disk.", "OK");
                return;
            }

            ScanScriptFile(path);
        }
        else
        {
            // Get all types in the current assembly
            Assembly assembly = Assembly.GetAssembly(typeof(StateAnimatorAttribute));
            Type[] types = assembly.GetTypes();

            // Find all enums
            foreach (Type type in types)
            {
                if (type.IsEnum)
                {
                    // Check each enum value for StateAnimator attribute
                    foreach (var enumValue in Enum.GetValues(type))
                    {
                        FieldInfo fieldInfo = type.GetField(enumValue.ToString());
                        StateAnimatorAttribute attribute = (StateAnimatorAttribute)Attribute.GetCustomAttribute(
                            fieldInfo, typeof(StateAnimatorAttribute));

                        if (attribute != null)
                        {
                            string paramName = attribute.AnimatorParameterName;
                            bool exists = HasParameter(targetAnimator, paramName);

                            foundParameters.Add(new StateParameterInfo
                            {
                                enumTypeName = type.Name,
                                enumValueName = enumValue.ToString(),
                                parameterName = paramName,
                                exists = exists,
                                selected = !exists // default select missing parameters
                            });
                        }
                    }
                }
            }
        }

        foundParameters = foundParameters.OrderBy(p => p.parameterName).ToList();

        Debug.Log($"Scan complete! Found {foundParameters.Count} parameters ({foundParameters.Count(p => !p.exists)} missing)");
    }

    private void ScanScriptFile(string path)
    {
        string code = File.ReadAllText(path);

        // Find enums in the file
        var enumRegex = new Regex(@"enum\s+(\w+)\s*\{([^}]*)\}", RegexOptions.Singleline);
        var enumMatches = enumRegex.Matches(code);

        foreach (Match enumMatch in enumMatches)
        {
            string enumName = enumMatch.Groups[1].Value;
            string enumBody = enumMatch.Groups[2].Value;

            // Match enum members with optional attribute blocks preceding them
            var memberRegex = new Regex(@"(\[([^\]]*StateAnimator[^\]]*)\]\s*)*(\w+)\s*(?:=[^,}]*)?\s*(?:,|$)", RegexOptions.Multiline);
            var memberMatches = memberRegex.Matches(enumBody);

            foreach (Match memberMatch in memberMatches)
            {
                string attrBlock = memberMatch.Groups[2].Value; // contents of the attribute block that contains StateAnimator
                string memberName = memberMatch.Groups[3].Value;

                if (string.IsNullOrEmpty(attrBlock))
                    continue;

                // Find first string literal inside the attribute block (e.g. "paramName")
                var strLit = Regex.Match(attrBlock, "\"([^\"]*)\"");
                if (!strLit.Success) continue;

                string paramName = strLit.Groups[1].Value;
                bool exists = HasParameter(targetAnimator, paramName);

                foundParameters.Add(new StateParameterInfo
                {
                    enumTypeName = enumName,
                    enumValueName = memberName,
                    parameterName = paramName,
                    exists = exists,
                    selected = !exists
                });
            }
        }
    }

    private void AddMissingParameters()
    {
        if (targetAnimator == null) return;

        int addedCount = 0;

        foreach (var param in foundParameters.Where(p => !p.exists && p.selected))
        {
            targetAnimator.AddParameter(param.parameterName, AnimatorControllerParameterType.Bool);
            addedCount++;
        }

        // Mark animator as dirty to save changes
        EditorUtility.SetDirty(targetAnimator);
        AssetDatabase.SaveAssets();

        Debug.Log($"Successfully added {addedCount} parameters to {targetAnimator.name}!");

        // Rescan to update status
        ScanForParameters();

        EditorUtility.DisplayDialog("Success", $"Added {addedCount} parameters to the Animator Controller!", "OK");
    }

    private bool HasParameter(AnimatorController animator, string parameterName)
    {
        foreach (var param in animator.parameters)
        {
            if (param.name == parameterName)
                return true;
        }
        return false;
    }
}
