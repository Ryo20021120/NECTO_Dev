using Necto.Skills;
using UnityEngine;

namespace Necto.Data
{
    [CreateAssetMenu(menuName = "NECTO/Master Database", fileName = "NectoMasterDatabase")]
    public sealed class NectoMasterDatabase : ScriptableObject
    {
        public const string ResourcePath = "Data/NectoMasterDatabase";

        public NectoCharacterDatabase characters;
        public TextAsset charactersJson;
        public TextAsset skillsJson;
        public NectoSkillData[] skills = new NectoSkillData[0];

        private static NectoMasterDatabase current;

        public static NectoMasterDatabase Current
        {
            get
            {
                if (current == null) current = Resources.Load<NectoMasterDatabase>(ResourcePath);
                return current;
            }
        }

        public static void SetCurrent(NectoMasterDatabase database)
        {
            current = database;
        }

        public NectoSkillData FindSkill(string skillId)
        {
            if (string.IsNullOrEmpty(skillId) || skills == null) return null;
            foreach (NectoSkillData skill in skills)
                if (skill != null && (skill.skillId == skillId || skill.masterSkillId == skillId)) return skill;
            return null;
        }
    }
}
