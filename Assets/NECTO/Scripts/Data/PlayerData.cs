using System;
using System.Collections.Generic;
using UnityEngine;

namespace Necto.Data
{
    /// <summary>プレイヤーの現在のキャラクター選択など、永続的に保存するデータ。</summary>
    [System.Serializable]
    public class PlayerData
    {
        public string selectedCharacterId = "player_001";
        public string activeMonsterId = "monster_001";
        public List<string> partyMonsterIds = new List<string>();
        public List<string> boxMonsterIds = new List<string>();
        public long lastUpdated;

        public PlayerData()
        {
            lastUpdated = DateTime.Now.Ticks;
        }
    }

    /// <summary>PlayerData の保存・読み込みを管理。</summary>
    public static class PlayerDataManager
    {
        private static string SavePath => System.IO.Path.Combine(Application.persistentDataPath, "PlayerData.json");

        public static PlayerData Load()
        {
            try
            {
                if (System.IO.File.Exists(SavePath))
                {
                    string json = System.IO.File.ReadAllText(SavePath);
                    PlayerData loaded = JsonUtility.FromJson<PlayerData>(json) ?? new PlayerData();
                    Normalize(loaded);
                    return loaded;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load PlayerData: {e.Message}");
            }
            PlayerData created = new PlayerData();
            Normalize(created);
            return created;
        }

        public static void Save(PlayerData data)
        {
            try
            {
                Normalize(data);
                data.lastUpdated = DateTime.Now.Ticks;
                string json = JsonUtility.ToJson(data, true);
                System.IO.File.WriteAllText(SavePath, json);
                Debug.Log($"PlayerData saved: {SavePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save PlayerData: {e.Message}");
            }
        }

        private static void Normalize(PlayerData data)
        {
            if (data == null) return;
            if (string.IsNullOrEmpty(data.selectedCharacterId)) data.selectedCharacterId = "player_001";
            if (data.partyMonsterIds == null) data.partyMonsterIds = new List<string>();
            if (data.boxMonsterIds == null) data.boxMonsterIds = new List<string>();
        }
    }
}
