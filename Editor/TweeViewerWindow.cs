using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using SimpleTwineDialogue;

public class TweeViewerWindow : EditorWindow
{
    private string tweeFilePath = "";
    private Dictionary<string, TweeParser.Passage> passages;
    private HashSet<string> variables;
    private Vector2 scrollPos;
    private GUIStyle boxStyle;
    private string startNode = "";
    
    [MenuItem("Window/Twine to Unity/Twee Viewer")]
    public static void ShowWindow()
    {
        GetWindow<TweeViewerWindow>("Twee Viewer");
    }
    
    private void OnGUI()
    {
        if (boxStyle == null)
        {
            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.margin = new RectOffset(10, 10, 5, 5);
            boxStyle.padding = new RectOffset(10, 10, 10, 10);
        }

        GUILayout.Space(10);
        GUILayout.Label("Twee File Interpreter", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        tweeFilePath = EditorGUILayout.TextField("Twee File Path", tweeFilePath);
        if (GUILayout.Button("Browse...", GUILayout.Width(80)))
        {
            string defaultPath = Path.Combine(Application.streamingAssetsPath, "Story");
            if (!Directory.Exists(defaultPath)) defaultPath = Application.streamingAssetsPath;
            
            string path = EditorUtility.OpenFilePanel("Select Twee File", defaultPath, "twee,txt");
            if (!string.IsNullOrEmpty(path))
            {
                tweeFilePath = path;
                ParseTweeFile();
            }
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Reload File"))
        {
            ParseTweeFile();
        }
        
        if (passages == null || passages.Count == 0)
        {
            GUILayout.Space(20);
            EditorGUILayout.HelpBox("Select a valid .twee file to view its flow and content.", MessageType.Info);
            return;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        // Show variables
        GUILayout.Space(10);
        GUILayout.Label("Variables Detected", EditorStyles.boldLabel);
        if (variables.Count > 0)
        {
            EditorGUI.indentLevel++;
            foreach(var v in variables)
            {
                EditorGUILayout.LabelField("$" + v);
            }
            EditorGUI.indentLevel--;
        }
        else
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("None");
            EditorGUI.indentLevel--;
        }
        
        GUILayout.Space(20);
        EditorGUILayout.LabelField($"Story Flow (Start Node: {startNode})", EditorStyles.boldLabel);
        
        foreach (var passagePair in passages)
        {
            var passage = passagePair.Value;
            
            EditorGUILayout.BeginVertical(boxStyle);
            
            EditorGUILayout.LabelField("Passage: " + passage.Title, EditorStyles.boldLabel);
            
            // Text preview
            string bodyPreview = passage.Body.Trim();
            if (bodyPreview.Length > 200) bodyPreview = bodyPreview.Substring(0, 200) + "...";
            if (!string.IsNullOrEmpty(bodyPreview))
            {
                EditorGUILayout.LabelField("Text:", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField(bodyPreview, EditorStyles.wordWrappedLabel);
            }
            
            // Images
            if (passage.Images != null && passage.Images.Count > 0)
            {
                GUILayout.Space(5);
                EditorGUILayout.LabelField("Images:", EditorStyles.miniBoldLabel);
                EditorGUI.indentLevel++;
                foreach (var img in passage.Images)
                {
                    EditorGUILayout.LabelField("🖼 " + img);
                }
                EditorGUI.indentLevel--;
            }
            
            // Set commands
            if (passage.SetCommands != null && passage.SetCommands.Count > 0)
            {
                GUILayout.Space(5);
                EditorGUILayout.LabelField("Sets Variables:", EditorStyles.miniBoldLabel);
                EditorGUI.indentLevel++;
                foreach (var cmd in passage.SetCommands)
                {
                    EditorGUILayout.LabelField($"⚙ ${cmd.VariableName} to {cmd.Value}");
                }
                EditorGUI.indentLevel--;
            }
            
            // Choices
            if (passage.ParsedChoices != null && passage.ParsedChoices.Count > 0)
            {
                GUILayout.Space(5);
                EditorGUILayout.LabelField("Choices (Links):", EditorStyles.miniBoldLabel);
                EditorGUI.indentLevel++;
                foreach (var choice in passage.ParsedChoices)
                {
                    string cond = choice.HasCondition ? $"(if: ${choice.ConditionVariable} {choice.ConditionOperator} {choice.ConditionValue}) " : "";
                    EditorGUILayout.LabelField($"➔ {cond}[{choice.Text}] -> {choice.Target}");
                }
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void ParseTweeFile()
    {
        if (string.IsNullOrEmpty(tweeFilePath) || !File.Exists(tweeFilePath))
        {
            passages = null;
            variables = null;
            return;
        }
        
        TweeParser parser = new TweeParser();
        string text = File.ReadAllText(tweeFilePath);
        passages = parser.ParseTweeFileFromText(text);
        
        // Find start node
        startNode = "Start";
        if (!passages.ContainsKey(startNode))
        {
            foreach(var key in passages.Keys)
            {
                startNode = key;
                break;
            }
        }

        variables = new HashSet<string>();
        foreach (var passage in passages.Values)
        {
            if (passage.SetCommands != null)
            {
                foreach (var cmd in passage.SetCommands)
                {
                    variables.Add(cmd.VariableName);
                }
            }
            if (passage.ParsedChoices != null)
            {
                foreach (var choice in passage.ParsedChoices)
                {
                    if (choice.HasCondition)
                        variables.Add(choice.ConditionVariable);
                }
            }
        }
    }
}
