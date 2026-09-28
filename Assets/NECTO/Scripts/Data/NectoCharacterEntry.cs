using UnityEngine;

namespace Necto.Data
{
    /// <summary>
    /// Data/Master/characters.json の1件をゲーム用に変換したもの。
    /// NectoCharacterDatabase にまとめて焼き込まれる。手で編集せず、取り込みで再生成する。
    /// </summary>
    [System.Serializable]
    public sealed class NectoCharacterEntry
    {
        [Header("識別")]
        // Inspector のリスト見出しに先頭フィールドが使われるため、名前を先に置く。
        public string displayName;
        public string id;
        [TextArea(1, 3)] public string description;

        [Header("分類")]
        public NectoCharacterCategory category;
        public NectoAffiliation affiliation;
        [Tooltip("味方の並び順。1〜3。0は未設定。")]
        public int partySlot;

        [Header("属性")]
        public NectoElement element;
        public NectoElement resistance;
        public NectoElement weakness;
        public NectoStatus statusSkill;

        [Header("ステータス")]
        public int level;
        public int maxHp;
        public int maxMp;
        public int attack;
        public int defense;
        [Tooltip("戦闘の行動順。フィールド移動速度とは別。")]
        public int battleSpeed;
        [Tooltip("フィールドの移動速度。")]
        public float moveSpeed;

        [Header("報酬")]
        public int exp;
        public int gold;
        public string[] dropItems = new string[0];

        [Header("技")]
        public string[] skillIds = new string[0];

        [Header("アセット")]
        public string prefabName;
        public string modelName;
        public string avatarName;
        public string animatorControllerName;
        [Tooltip("prefabName から取り込み時に解決される。")]
        public GameObject prefab;
        [Tooltip("animatorControllerName から取り込み時に解決される。")]
        public RuntimeAnimatorController animatorController;
        [Tooltip("一覧UIに表示する完成済みUIプレハブ。背景とキャラクター画像をまとめて保持します。")]
        public GameObject iconPrefab;

        [Header("状態")]
        public NectoImplState implState;
        public bool enabled = true;
        public string spawnLocation;
        [TextArea(1, 3)] public string notes;
    }
}
