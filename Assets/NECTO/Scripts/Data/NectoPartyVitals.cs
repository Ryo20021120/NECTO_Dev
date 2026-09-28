using UnityEngine;

namespace Necto.Data
{
    /// <summary>召喚戦・憑依戦・パーティ画面で共有する仲間モンスターの現在HP/MP。</summary>
    public static class NectoPartyVitals
    {
        public static int GetHp(string id, int maxHp) => Mathf.Clamp(PlayerPrefs.GetInt(Key(id, "HP"), maxHp), 0, maxHp);
        public static int GetMp(string id, int maxMp) => Mathf.Clamp(PlayerPrefs.GetInt(Key(id, "MP"), maxMp), 0, maxMp);

        public static void Set(string id, int hp, int mp)
        {
            if (string.IsNullOrEmpty(id)) return;
            PlayerPrefs.SetInt(Key(id, "HP"), Mathf.Max(0, hp));
            PlayerPrefs.SetInt(Key(id, "MP"), Mathf.Max(0, mp));
            PlayerPrefs.Save();
        }

        private static string Key(string id, string stat) => $"NECTO.PartyVitals.{id}.{stat}";
    }
}
