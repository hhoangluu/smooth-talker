using UnityEngine;

namespace ConversionSystem.Config
{
    [CreateAssetMenu(fileName = "NewPersonality", menuName = "Conversion System/Personality Config")]
    public class PersonalityConfig : ScriptableObject
    {
        public string PersonalityId;
        public Sprite Avatar;
        public string VoiceModel = "aura-asteria-en";

        [TextArea(3, 6)]
        public string PersonalityPrompt;

        [Header("Specific Behavior")]
        [TextArea(3, 6)]
        public string DefaultBehavior;

        [TextArea(3, 6)]
        public string HotGirlBehavior;

        [TextArea(3, 6)]
        public string GrandMaBehavior;

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
