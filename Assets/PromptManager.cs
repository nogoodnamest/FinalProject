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

    public void GenerateMultiTurnConversation(Group.GroupData groupData, int turnCount = 10)
    {
        string prompt = GenerateMultiTurnPrompt(groupData, turnCount);

        geminiAPI.SendPrompt(prompt, (response) =>
        {
            List<ConversationTurn> turns = ParseMultiTurnResponse(response);

            foreach (var turn in turns)
            {
                Character speaker = groupData.characters.FirstOrDefault(c => c.npcID == turn.speaker);
                if (speaker != null)
                {
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

    private string GenerateInitialPrompt(Group.GroupData groupData, int turnCount = 10)
    {
        StringBuilder prompt = new StringBuilder();

        prompt.AppendLine("You are simulating a group conversation among AI-driven NPCs in a virtual environment.");
        prompt.AppendLine($"The topic of discussion is: \"{groupData.topic}\".");
        prompt.AppendLine("\nHere is detailed information about the NPCs involved in the conversation:\n");

        foreach (Character npc in groupData.characters)
        {
            string interestLevel = npc.interests.FirstOrDefault(i => i.interestName == groupData.topic)?.interestLevel.ToString() ?? "Unknown";
            string likes = (npc.likes != null && npc.likes.Length > 0) ? string.Join(", ", npc.likes) : "None";
            string dislikes = (npc.dislikes != null && npc.dislikes.Length > 0) ? string.Join(", ", npc.dislikes) : "None";
            string relations = (npc.relations != null && npc.relations.Length > 0)
                ? string.Join(", ", npc.relations.Select(r => $"{r.relationName} ({r.relationLevel:+#;-#;0})"))
                : "None";

            prompt.AppendLine($"- {npc.npcID}");
            prompt.AppendLine($"  Description: {npc.description}");
            prompt.AppendLine($"  Interest in topic: {interestLevel}");
            prompt.AppendLine($"  Likes: {likes}");
            prompt.AppendLine($"  Dislikes: {dislikes}");
            prompt.AppendLine($"  Relationships: {relations}\n");
        }

        prompt.AppendLine($"Please generate an engaging and natural conversation among the above NPCs.");
        prompt.AppendLine($"Format strictly as:");
        prompt.AppendLine("npcID: message");
        prompt.AppendLine("Use only npcIDs from the list above.");
        prompt.AppendLine($"Begin the conversation below with {turnCount} total lines.");

        return prompt.ToString();
    }

    private string GenerateMultiTurnPrompt(Group.GroupData groupData, int turnCount = 10)
    {
        StringBuilder prompt = new StringBuilder();

        prompt.AppendLine($"The group is continuing their conversation on the topic: \"{groupData.topic}\".");

        prompt.AppendLine("\nHere are the active NPCs in this group:");

        foreach (Character npc in groupData.characters)
        {
            string interest = npc.interests.FirstOrDefault(i => i.interestName == groupData.topic)?.interestLevel.ToString() ?? "Unknown";
            string relations = (npc.relations != null && npc.relations.Length > 0)
                ? string.Join(", ", npc.relations.Select(r => $"{r.relationName} ({r.relationLevel:+#;-#;0})"))
                : "None";

            prompt.AppendLine($"- {npc.npcID}: Interest in topic = {interest}; Relations: {relations}");
        }

        prompt.AppendLine("\nRecent conversation turns:");

        int recentCount = Mathf.Min(6, groupData.conversationHistory.Count);
        for (int i = groupData.conversationHistory.Count - recentCount; i < groupData.conversationHistory.Count; i++)
        {
            var turn = groupData.conversationHistory[i];
            prompt.AppendLine($"{turn.speaker}: {turn.message}");
        }

        prompt.AppendLine($"\nContinue the conversation with {turnCount} more lines.");
        prompt.AppendLine("Format strictly as: npcID: message");
        prompt.AppendLine("Only include NPCs listed above.");

        return prompt.ToString();
    }

    private List<ConversationTurn> ParseMultiTurnResponse(string rawResponse)
    {
        var lines = rawResponse.Split('\n');
        List<ConversationTurn> turns = new List<ConversationTurn>();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            int separatorIndex = line.IndexOf(':');
            if (separatorIndex > 0)
            {
                string speaker = line.Substring(0, separatorIndex).Trim();
                string message = line.Substring(separatorIndex + 1).Trim();

                turns.Add(new ConversationTurn(speaker, message));
            }
        }

        return turns;
    }


    
}



