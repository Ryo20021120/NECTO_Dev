using System;
using System.Collections.Generic;
using UnityEngine;

namespace Necto.Data
{
    [Serializable]
    public sealed class NectoPlayableCharacterData
    {
        [Header("プレイアブルキャラクター")]
        public string id;
        public string displayName;
        public GameObject prefab;
        public GameObject iconPrefab;
    }

    /// <summary>JSONマスターとは独立したデバッグ用プレイアブルDB。</summary>
    [CreateAssetMenu(menuName = "NECTO/Debug/Playable Character Database", fileName = "NectoPlayableCharacterDatabase")]
    public sealed class NectoPlayableCharacterDatabase : ScriptableObject
    {
        public const string ResourcePath = "Data/Debug/NectoPlayableCharacterDatabase";

        [SerializeField] private List<NectoPlayableCharacterData> characters =
            new List<NectoPlayableCharacterData>();

        public IReadOnlyList<NectoPlayableCharacterData> Characters => characters;

        public NectoPlayableCharacterData Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return characters.Find(character => character != null && character.id == id);
        }

        public static NectoPlayableCharacterDatabase Load()
        {
            return Resources.Load<NectoPlayableCharacterDatabase>(ResourcePath);
        }
    }
}
