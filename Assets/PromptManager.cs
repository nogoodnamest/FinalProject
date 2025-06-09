using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// Manages multi-turn conversation generation for a given GroupData using Gemini API.
/// </summary>
public class PromptManager : MonoBehaviour
{
    public GeminiAPI geminiAPI; // Assign in the inspector 

    /// <summary>
    /// Generates a prompt based on the group's state and sends it to Gemini.
    /// Parses and processes the response as conversation turns.
    /// </summary>
    /// <param name="groupData">Group containing NPCs and conversation history</param>
    /// <param name="turnCount">Number of lines Gemini should generate</param>
    public void GenerateMultiTurnConversation(Group.GroupData groupData, int turnCount = 10)
    {
        string prompt;

        // Decide whether this is the first conversation or a continuation
        if (groupData.conversationHistory == null || groupData.conversationHistory.Count == 0)
        {
            // If it's the first turn, generate the initial character info prompt
            prompt = GenerateInitialPrompt(groupData, turnCount);
        }
        else
        {
            // Otherwise, continue from previous turns
            prompt = GenerateMultiTurnPrompt(groupData, turnCount);
        }

        // Send prompt to Gemini and handle response
        geminiAPI.SendPrompt(prompt, (response) =>
        {
            // Parse Gemini's response into a list of turns
            List<ConversationTurn> turns = ParseMultiTurnResponse(response);

            // Loop through each generated turn
            foreach (var turn in turns)
            {
                // Find the NPC who matches the speaker name
                Character speaker = groupData.characters.FirstOrDefault(c => c.npcID == turn.speaker);
                if (speaker != null)
                {
                    // Make the character speak and update conversation history
                    speaker.Broadcast(turn.message); // Broadcast NPC message
                    groupData.conversationHistory.Add(turn);
                }
                else
                {
                    Debug.LogWarning($"Speaker '{turn.speaker}' not found in group.");
                }
            }
        });
    }

    /// <summary>
    /// Creates the first-time prompt that includes full NPC descriptions and interests.
    /// </summary>
    /// <param name="groupData">Group containing NPCs and conversation history</param>
    /// <param name="turnCount">Number of lines Gemini should generate</param>
    private string GenerateInitialPrompt(Group.GroupData groupData, int turnCount = 10)
    {
        StringBuilder prompt = new StringBuilder();

        // Set up the scene and task
        prompt.AppendLine("You are simulating a group conversation among AI-driven NPCs in a virtual environment.");
        prompt.AppendLine($"The topic of discussion is: \"{groupData.topic}\"."); // The group topic
        prompt.AppendLine("\nHere is detailed information about the NPCs involved in the conversation:\n");

        // Add data about each NPC into the prompt
        foreach (Character npc in groupData.characters)
        {
            // Get interest level for current topic
            string interestLevel = npc.interests.FirstOrDefault(i => i.interestName == groupData.topic)?.interestLevel.ToString() ?? "Unknown";

            // Format likes and dislikes as comma-separated lists
            string likes = (npc.likes != null && npc.likes.Length > 0) ? string.Join(", ", npc.likes) : "None";
            string dislikes = (npc.dislikes != null && npc.dislikes.Length > 0) ? string.Join(", ", npc.dislikes) : "None";

            // Format relationships (e.g. "Alex (+2)") Not needed?
            string relations = (npc.relations != null && npc.relations.Length > 0)
                ? string.Join(", ", npc.relations.Select(r => $"{r.relationName} ({r.relationLevel:+#;-#;0})"))
                : "None";
            
            // Append NPC details to prompt
            prompt.AppendLine($"- {npc.npcID}");
            prompt.AppendLine($"  Description: {npc.description}");
            prompt.AppendLine($"  Interest in topic: {interestLevel}");
            prompt.AppendLine($"  Likes: {likes}");
            prompt.AppendLine($"  Dislikes: {dislikes}");
            prompt.AppendLine($"  Relationships: {relations}\n");
        }

        prompt.AppendLine($"Generate an engaging and natural conversation among the above NPCs based on their descriptions, likes, and dislikes.");
        prompt.AppendLine($"Format the one turn of conversation strictly as:");
        prompt.AppendLine("npcID: message");
        prompt.AppendLine("Use only npcIDs from the list above.");
        prompt.AppendLine($"Begin the conversation below with {turnCount} total turns.");

        return prompt.ToString();
    }

    /// <summary>
    /// Creates a follow-up prompt using conversation history and current interests.
    /// </summary>
    private string GenerateMultiTurnPrompt(Group.GroupData groupData, int turnCount = 10)
    {
        StringBuilder prompt = new StringBuilder();

        // Frame the continuing conversation
        prompt.AppendLine($"The group is continuing their conversation on the topic: \"{groupData.topic}\".");
        prompt.AppendLine("\nHere are the active NPCs in this group:");

        // List all NPCs and their interest and relations
        foreach (Character npc in groupData.characters)
        {
            string interest = npc.interests.FirstOrDefault(i => i.interestName == groupData.topic)?.interestLevel.ToString() ?? "Unknown";
            string relations = (npc.relations != null && npc.relations.Length > 0)
                ? string.Join(", ", npc.relations.Select(r => $"{r.relationName} ({r.relationLevel:+#;-#;0})"))
                : "None";

            prompt.AppendLine($"- {npc.npcID}: Interest in topic = {interest}; Relations: {relations}");
        }

        // Add most recent conversation lines as context
        prompt.AppendLine("\nRecent conversation turns:");

        int recentCount = Mathf.Min(20, groupData.conversationHistory.Count); // Show last 20 lines of chat history or less if not enough
        for (int i = groupData.conversationHistory.Count - recentCount; i < groupData.conversationHistory.Count; i++)
        {
            var turn = groupData.conversationHistory[i];
            prompt.AppendLine($"{turn.speaker}: {turn.message}");
        }

        // Ask Gemini to continue with new turns
        prompt.AppendLine($"\nContinue the conversation with {turnCount} more lines.");
        prompt.AppendLine("Format strictly as: npcID: message");
        prompt.AppendLine("Only include NPCs listed above.");

        return prompt.ToString();
    }

    /// <summary>
    /// Parses Gemini's multi-line response into structured turns.
    /// </summary>
    private List<ConversationTurn> ParseMultiTurnResponse(string rawResponse)
    {
        var lines = rawResponse.Split('\n'); // Split lines by newline
        List<ConversationTurn> turns = new List<ConversationTurn>();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            int separatorIndex = line.IndexOf(':');
            if (separatorIndex > 0)
            {
                // Look for the first colon to separate speaker from message
                string speaker = line.Substring(0, separatorIndex).Trim();
                string message = line.Substring(separatorIndex + 1).Trim();

                turns.Add(new ConversationTurn(speaker, message));
            }
        }

        return turns;
    }

}



