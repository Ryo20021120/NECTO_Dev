using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Necto.Data
{
    /// <summary>
    /// Data/Master/characters.json から焼き込まれるキャラクターデータ。
    /// NECTO/Editor/NectoCharacterMasterImporter が生成するので手で編集しないこと。
    /// リストの並び順はマスタの並び順をそのまま保持する。
    /// </summary>
    public sealed class NectoCharacterDatabase : ScriptableObject
    {
        public const string ResourcePath = "Data/NectoCharacterDatabase";

        public string masterVersion;
        public string importedAt;

        [Header("プレイアブルキャラ")]
        public List<NectoCharacterEntry> players = new List<NectoCharacterEntry>();

        [Header("NPC")]
        public List<NectoCharacterEntry> npcs = new List<NectoCharacterEntry>();

        [Header("モンスター")]
        public List<NectoCharacterEntry> monsters = new List<NectoCharacterEntry>();

        [Header("区分なし")]
        [Tooltip("マスタの区分が未設定・不明なもの。通常は空。")]
        public List<NectoCharacterEntry> others = new List<NectoCharacterEntry>();

        /// <summary>区分をまたいだ全件。</summary>
        public IEnumerable<NectoCharacterEntry> All =>
            players.Concat(npcs).Concat(monsters).Concat(others);

        private static NectoCharacterDatabase cached;
        private static bool warned;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            cached = null;
            warned = false;
        }

        public static NectoCharacterDatabase Load()
        {
            if (cached != null) return cached;

            // Runtime master is the single source of truth for prefab/iconPrefab references.
            // Keep the Resources asset only as a compatibility fallback for old scenes.
            NectoMasterDatabase master = NectoMasterDatabase.Current;
            cached = master != null ? master.characters : null;
            if (cached == null)
                cached = Resources.Load<NectoCharacterDatabase>(ResourcePath);
            if (cached == null && !warned)
            {
                warned = true;
                Debug.LogWarning(
                    "NECTO: キャラクターデータベースが見つかりません。" +
                    "Data/Master/characters.json から取り込み直してください。");
            }
            return cached;
        }

        public NectoCharacterEntry Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            // Inspector で空行が追加されると null が混ざりうる。起動時に必ず通るので弾いておく。
            return All.FirstOrDefault(entry => entry != null && entry.id == id);
        }

        /// <summary>パーティに編成できるか。味方とボックスの有効なキャラのみ。</summary>
        public bool IsPartyEligible(string id)
        {
            NectoCharacterEntry entry = Find(id);
            return entry != null && entry.enabled
                && (entry.affiliation == NectoAffiliation.Ally || entry.affiliation == NectoAffiliation.Box);
        }

        /// <summary>所属で絞り込む。</summary>
        public IEnumerable<NectoCharacterEntry> ByAffiliation(NectoAffiliation affiliation)
        {
            return All.Where(entry => entry.enabled && entry.affiliation == affiliation);
        }

        /// <summary>所属=味方を枠順に並べる。枠未設定は末尾。</summary>
        public IEnumerable<NectoCharacterEntry> OrderedAllies()
        {
            return ByAffiliation(NectoAffiliation.Ally)
                .OrderBy(entry => entry.partySlot <= 0 ? int.MaxValue : entry.partySlot);
        }
    }
}
