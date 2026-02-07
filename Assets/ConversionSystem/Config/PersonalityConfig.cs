using UnityEngine;

namespace ConversionSystem.Config
{
    [CreateAssetMenu(fileName = "NewPersonality", menuName = "Conversion System/Personality Config")]
    public class PersonalityConfig : ScriptableObject
    {
        public string PersonalityId;

        [TextArea(3, 6)]
        public string PersonalityPrompt;

        [TextArea(2, 4)]
        public string OpeningDialogue;

        [Header("Triggers (Raise Suspicion)")]
        [TextArea(3, 6)]
        public string RaiseSuspicionTriggers;

        [Header("Soft Spots (Lower Suspicion)")]
        [TextArea(3, 6)]
        public string LowerSuspicionTriggers;

        [Header("Catchphrases")]
        public string[] Catchphrases;
    }
}
