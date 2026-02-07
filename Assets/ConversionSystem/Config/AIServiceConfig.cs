using UnityEngine;

namespace ConversionSystem.Config
{
    public enum AIProvider
    {
        Gemini,
        ChatGPT,
        Mistral
    }

    [CreateAssetMenu(fileName = "AIServiceConfig", menuName = "Conversion System/AI Service Config")]
    public class AIServiceConfig : ScriptableObject
    {
        [Header("Provider")]
        public AIProvider Provider = AIProvider.Gemini;

        [Header("API Settings")]
        public string ApiKey;
        public string ModelName = "gemini-2.5-flash";

        [Header("Generation Settings")]
        [Range(0f, 2f)]
        public float Temperature = 1f;

        [Range(0f, 1f)]
        public float TopP = 0.95f;

        public int MaxTokens = 1024;

        [Header("Prompt Template")]
        [TextArea(10, 20)]
        public string PromptTemplate = @"You are a traffic police NPC in a game.
The player was caught speeding and is trying to get off with a warning.

CHARACTER INFO:
{PersonalityDescription}

TRIGGERS (RAISE suspicion - be more strict):
{RaiseSuspicionTriggers}

SOFT SPOTS (LOWER suspicion - be more lenient):
{LowerSuspicionTriggers}

CATCHPHRASES (use these in your dialogue):
{Catchphrases}

GAME STATE:
- Current turn: {CurrentTurn}/{MaxTurns}
- If this is turn {MaxTurns}, you MUST make a final decision (TICKET or WARNING).

CONVERSATION HISTORY:
{History}

PLAYER'S LATEST MESSAGE:
""{PlayerInput}""

OUTPUT REQUIREMENTS:
Return a JSON object with these fields:
- dialogue: (string) Your response to the player (in character, under 40 words, use catchphrases when appropriate).
- leniency_score: (int) Leniency score from 0-100 (higher = more likely to pardon).
- decision: (string) ""PENDING"" (if not final turn), ""TICKET"" (Fine), ""WARNING"" (Let go).";
    }
}
