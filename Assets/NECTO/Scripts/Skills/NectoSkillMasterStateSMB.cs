using UnityEngine;
using UnityEngine.Serialization;

namespace Necto.Skills
{
    /// <summary>
    /// Animator Stateと技データをIDで関連付けるためのメタデータ。
    /// 攻撃制御は行わず、技IDと制作用メモだけを保持する。
    /// </summary>
    public sealed class NectoSkillMasterStateSMB : StateMachineBehaviour
    {
        [Header("技ID")]
        [Tooltip("スキルScriptableObjectの技IDと同じ値を入れます。例: skill_002")]
        [FormerlySerializedAs("masterSkillId")]
        [SerializeField] private string skillId = "";

        [Tooltip("このステートが表す技や用途を記録する説明欄です。")]
        [TextArea(2, 5)]
        [SerializeField] private string description = "";

        [Header("技の開始State")]
        [Tooltip("例: Base Layer.通常攻撃01.Attack_Normal_01_01")]
        [SerializeField] private string animatorStateName = "";

        public string SkillId => skillId;
        public string MasterSkillId => skillId;
        public string Description => description;
        public string AnimatorStateName => animatorStateName;
    }
}
