using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System;

public class GeminiAPI : MonoBehaviour
{
    
    private string apiKey;  //Replace this with your Gemini API Key

    private string apiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key=";

    private void Start()
    {
        apiKey = ConfigManager.Config.apiKey;
        
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("API Key is missing! Please check your config.");
        }
    }

    public void SendPrompt(string prompt, Action<string> onResponse)
    {
        StartCoroutine(SendPromptCoroutine(prompt, onResponse));
    }

    private IEnumerator SendPromptCoroutine(string prompt, Action<string> onResponse)
    {
        string fullUrl = apiUrl + apiKey;
        
        // Create the JSON body for the request
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
                onResponse?.Invoke(request.downloadHandler.text);
            }
            else
            {
                onResponse?.Invoke($"Error: {request.error} | Response: {request.downloadHandler.text}");
            }
        }
    }

    private string EscapeJsonString(string input)
    {
        return input.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }


}