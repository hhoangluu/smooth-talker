using System;
using System.Collections.Generic;

namespace ConversionSystem.Data
{
    public enum PlayerType
    {
        Default,
        HotGirl,
        GrandMa
    }

    /// <summary>
    /// Request data to send to AI API
    /// </summary>
    [Serializable]
    public class AIRequestData
    {
        public string PersonalityDescription;
        public string SpecificBehavior;
        public string RaiseSuspicionTriggers;
        public string LowerSuspicionTriggers;
        public string Catchphrases;
        public int CurrentTurn;
        public int MaxTurns;
        public string PlayerInput;
        public List<DialogueEntry> History;

        public AIRequestData()
        {
            History = new List<DialogueEntry>();
        }
    }

    [Serializable]
    public class DialogueEntry
    {
        public string Speaker;
        public string Message;

        public DialogueEntry(string speaker, string message)
        {
            Speaker = speaker;
            Message = message;
        }

        public override string ToString()
        {
            return $"{Speaker}: {Message}";
        }
    }
}
