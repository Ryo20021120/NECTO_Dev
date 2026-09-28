namespace Necto.Data
{
    // Thunderは既存アセットの序数を壊さないよう必ず末尾に追加すること。
    public enum NectoElement { None, Fire, Water, Wind, Earth, Thunder }

    public enum NectoStatus { None, Poison }

    public enum NectoCharacterCategory { Unknown, Player, Npc, Monster }

    public enum NectoAffiliation { None, Ally, Enemy, Box }

    public enum NectoSkillType { Basic, Command }

    public enum NectoImplState { NotStarted, InProgress, Done }

    /// <summary>マスターデータの日本語表記と内部enumの相互変換。</summary>
    public static class NectoMasterLabel
    {
        public static string ToJapanese(NectoElement element)
        {
            switch (element)
            {
                case NectoElement.Fire: return "炎";
                case NectoElement.Water: return "水";
                case NectoElement.Earth: return "土";
                case NectoElement.Wind: return "風";
                case NectoElement.Thunder: return "雷";
                default: return "無";
            }
        }

        public static string ToJapanese(NectoCharacterCategory category)
        {
            switch (category)
            {
                case NectoCharacterCategory.Player: return "プレイヤー";
                case NectoCharacterCategory.Npc: return "NPC";
                case NectoCharacterCategory.Monster: return "モンスター";
                default: return "";
            }
        }

        public static string ToJapanese(NectoAffiliation affiliation)
        {
            switch (affiliation)
            {
                case NectoAffiliation.Ally: return "味方";
                case NectoAffiliation.Enemy: return "敵";
                case NectoAffiliation.Box: return "ボックス";
                default: return "";
            }
        }

        public static string ToJapanese(NectoStatus status)
        {
            return status == NectoStatus.Poison ? "毒" : "なし";
        }

        public static string ToJapanese(NectoImplState state)
        {
            switch (state)
            {
                case NectoImplState.InProgress: return "作業中";
                case NectoImplState.Done: return "実装済み";
                default: return "未着手";
            }
        }

        public static NectoImplState ParseImplState(string value)
        {
            switch ((value ?? string.Empty).Trim())
            {
                case "作業中": return NectoImplState.InProgress;
                case "実装済み": return NectoImplState.Done;
                default: return NectoImplState.NotStarted;
            }
        }

        /// <summary>属性の書き戻し用。Noneは空文字ではなく「無」。</summary>
        public static string ToOptionalJapanese(NectoElement element)
        {
            return element == NectoElement.None ? "" : ToJapanese(element);
        }

        /// <summary>日本語・英語どちらの表記も受け付ける。未知の値はNoneを返す。</summary>
        public static NectoElement ParseElement(string value)
        {
            switch ((value ?? string.Empty).Trim())
            {
                case "炎": case "火": case "Fire": return NectoElement.Fire;
                case "水": case "Water": return NectoElement.Water;
                case "土": case "Earth": return NectoElement.Earth;
                case "風": case "Wind": return NectoElement.Wind;
                case "雷": case "Thunder": return NectoElement.Thunder;
                default: return NectoElement.None;
            }
        }

        public static NectoStatus ParseStatus(string value)
        {
            switch ((value ?? string.Empty).Trim())
            {
                case "毒": case "Poison": return NectoStatus.Poison;
                default: return NectoStatus.None;
            }
        }

        public static NectoAffiliation ParseAffiliation(string value)
        {
            switch ((value ?? string.Empty).Trim())
            {
                case "味方": case "Ally": return NectoAffiliation.Ally;
                case "敵": case "Enemy": return NectoAffiliation.Enemy;
                case "ボックス": case "Box": return NectoAffiliation.Box;
                default: return NectoAffiliation.None;
            }
        }

        public static NectoCharacterCategory ParseCategory(string value)
        {
            switch ((value ?? string.Empty).Trim())
            {
                case "プレイヤー": return NectoCharacterCategory.Player;
                case "NPC": return NectoCharacterCategory.Npc;
                case "モンスター": return NectoCharacterCategory.Monster;
                default: return NectoCharacterCategory.Unknown;
            }
        }

        public static NectoSkillType ParseSkillType(string value)
        {
            return (value ?? string.Empty).Trim() == "コマンド"
                ? NectoSkillType.Command
                : NectoSkillType.Basic;
        }
    }
}
