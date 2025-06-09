using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

public class GeminiAPI : MonoBehaviour
{
    [Header("Replace this with your Gemini API Key")]
    private string apiKey;

    private string apiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key=";

    private void Start()
    {
        apiKey = ConfigManager.Config.apiKey;

        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("API Key is missing! Please check your config.");
        }
    }

    /// <summary>
    /// Sends a prompt to Gemini and returns the LLM's response via callback.
    /// </summary>
    public void SendPrompt(string prompt, Action<string> onResponse)
    {
        StartCoroutine(SendPromptCoroutine(prompt, onResponse));
    }

    private IEnumerator SendPromptCoroutine(string prompt, Action<string> onResponse)
    {
        string fullUrl = apiUrl + apiKey;

        string jsonBody = $@"
        {{
            ""contents"": [
                {{
                    ""parts"": [
                        {{
                            ""text"": ""{EscapeJsonString(prompt)}""
                        }}
                    ]
                }}
            ]
        }}";

        using (UnityWebRequest request = new UnityWebRequest(fullUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string rawJson = request.downloadHandler.text;
                Debug.Log("Full Gemini JSON Response:\n" + rawJson);

                string messageText = ExtractTextFromResponse(rawJson);
                Debug.Log("Parsed Gemini Message:\n" + messageText);

                onResponse?.Invoke(messageText);
            }
            else
            {
                Debug.LogError("Error: " + request.error + " | Response: " + request.downloadHandler.text);
                onResponse?.Invoke(null);
            }
        }
    }


    private string EscapeJsonString(string input)
    {
        return input.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r");
    }

    private string ExtractTextFromResponse(string rawJson)
    {
        GeminiResponse parsed = JsonUtility.FromJson<GeminiResponse>(rawJson);

        if (parsed != null && parsed.candidates.Length > 0 &&
            parsed.candidates[0].content != null &&
            parsed.candidates[0].content.parts.Length > 0)
        {
            return parsed.candidates[0].content.parts[0].text;
        }

        return "[Failed to parse Gemini response]";
    }

    [System.Serializable]
    public class GeminiResponse
    {
        public Candidate[] candidates;
    }

    [System.Serializable]
    public class Candidate
    {
        public Content content;
    }

    [System.Serializable]
    public class Content
    {
        public Part[] parts;
    }

    [System.Serializable]
    public class Part
    {
    public string text;
    }
}
