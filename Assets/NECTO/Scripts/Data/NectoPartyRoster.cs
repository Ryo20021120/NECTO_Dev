using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Necto.Data
{
    public static class NectoPartyRoster
    {
        private static List<string> partyIds;
        private static List<string> boxIds;
        private static PlayerData saveData;

        public static event Action<string> ActiveMonsterChanged;

        public static IReadOnlyList<string> PartyIds
        {
            get { EnsureLoaded(); return partyIds; }
        }

        public static IReadOnlyList<string> BoxIds
        {
            get { EnsureLoaded(); return boxIds; }
        }

        public static bool IsInParty(string id) { EnsureLoaded(); return partyIds.Contains(id); }

        /// <summary>フィールドで現在力を借りているモンスター。</summary>
        public static string ActiveMonsterId
        {
            get
            {
                EnsureLoaded();
                return partyIds.Contains(saveData.activeMonsterId)
                    ? saveData.activeMonsterId
                    : (partyIds.Count > 0 ? partyIds[0] : string.Empty);
            }
        }

        public static void SetActiveMonster(string id)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(id) || !partyIds.Contains(id) || id == ActiveMonsterId) return;

            saveData.activeMonsterId = id;
            SaveRoster();
            ActiveMonsterChanged?.Invoke(id);
        }

        public static void Swap(string outgoingId, string incomingId)
        {
            EnsureLoaded();
            int index = partyIds.IndexOf(outgoingId);
            if (index < 0 || string.IsNullOrEmpty(incomingId) || partyIds.Contains(incomingId)) return;
            bool replacedActiveMonster = ActiveMonsterId == outgoingId;
            partyIds[index] = incomingId;
            boxIds.Remove(incomingId);
            if (!boxIds.Contains(outgoingId)) boxIds.Add(outgoingId);
            if (replacedActiveMonster) saveData.activeMonsterId = incomingId;
            SaveRoster();
        }

        private static void EnsureLoaded()
        {
            if (partyIds != null && boxIds != null) return;
            saveData = PlayerDataManager.Load();
            partyIds = saveData.partyMonsterIds
                .Where(id => !string.IsNullOrEmpty(id)).Distinct().Take(3).ToList();
            boxIds = saveData.boxMonsterIds
                .Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();

            // マスタ側でIDが変わると、保存済みの編成が存在しないIDを指したままになる。
            // 見つからないものは捨てて既定パーティへ戻す。
            NectoCharacterDatabase database = NectoCharacterDatabase.Load();
            if (database != null) partyIds.RemoveAll(id => !database.IsPartyEligible(id));

            if (partyIds.Count == 0) partyIds = LoadDefaults();
            if (partyIds.Count == 0) partyIds.Add("monster_001");

            if (boxIds.Count == 0 && database != null)
                boxIds = database.monsters
                    .Where(entry => entry != null && entry.enabled && !partyIds.Contains(entry.id))
                    .Select(entry => entry.id)
                    .Distinct()
                    .ToList();

            if (!partyIds.Contains(saveData.activeMonsterId))
                saveData.activeMonsterId = partyIds.Count > 0 ? partyIds[0] : string.Empty;

            SaveRoster();
        }

        private static List<string> LoadDefaults()
        {
            NectoCharacterDatabase database = NectoCharacterDatabase.Load();
            if (database == null) return new List<string>();
            // 枠（partySlot）が既定パーティの並び順。マスタの行順に依存させない。
            return database.OrderedAllies().Take(3).Select(entry => entry.id).ToList();
        }

        private static void SaveRoster()
        {
            PlayerData latest = PlayerDataManager.Load();
            if (saveData != null) latest.activeMonsterId = saveData.activeMonsterId;
            latest.partyMonsterIds = new List<string>(partyIds ?? new List<string>());
            latest.boxMonsterIds = new List<string>(boxIds ?? new List<string>());
            PlayerDataManager.Save(latest);
            saveData = latest;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            partyIds = null;
            boxIds = null;
            saveData = null;
        }
    }
}
