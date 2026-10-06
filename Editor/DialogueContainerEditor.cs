using UnityEngine;
using UnityEditor;
using UnityEditor.Networking;
using UnityEngine.Networking;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using SimpleTwineDialogue;

[CustomEditor(typeof(DialogueContainer))]
public class DialogueContainerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DialogueContainer container = (DialogueContainer)target;

        GUILayout.Space(10);
        if (GUILayout.Button("Download Images to StreamingAssets"))
        {
            DownloadImages(container);
        }

        GUILayout.Space(10);
        GUILayout.Label("Twee Variables", EditorStyles.boldLabel);
        
        if (!string.IsNullOrEmpty(container.localFileName))
        {
            string storyFolder = Path.Combine(Application.streamingAssetsPath, "Story");
            string filePath = Path.Combine(storyFolder, container.localFileName + ".twee");
            
            if (File.Exists(filePath))
            {
                TweeParser parser = new TweeParser();
                string text = File.ReadAllText(filePath);
                var passages = parser.ParseTweeFileFromText(text);
                
                HashSet<string> vars = new HashSet<string>();
                foreach (var passage in passages.Values)
                {
                    if (passage.SetCommands != null)
                    {
                        foreach (var cmd in passage.SetCommands)
                        {
                            vars.Add(cmd.VariableName);
                        }
                    }
                    if (passage.ParsedChoices != null)
                    {
                        foreach (var choice in passage.ParsedChoices)
                        {
                            if (choice.HasCondition)
                                vars.Add(choice.ConditionVariable);
                        }
                    }
                }
                
                if (vars.Count > 0)
                {
                    EditorGUI.indentLevel++;
                    foreach(string v in vars)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(v, GUILayout.Width(120));
                        
                        string type = PlayerPrefs.GetString(v + "_type", "");
                        if (type == "int" || type == "bool") 
                        {
                            int val = PlayerPrefs.GetInt(v, 0);
                            int newVal = EditorGUILayout.IntField(val);
                            if (newVal != val) PlayerPrefs.SetInt(v, newVal);
                        }
                        else if (type == "float")
                        {
                            float val = PlayerPrefs.GetFloat(v, 0f);
                            float newVal = EditorGUILayout.FloatField(val);
                            if (newVal != val) PlayerPrefs.SetFloat(v, newVal);
                        }
                        else 
                        {
                            string val = PlayerPrefs.GetString(v, "");
                            string newVal = EditorGUILayout.TextField(val);
                            if (newVal != val) PlayerPrefs.SetString(v, newVal);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUI.indentLevel--;
                    if (GUI.changed) {
                        PlayerPrefs.Save();
                    }

                    if (GUILayout.Button("Clear All Progress & Variables"))
                    {
                        PlayerPrefs.DeleteKey("SavedPassage_" + container.localFileName);
                        foreach (string v in vars)
                        {
                            PlayerPrefs.DeleteKey(v);
                            PlayerPrefs.DeleteKey(v + "_type");
                        }
                        PlayerPrefs.Save();
                        Debug.Log("Progress and variables cleared.");
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("No variables found in the twee file.", MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Twee file not found.", MessageType.Warning);
            }
        }
    }

    private void DownloadImages(DialogueContainer container)
    {
        if (string.IsNullOrEmpty(container.localFileName))
        {
            Debug.LogError("Local File Name is empty!");
            return;
        }

        string storyFolder = Path.Combine(Application.streamingAssetsPath, "Story");
        string filePath = Path.Combine(storyFolder, container.localFileName + ".twee");

        if (!File.Exists(filePath))
        {
            Debug.LogError("Twee file not found: " + filePath);
            
            // Try to find ANY twee file if localFileName isn't exact
            string[] tweeFiles = Directory.GetFiles(storyFolder, "*.twee");
            if (tweeFiles.Length > 0)
            {
                Debug.LogWarning("Using first available twee file instead: " + tweeFiles[0]);
                filePath = tweeFiles[0];
            }
            else
            {
                return;
            }
        }

        TweeParser parser = new TweeParser();
        string text = File.ReadAllText(filePath);
        var passages = parser.ParseTweeFileFromText(text);

        HashSet<string> imageUrls = new HashSet<string>();
        foreach (var passage in passages.Values)
        {
            foreach (string img in passage.Images)
            {
                if (img.StartsWith("http://", System.StringComparison.OrdinalIgnoreCase) || 
                    img.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
                {
                    imageUrls.Add(img);
                }
            }
        }

        if (imageUrls.Count == 0)
        {
            Debug.Log("No web images found in the .twee file.");
            return;
        }

        string imagesFolder = Path.Combine(Application.streamingAssetsPath, "images");
        if (!Directory.Exists(imagesFolder))
        {
            Directory.CreateDirectory(imagesFolder);
        }

        Debug.Log($"Found {imageUrls.Count} web images. Starting download...");
        // Start an editor coroutine to download
        EditorCoroutine.Start(DownloadImagesRoutine(imageUrls, imagesFolder));
    }

    private IEnumerator DownloadImagesRoutine(HashSet<string> imageUrls, string saveFolder)
    {
        int count = 0;
        foreach (string url in imageUrls)
        {
            string safeName = DialogueContainer.GetSafeFilename(url);
            string savePath = Path.Combine(saveFolder, safeName);

            if (File.Exists(savePath))
            {
                Debug.Log($"Image already exists locally: {safeName}");
                count++;
                continue;
            }

            Debug.Log($"Downloading: {url}");
            using (UnityWebRequest uwr = UnityWebRequest.Get(url))
            {
                // Send request
                var operation = uwr.SendWebRequest();
                while (!operation.isDone)
                {
                    yield return null;
                }

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to download {url}: {uwr.error}");
                }
                else
                {
                    byte[] bytes = uwr.downloadHandler.data;
                    if (bytes != null && bytes.Length > 0)
                    {
                        File.WriteAllBytes(savePath, bytes);
                        Debug.Log($"Saved: {savePath}");
                        count++;
                    }
                    else
                    {
                        Debug.LogError($"Failed to download {url}: No data received.");
                    }
                }
            }
        }

        Debug.Log($"Download complete. {count}/{imageUrls.Count} images saved to StreamingAssets/images.");
        AssetDatabase.Refresh();
    }
}

// Simple editor coroutine runner
public static class EditorCoroutine
{
    private static IEnumerator current;

    public static void Start(IEnumerator routine)
    {
        current = routine;
        EditorApplication.update += Update;
    }

    private static void Update()
    {
        if (current == null)
        {
            EditorApplication.update -= Update;
            return;
        }

        if (!current.MoveNext())
        {
            EditorApplication.update -= Update;
            current = null;
        }
    }
}
