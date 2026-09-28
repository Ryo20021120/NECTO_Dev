using UnityEngine;

namespace Necto.Skills
{
    /// <summary>
    /// デバッグ版で使う最小構成の技データ。
    /// JSONマスターとは連動しない。
    /// </summary>
    [CreateAssetMenu(menuName = "NECTO/Debug/Skill Data", fileName = "DebugSkill_New")]
    public sealed class NectoDebugSkillData : ScriptableObject
    {
        [Header("技の名称")]
        [SerializeField] private string displayName;

        [Header("技ID")]
        [Tooltip("AnimatorのStateMachineBehaviourに登録したIDと照合します。")]
        [SerializeField] private string skillId;

        [Header("入力コマンド")]
        [Tooltip("A、AA、YA、YYAのように入力します。")]
        [SerializeField] private string command;

        public string DisplayName => displayName;
        public string SkillId => skillId;
        public string Command => command;
    }
}
