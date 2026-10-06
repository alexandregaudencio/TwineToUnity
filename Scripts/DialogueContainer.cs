using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;

namespace SimpleTwineDialogue
{

    public class DialogueContainer : MonoBehaviour
    {
        public bool startOnLoad = true;
        public string localFileName;
        public bool saveProgress = true;

        [Header("UI Components")]
        // Text component to display passage content
        public TextMeshProUGUI passageText;
        
        // Prefab for choice buttons that will be instantiated
        public Button choiceButtonPrefab;
        
        // Container where choice buttons will be spawned
        public Transform choiceButtonContainer;
        
        // Prefab for images that will be instantiated
        public Image imagePrefab;
        public AudioSource audioSource;

        [Header("Effect Settings")]
        // Speed of the image pulse effect (higher = faster). Set in the Inspector.
        [Tooltip("Speed of the image pulse effect (higher = faster). Set in the Inspector.")]
        public float imagePulseSpeed = 5;

        // Typing effect settings
        [Tooltip("Characters per second for the typewriter effect.")]
        public float typingSpeed = 60f;

        // Sound to play when a passage typing starts
        public AudioClip typeStartSfx;
        // Counter for tracking how many choices the player has made
        int myChoices = 0;
        public TextMeshProUGUI myChoiceCounterUI;

        [Space(10) ]
        // Internal handle for the typing coroutine so we can cancel it
        private Coroutine typingCoroutine;

        // Parser instance for reading Twee files
        private TweeParser tweeParser;
        
        // Dictionary storing all passages from the Twee file
        private Dictionary<string, TweeParser.Passage> passages;
        
        // Title of the currently displayed passage
        private string currentPassageTitle;
        private Coroutine dialogueCoroutine;
        /// <summary>
        /// Initialize the text adventure and start loading the Twee file
        /// </summary>
        void Start()
        {
           if(startOnLoad)
           {
                StartupDialogue();
           }
        }

        void StartupDialogue()
        {
            StartDialogue(localFileName);
        }

        void StartDialogue(string filePath)
        {
            tweeParser = new TweeParser();
            if(dialogueCoroutine != null)
            {
                StopCoroutine(dialogueCoroutine);
            }
            dialogueCoroutine = StartCoroutine(LoadTweeFile(Path.Combine(Application.streamingAssetsPath, "Story", filePath+".twee")));
        }


       
        /// <summary>
        /// Called when a choice button is clicked
        /// </summary>
        /// <param name="choiceTitle">The target passage to navigate to</param>
        /// <param name="currentPassageText">The text of the current passage</param>
        void OnChoiceSelected(string choiceTitle, string currentPassageText)
        {
            DisplayPassage(choiceTitle);
            myChoices += 1;
            myChoiceCounterUI.text = "Choices made: " + myChoices.ToString();
        }

        /// <summary>
        /// Load and parse the Twee file from either web or local storage
        /// </summary>
        /// <param name="filePath">Path to the Twee file (URL for web, file path for local)</param>
        IEnumerator LoadTweeFile(string filePath)
        {
           
                // Load from local StreamingAssets folder
                if (File.Exists(filePath))
                {
                    string text = File.ReadAllText(filePath, Encoding.UTF8);
                    passages = tweeParser.ParseTweeFileFromText(text);
                    
                    CheckForStartPassage();

                    yield break; // Exit the coroutine since we're using local file loading
                }
                else
                {
                    Debug.LogError("Twee file not found in StreamingAssets/Story: " + filePath);
                    yield break;
                }

            

        }

        IEnumerator LoadImage(string imageFileName)
        {
            if (imagePrefab == null )
            {
                Debug.LogError("ImagePrefab is not assigned.");
                yield break;
            }

            Texture2D texture = null;

            if (imageFileName.StartsWith("http://", System.StringComparison.OrdinalIgnoreCase) || imageFileName.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
            {
                string safeName = GetSafeFilename(imageFileName);
                string localCachePath = Path.Combine(Application.streamingAssetsPath, "images", safeName);

                if (File.Exists(localCachePath))
                {
                    byte[] imageBytes = File.ReadAllBytes(localCachePath);
                    texture = new Texture2D(2, 2);
                    texture.LoadImage(imageBytes);
                }
                else
                {
                    using (UnityWebRequest uwr = UnityWebRequest.Get(imageFileName))
                    {
                        yield return uwr.SendWebRequest();

                        if (uwr.result != UnityWebRequest.Result.Success)
                        {
                            Debug.LogError($"Failed to download image from {imageFileName}: {uwr.error}");
                            yield break;
                        }
                        byte[] downloadedBytes = uwr.downloadHandler.data;
                        texture = new Texture2D(2, 2);
                        if (!texture.LoadImage(downloadedBytes))
                        {
                            Debug.LogError("Failed to parse downloaded image data as texture.");
                            yield break;
                        }
                    }
                }
            }
            else
            {
                string imagePath = Path.Combine(Application.streamingAssetsPath, "Story", imageFileName);
                if (File.Exists(imagePath))
                {
                    byte[] imageBytes = File.ReadAllBytes(imagePath);
                    texture = new Texture2D(2, 2);
                    texture.LoadImage(imageBytes);
                }
                else
                {
                    Debug.LogError("Image file not found: " + imagePath);
                    yield break;
                }
            }

            if (texture != null)
            {
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
                Image image = imagePrefab;
                image.sprite = sprite;
                image.gameObject.SetActive(true);

                var pulser = image.gameObject.GetComponent<ImagePulser>();
                if (pulser == null) pulser = image.gameObject.AddComponent<ImagePulser>();
                pulser.PulseOnce(Mathf.Max(0.0001f, imagePulseSpeed));
            }
        }

        /// <summary>
        /// Display a passage and its contents (text, choices, images) in the UI
        /// Supports all Twine link formats: [[Target]], [[Text|Target]], [[Text->Target]]
        /// </summary>
        /// <param name="passageTitle">The title of the passage to display</param>
        public void DisplayPassage(string passageTitle)
        {
            if (!passages.TryGetValue(passageTitle, out var passage))
            {
                Debug.LogError("Passage not found: " + passageTitle);
                return;
            }

            if (saveProgress)
            {
                PlayerPrefs.SetString("SavedPassage_" + localFileName, passageTitle);
                PlayerPrefs.Save();
            }
            
            // Execute variable assignments
            if (passage.SetCommands != null)
            {
                foreach (var setCmd in passage.SetCommands)
                {
                    string valStr = setCmd.Value.Trim();
                    
                    if (valStr.ToLower() == "true")
                    {
                        PlayerPrefs.SetInt(setCmd.VariableName, 1);
                        PlayerPrefs.SetString(setCmd.VariableName + "_type", "bool");
                    }
                    else if (valStr.ToLower() == "false")
                    {
                        PlayerPrefs.SetInt(setCmd.VariableName, 0);
                        PlayerPrefs.SetString(setCmd.VariableName + "_type", "bool");
                    }
                    else 
                    {
                        var mathMatch = Regex.Match(valStr, @"\$(?<var>\w+)\s*(?<op>[\+\-\*\/])\s*(?<num>\d+(?:\.\d+)?)");
                        if (mathMatch.Success)
                        {
                            string sourceVar = mathMatch.Groups["var"].Value;
                            string op = mathMatch.Groups["op"].Value;
                            float num = float.Parse(mathMatch.Groups["num"].Value, System.Globalization.CultureInfo.InvariantCulture);
                            
                            float currentVal = 0f;
                            if (PlayerPrefs.HasKey(sourceVar + "_type"))
                            {
                                string t = PlayerPrefs.GetString(sourceVar + "_type");
                                if (t == "int" || t == "bool") currentVal = PlayerPrefs.GetInt(sourceVar, 0);
                                else if (t == "float") currentVal = PlayerPrefs.GetFloat(sourceVar, 0f);
                            }
                            
                            float result = currentVal;
                            if (op == "+") result += num;
                            else if (op == "-") result -= num;
                            else if (op == "*") result *= num;
                            else if (op == "/") result /= num;

                            valStr = result.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        }

                        if (int.TryParse(valStr, out int intVal))
                        {
                            PlayerPrefs.SetInt(setCmd.VariableName, intVal);
                            PlayerPrefs.SetString(setCmd.VariableName + "_type", "int");
                        }
                        else if (float.TryParse(valStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float floatVal))
                        {
                            PlayerPrefs.SetFloat(setCmd.VariableName, floatVal);
                            PlayerPrefs.SetString(setCmd.VariableName + "_type", "float");
                        }
                        else
                        {
                            PlayerPrefs.SetString(setCmd.VariableName, valStr);
                            PlayerPrefs.SetString(setCmd.VariableName + "_type", "string");
                        }
                    }
                }
                PlayerPrefs.Save();
            }

            // Clear previous content
            ClearChoices();
            ClearImages();

            // Display passage text (use typewriter effect)
            currentPassageTitle = passageTitle;

            // Replace variables in text (e.g., $Gold)
            string textToDisplay = passage.Body;
            
            // Remove optional print wrappers like (print: $var) or <<print $var>> so they just become $var
            textToDisplay = Regex.Replace(textToDisplay, @"\(print:\s*\$(?<var>\w+)\)", "$${var}");
            textToDisplay = Regex.Replace(textToDisplay, @"<<print\s*\$(?<var>\w+)>>", "$${var}");

            var varRegex = new Regex(@"\$(?<var>\w+)");
            textToDisplay = varRegex.Replace(textToDisplay, match => 
            {
                string varName = match.Groups["var"].Value.Trim();
                if (PlayerPrefs.HasKey(varName + "_type") || PlayerPrefs.HasKey(varName))
                {
                    string type = PlayerPrefs.GetString(varName + "_type", "string");
                    if (type == "int") return PlayerPrefs.GetInt(varName, 0).ToString();
                    if (type == "float") return PlayerPrefs.GetFloat(varName, 0f).ToString();
                    return PlayerPrefs.GetString(varName, "");
                }
                return match.Value; // return original if not found
            });


            // Start typewriter effect (cancel previous if running)
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }
            typingCoroutine = StartCoroutine(TypeText(textToDisplay));

            // Create choice buttons using ParsedChoices (handles all link formats automatically)
            foreach (var choice in passage.ParsedChoices)
            {
                if (choice.HasCondition)
                {
                    float currentVal = 0f;
                    if (PlayerPrefs.HasKey(choice.ConditionVariable + "_type") || PlayerPrefs.HasKey(choice.ConditionVariable))
                    {
                        string type = PlayerPrefs.GetString(choice.ConditionVariable + "_type", "string");
                        if (type == "int" || type == "bool") currentVal = PlayerPrefs.GetInt(choice.ConditionVariable, 0);
                        else if (type == "float") currentVal = PlayerPrefs.GetFloat(choice.ConditionVariable, 0f);
                    }
                    
                    float targetVal = 0f;
                    float.TryParse(choice.ConditionValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out targetVal);
                    
                    bool conditionMet = false;
                    switch (choice.ConditionOperator)
                    {
                        case ">": conditionMet = currentVal > targetVal; break;
                        case "<": conditionMet = currentVal < targetVal; break;
                        case ">=": conditionMet = currentVal >= targetVal; break;
                        case "<=": conditionMet = currentVal <= targetVal; break;
                        case "==":
                        case "=": conditionMet = currentVal == targetVal; break;
                        case "!=": conditionMet = currentVal != targetVal; break;
                    }
                    
                    if (!conditionMet) continue;
                }

                var choiceButton = Instantiate(choiceButtonPrefab, choiceButtonContainer);
                
                // Display the choice text on the button
                choiceButton.GetComponentInChildren<TextMeshProUGUI>().text = choice.Text;
                
                // When clicked, navigate to the target passage
                string targetPassage = choice.Target; // Capture for lambda
                choiceButton.onClick.AddListener(() => OnChoiceSelected(targetPassage, passage.Body));
            }

            // Load and display images
            foreach (var imageFileName in passage.Images)
            {
                StartCoroutine(LoadImage(imageFileName));
            }
        }

        /// <summary>
        /// Typewriter effect coroutine that reveals text character-by-character.
        /// Plays `typeStartSfx` once at the start if an AudioSource is assigned.
        /// </summary>
        /// <param name="fullText">The complete passage text to reveal.</param>
        IEnumerator TypeText(string fullText)
        {
            if (passageText == null)
            {
                yield break;
            }


            passageText.text = "";

            if (typingSpeed <= 0f)
            {
                passageText.text = fullText;
                typingCoroutine = null;
                yield break;
            }

            float delay = 1f / typingSpeed;
            for (int i = 0; i < fullText.Length; i++)
            {
                if (passageText.text.Length % 5 == 0)
                {
                    if (audioSource != null && typeStartSfx != null)
                    {
                        audioSource.PlayOneShot(typeStartSfx);
                    }
                }
                passageText.text += fullText[i];
                yield return new WaitForSeconds(delay);


            }
            
            
            typingCoroutine = null;
        }

        /// <summary>
        /// Check if a "Start" passage exists and display it, or show helpful error messages
        /// </summary>
        void CheckForStartPassage(){
                string startNode = "Start";
                
                if (saveProgress && PlayerPrefs.HasKey("SavedPassage_" + localFileName))
                {
                    string savedNode = PlayerPrefs.GetString("SavedPassage_" + localFileName);
                    if (passages.ContainsKey(savedNode))
                    {
                        startNode = savedNode;
                    }
                }
                
                if (!passages.ContainsKey(startNode)) {
                    foreach(var key in passages.Keys) {
                        startNode = key;
                        break;
                    }
                }

                if (passages.ContainsKey(startNode))
                {
                    Debug.Log("Passage '" + startNode + "' found.");
                    DisplayPassage(startNode);  // Display the initial passage
                }
                else
                {
                    // Show detailed error message with troubleshooting steps
                    Debug.LogError("Passage 'Start' not found.");
                    Debug.LogError("");
                    Debug.LogError("=== HOW TO FIX ===");
                    Debug.LogError("1. Make sure your Twee file has a passage named 'Start' (case-sensitive)");
                    Debug.LogError("2. Check the StoryData section - the 'start' field should be set to 'Start'");
                    Debug.LogError("3. Verify your Twee file format:");
                    Debug.LogError("   :: Start {\"position\":\"400,100\",\"size\":\"100,100\"}");
                    Debug.LogError("   Your story text here...");
                    Debug.LogError("   [[Choice text|Target passage]]");
                    Debug.LogError("");
                    Debug.LogError("Available passages found in file:");
                    
                    if (passages.Count == 0)
                    {
                        Debug.LogError("   (No passages found - check if file was loaded correctly)");
                    }
                    else
                    {
                        // List all available passages to help debugging
                        foreach (var passageTitle in passages.Keys)
                        {
                            Debug.LogError($"   - {passageTitle}");
                        }
                    }
                }
        }

        // Clear out the button choices to make room for the new ones.
        void ClearChoices()
        {
            foreach (Transform child in choiceButtonContainer)
            {
                Destroy(child.gameObject);
            }

        }

        // Clear out the image to make room for the new one.
        void ClearImages()
        {
            imagePrefab.sprite = null;
        }

        public static string GetSafeFilename(string url)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(url));
                string hashString = System.BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                return hashString + ".png";
            }
        }
    }
}