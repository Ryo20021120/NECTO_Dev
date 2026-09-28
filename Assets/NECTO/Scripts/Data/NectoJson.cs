using System;
using System.Collections.Generic;
using UnityEngine;

namespace Necto.Data
{
    /// <summary>
    /// マスターデータJSONの読み込みヘルパー。
    /// JsonUtilityはトップレベル配列を解析できないため、{"items":[...]}で包んでから渡す。
    /// </summary>
    public static class NectoJson
    {
        [Serializable]
        private sealed class Wrapper<T>
        {
            public T[] items;
        }

        public static List<T> FromJsonArray<T>(string json, string sourceLabel = "")
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<T>();

            // BOM・ゼロ幅スペースが先頭に混ざるとJsonUtilityが失敗する。
            string trimmed = json.TrimStart('\uFEFF', '\u200B').Trim();
            if (trimmed.Length == 0) return new List<T>();

            if (trimmed[0] != '[')
            {
                Debug.LogError($"NECTO: JSONのトップレベルが配列ではありません。{sourceLabel}");
                return new List<T>();
            }

            try
            {
                Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>("{\"items\":" + trimmed + "}");
                return wrapper?.items != null ? new List<T>(wrapper.items) : new List<T>();
            }
            catch (Exception exception)
            {
                Debug.LogError($"NECTO: JSONの解析に失敗しました。{sourceLabel} / {exception.Message}");
                return new List<T>();
            }
        }
    }
}
