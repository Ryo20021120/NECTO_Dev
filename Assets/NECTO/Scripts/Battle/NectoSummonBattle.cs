using System.Collections.Generic;
using UnityEngine;

namespace Necto.Battle
{
    /// <summary>
    /// 開発用プロジェクトで、UI表示中の入力ロックだけを共有する最小実装。
    /// 実戦闘は後から独立したシステムとして追加する。
    /// </summary>
    public sealed class NectoSummonBattle : MonoBehaviour
    {
        private static readonly HashSet<Object> MenuLockOwners = new HashSet<Object>();

        public static bool InputLocked => MenuLockOwners.Count > 0;
        public static bool BattleOpen => false;

        public static void SetMenuLock(Object owner, bool locked)
        {
            if (owner == null) return;
            if (locked) MenuLockOwners.Add(owner);
            else MenuLockOwners.Remove(owner);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            MenuLockOwners.Clear();
        }
    }
}
