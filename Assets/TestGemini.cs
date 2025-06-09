using System.Collections;
using UnityEngine;

// Define the classes needed to deserialize the JSON
// You can place these inside your TestGemini file or in a separate file.
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


public class TestGemini : MonoBehaviour
{
    public GeminiAPI geminiAPI;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(0.5f); // Wait before sending the prompt

        geminiAPI.SendPrompt("Explain how AI works in a few words", (responseJson) =>
        {
            Debug.Log("Full Gemini JSON Response: " + responseJson);

            GeminiResponse parsedResponse = JsonUtility.FromJson<GeminiResponse>(responseJson);

            // Check for nulls and array lengths to avoid errors.
            if (parsedResponse != null && parsedResponse.candidates.Length > 0 &&
                parsedResponse.candidates[0].content != null &&
                parsedResponse.candidates[0].content.parts.Length > 0)
            {
                string aiMessage = parsedResponse.candidates[0].content.parts[0].text;

                Debug.Log("Parsed AI Message: " + aiMessage);
            }
            else
            {
                Debug.LogError("Could not parse Gemini response or the response was empty.");
            }
        });
    }
}