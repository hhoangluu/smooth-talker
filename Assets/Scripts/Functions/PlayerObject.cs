using System;
using System.Collections.Generic;
using UnityEngine;
using ConversionSystem.Core;

public class PlayerObject : MonoBehaviour
{
    [Serializable]
    public struct NamedObject
    {
        public string Key;
        public GameObject Value;
    }

    public List<NamedObject> Characters;
    public List<NamedObject> NPCs;

    private void OnEnable()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        string characterId = gm.CurrentCharacter != null ? gm.CurrentCharacter.CharacterId : "";
        string npcId = gm.CurrentNPC != null ? gm.CurrentNPC.PersonalityId : "";

        foreach (var entry in Characters)
        {
            if (entry.Value != null)
                entry.Value.SetActive(entry.Key == characterId);
        }

        foreach (var entry in NPCs)
        {
            if (entry.Value != null)
                entry.Value.SetActive(entry.Key == npcId);
        }
    }
}
