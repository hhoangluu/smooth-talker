using System;
using UnityEngine;

namespace ConversionSystem.Data
{
    /// <summary>
    /// Response data from AI API
    /// </summary>
    [Serializable]
    public class AIResponseData
    {
        [SerializeField] private string dialogue;
        [SerializeField] private int leniency_score;
        [SerializeField] private string decision;

        public string Dialogue => dialogue;
        public int LeniencyScore => leniency_score;
        public DecisionType Decision => ParseDecision(decision);

        private DecisionType ParseDecision(string value)
        {
            if (string.IsNullOrEmpty(value))
                return DecisionType.Pending;

            return value.ToUpper() switch
            {
                "TICKET" => DecisionType.Ticket,
                "WARNING" => DecisionType.Warning,
                _ => DecisionType.Pending
            };
        }

        public bool IsFinalDecision => Decision != DecisionType.Pending;
        public bool IsPlayerPardoned => Decision == DecisionType.Warning;
    }

    public enum DecisionType
    {
        Pending,
        Ticket,
        Warning
    }
}
