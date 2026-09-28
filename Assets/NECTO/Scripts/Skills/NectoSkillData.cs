using System;
using System.Collections.Generic;
using Necto.Data;
using Necto.Effects;
using UnityEngine;
using UnityEngine.Timeline;

namespace Necto.Skills
{
    [Serializable]
    public sealed class NectoVfxEventConfig
    {
        public string eventId = "Main";
        public GameObject prefab;
        public NectoEffectPointType anchor = NectoEffectPointType.RightHand;
        public bool follow = true;
        public Vector3 positionOffset = Vector3.zero;
        public Vector3 rotationOffset = Vector3.zero;
        public Vector3 scale = Vector3.one;
        [Min(0.05f)] public float lifetime = 2f;
        public bool stopOnStateExit = true;
        public bool allowMultiple = false;
    }

    [CreateAssetMenu(menuName = "NECTO/Skill Data", fileName = "Skill_New")]
    public sealed class NectoSkillData : ScriptableObject
    {
        [Header("Identity / 基本情報")]
        public string skillId = "attack_normal_a";
        public string displayName = "通常攻撃A";

        [Header("Input / 入力")]
        [Tooltip("A、YA、YYAのように入力します")]
        public string command = "A";

        [Header("Battle / 戦闘値")]
        [Min(0)] public int power = 10;
        [Min(0)] public int mpCost;

        [Header("Presentation / 演出")]
        public TimelineAsset timeline;
        public AnimationClip animationClip;
        public GameObject effectPrefab;
        public NectoEffectPointType effectPoint = NectoEffectPointType.RightHand;

        [Header("VFX Events / エフェクトイベント設定")]
        public List<NectoVfxEventConfig> vfxEvents = new List<NectoVfxEventConfig>();

        [Header("Animator")]
        [Tooltip("Player_Main.controller 内での State パス。例: \"Base Layer.Monster Skills.Skill_001_水斬撃\"")]
        public string animatorStatePath = "";
        [Min(0.05f)] public float transitionDurationTime = 0.1f;

        [Header("Return / 終了")]
        public string returnState = "Idle";

        [Header("Master data / マスターデータ連携")]
        [Tooltip("Data/Master/skills.json の id。空欄なら取り込み対象外。")]
        public string masterSkillId = "";
        [Tooltip("motion1/2/3 のどの段に対応するか。0始まり。")]
        [Min(0)] public int comboIndex;

        [Header("Master mirror / マスター側の写し（取り込みで上書きされる）")]
        [TextArea(1, 3)] public string description = "";
        public NectoElement element = NectoElement.None;
        public NectoSkillType skillType = NectoSkillType.Basic;
        public string buff = "";
        public string debuff = "";
        [Tooltip("マスターが指定するAnimation Clip名。参照の自動解決はしない。")]
        public string motionName = "";
        public bool masterEnabled = true;
        [TextArea(1, 3)] public string notes = "";

        public NectoVfxEventConfig GetVfxConfig(string eventId)
        {
            if (string.IsNullOrEmpty(eventId) || vfxEvents == null) return null;
            return vfxEvents.Find(cfg => cfg != null && cfg.eventId == eventId);
        }
    }
}
