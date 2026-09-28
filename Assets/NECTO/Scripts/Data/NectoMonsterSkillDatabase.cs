using System;
using System.Collections.Generic;
using Necto.Skills;
using UnityEngine;

namespace Necto.Data
{
    /// <summary>
    /// モンスターと、そのモンスターが使用できる技の組み合わせ。
    /// リストにエントリーを追加すれば複数体を登録できる。
    /// </summary>
    [CreateAssetMenu(menuName = "NECTO/Monster Skill Database", fileName = "NectoMonsterSkillDatabase")]
    public sealed class NectoMonsterSkillDatabase : ScriptableObject
    {
        [SerializeField] private List<NectoMonsterSkillEntry> monsters = new List<NectoMonsterSkillEntry>();

        public IReadOnlyList<NectoMonsterSkillEntry> Monsters => monsters;

        public NectoMonsterSkillEntry Find(string monsterId)
        {
            if (string.IsNullOrWhiteSpace(monsterId)) return null;
            return monsters.Find(monster => monster != null && monster.MonsterId == monsterId);
        }
    }

    [Serializable]
    public sealed class NectoMonsterSkillEntry
    {
        [Header("モンスター情報")]
        [SerializeField] private string monsterId;
        [SerializeField] private string displayName;

        [Header("モンスターのオブジェクト")]
        [SerializeField] private GameObject monsterPrefab;
        [SerializeField] private GameObject iconPrefab;

        [Header("このモンスターが使える技")]
        [Tooltip("このモンスターが使用できるデバッグ用スキル。")]
        [SerializeField] private List<NectoDebugSkillData> skills = new List<NectoDebugSkillData>();

        public string MonsterId => monsterId;
        public string DisplayName => displayName;
        public GameObject MonsterPrefab => monsterPrefab;
        public GameObject IconPrefab => iconPrefab;
        public IReadOnlyList<NectoDebugSkillData> Skills => skills;

        public NectoDebugSkillData FindSkillByCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return null;
            return skills.Find(skill => skill != null &&
                string.Equals(skill.Command, command, StringComparison.OrdinalIgnoreCase));
        }
    }
}
