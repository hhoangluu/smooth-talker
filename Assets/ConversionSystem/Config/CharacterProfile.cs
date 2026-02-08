using UnityEngine;
using ConversionSystem.Data;

namespace ConversionSystem.Config
{
    [CreateAssetMenu(fileName = "NewCharacter", menuName = "Conversion System/Character Profile")]
    public class CharacterProfile : ScriptableObject
    {
        public string CharacterId;
        public Sprite Avatar;
        public PlayerType PlayerType = PlayerType.Default;
    }
}
