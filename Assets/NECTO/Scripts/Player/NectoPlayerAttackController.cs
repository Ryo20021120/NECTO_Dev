using System.Text;
using Necto.Battle;
using Necto.Data;
using Necto.Skills;
using Necto.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Necto.Player
{
    /// <summary>
    /// A/Y/Bの入力列から、現在選択中のモンスターが持つ技を決定する。
    /// 技IDとStateMachineBehaviourを照合し、対応するAnimator Stateを直接再生する。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("NECTO/Player/Player Attack Controller")]
    public sealed class NectoPlayerAttackController : MonoBehaviour
    {
        [Header("デバッグ用データ")]
        [SerializeField] private NectoMonsterSkillDatabase monsterSkillDatabase;

        [Header("コマンド受付")]
        [Tooltip("次のボタン入力を待つ時間。")]
        [Min(0.05f)]
        [SerializeField] private float commandTimeout = 0.55f;

        [Header("Animator直接再生")]
        [Tooltip("攻撃ステートへ切り替える時間。")]
        [Min(0f)]
        [SerializeField] private float attackFadeDuration = 0.08f;

        [Tooltip("攻撃終了後に戻るステート。")]
        [SerializeField] private string locomotionStateName = "Base Layer.Locomotion";

        [Header("参照（通常は自動取得）")]
        [SerializeField] private Animator animator;

        [Header("検証用")]
        [Tooltip("攻撃入力からAnimator再生判定までをConsoleに表示します。")]
        [SerializeField] private bool showDebugLog = true;

        private readonly StringBuilder commandBuffer = new StringBuilder(4);
        private float lastInputTime = float.NegativeInfinity;
        private int playingAttackStateHash;
        private bool waitingForAttackState;
        private bool playingAttack;
        private string activeSkillId = string.Empty;
        private int queuedComboSteps;
        private NectoDebugSkillData queuedNextSkill;

        public string LastCommand { get; private set; } = string.Empty;
        public string LastSkillId { get; private set; } = string.Empty;
        public bool MovementLocked => playingAttack;

        private void Awake()
        {
            RefreshAnimator();
        }

        private void Update()
        {
            if (!IsAnimatorActive()) RefreshAnimator();

            UpdateDirectAttackPlayback();

            if (Time.unscaledTime - lastInputTime > commandTimeout)
                commandBuffer.Clear();

            if (NectoSummonBattle.InputLocked ||
                NectoFieldPartyHud.InputCaptured ||
                NectoPlayerDodgeController.IsDodging)
            {
                commandBuffer.Clear();
                return;
            }

            Gamepad gamepad = Gamepad.current;
            if (gamepad == null) return;

            if (UsesNintendoButtonLayout(gamepad))
            {
                // Nintendo Switch: 右=A / 下=B / 上=X / 左=Y
                if (gamepad.buttonEast.wasPressedThisFrame) PushCommand('A');
                if (gamepad.buttonSouth.wasPressedThisFrame) PushCommand('B');
                if (gamepad.buttonNorth.wasPressedThisFrame) PushCommand('X');
                if (gamepad.buttonWest.wasPressedThisFrame) PushCommand('Y');
            }
            else
            {
                // Xbox: 下=A / 右=B / 左=X / 上=Y
                if (gamepad.buttonSouth.wasPressedThisFrame) PushCommand('A');
                if (gamepad.buttonEast.wasPressedThisFrame) PushCommand('B');
                if (gamepad.buttonWest.wasPressedThisFrame) PushCommand('X');
                if (gamepad.buttonNorth.wasPressedThisFrame) PushCommand('Y');
            }
        }

        private static bool UsesNintendoButtonLayout(Gamepad gamepad)
        {
            if (gamepad == null) return false;

            string deviceText = string.Concat(
                gamepad.layout, " ",
                gamepad.displayName, " ",
                gamepad.description.manufacturer, " ",
                gamepad.description.product).ToLowerInvariant();

            return deviceText.Contains("nintendo") ||
                   deviceText.Contains("switch") ||
                   deviceText.Contains("joy-con") ||
                   deviceText.Contains("pro controller");
        }

        private void PushCommand(char button)
        {
            Log($"① 入力検出: {button}");

            lastInputTime = Time.unscaledTime;
            commandBuffer.Append(button);
            if (commandBuffer.Length > 4)
                commandBuffer.Remove(0, commandBuffer.Length - 4);

            Log($"② 現在の入力列: {commandBuffer}");

            NectoMonsterSkillEntry monster = GetActiveMonster();
            if (monster == null)
            {
                LogError("③ 失敗: 選択中のモンスターまたはデータが取得できません。");
                return;
            }

            Log($"③ モンスター取得成功: {NectoPartyRoster.ActiveMonsterId}");

            NectoDebugSkillData skill = FindLongestMatchingSuffix(monster);
            if (skill == null)
            {
                LogError($"④ 失敗: 入力 '{commandBuffer}' に一致する技がありません。");
                return;
            }

            LastCommand = skill.Command;
            LastSkillId = skill.SkillId;
            Log($"④ 技検索成功: {skill.DisplayName} / ID={skill.SkillId} / Command={skill.Command}");
            PlaySkill(skill);
        }

        private NectoMonsterSkillEntry GetActiveMonster()
        {
            if (monsterSkillDatabase == null)
            {
                Debug.LogWarning("NECTO: Player Attack ControllerにMonster Skill Databaseが登録されていません。", this);
                return null;
            }

            string monsterId = NectoPartyRoster.ActiveMonsterId;
            NectoMonsterSkillEntry monster = monsterSkillDatabase.Find(monsterId);
            if (monster == null)
                Debug.LogWarning($"NECTO: 選択中のモンスター '{monsterId}' がデバッグデータにありません。", this);
            return monster;
        }

        private NectoDebugSkillData FindLongestMatchingSuffix(NectoMonsterSkillEntry monster)
        {
            string entered = commandBuffer.ToString();
            NectoDebugSkillData best = null;

            foreach (NectoDebugSkillData skill in monster.Skills)
            {
                if (skill == null || string.IsNullOrWhiteSpace(skill.Command)) continue;
                if (!entered.EndsWith(skill.Command, System.StringComparison.OrdinalIgnoreCase)) continue;
                if (best == null || skill.Command.Length > best.Command.Length) best = skill;
            }

            return best;
        }

        private void PlaySkill(NectoDebugSkillData skill)
        {
            if (animator == null)
            {
                LogError("⑤ 失敗: 有効なAnimatorが見つかりません。");
                return;
            }

            if (string.IsNullOrWhiteSpace(skill.SkillId))
            {
                LogError("⑤ 失敗: Skill Dataの技IDが空です。");
                return;
            }

            Log($"⑤ Animator取得成功: {animator.name} / Controller={animator.runtimeAnimatorController?.name}");

            NectoSkillMasterStateSMB registeredBehaviour = FindRegisteredSkillBehaviour(skill.SkillId);
            bool hasRegisteredStateId = registeredBehaviour != null;
            Log($"⑥ StateMachineBehaviour ID照合: {skill.SkillId} = {(hasRegisteredStateId ? "一致" : "見つからない")}");
            if (!hasRegisteredStateId)
            {
                Debug.LogWarning($"NECTO: Animator内に技ID '{skill.SkillId}' のNectoSkillMasterStateSMBが見つかりません。", animator);
                return;
            }

            if (playingAttack && activeSkillId == skill.SkillId)
            {
                queuedComboSteps = Mathf.Min(queuedComboSteps + 1, 2);
                Log($"⑧ コンボ先行入力受付: {queuedComboSteps}回分");
                commandBuffer.Clear();
                return;
            }

            if (playingAttack)
            {
                // 別技は現在のモーションを中断せず、攻撃終了後に1つだけ発動する。
                // 攻撃中にさらに別コマンドを完成させた場合は、最後に入力した技で予約を更新する。
                queuedNextSkill = skill;
                Log($"⑧ 次の技を予約: {skill.DisplayName} / ID={skill.SkillId}");
                commandBuffer.Clear();
                return;
            }

            string stateName = registeredBehaviour.AnimatorStateName;
            if (string.IsNullOrWhiteSpace(stateName))
            {
                LogError($"⑦ 再生失敗: ID '{skill.SkillId}' のBehaviourにAnimator State Nameが未入力です。");
                return;
            }

            int stateHash = Animator.StringToHash(stateName);
            if (!animator.HasState(0, stateHash))
            {
                LogError($"⑦ 再生失敗: Animatorにステート '{stateName}' が見つかりません。");
                return;
            }

            activeSkillId = skill.SkillId;
            queuedComboSteps = 0;
            Log($"⑦ 技の開始Stateを再生: {stateName}");
            animator.CrossFadeInFixedTime(stateHash, attackFadeDuration, 0, 0f);
            playingAttackStateHash = stateHash;
            waitingForAttackState = true;
            playingAttack = true;
            commandBuffer.Clear();
        }

        private void UpdateDirectAttackPlayback()
        {
            if (!playingAttack || animator == null) return;

            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
            AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);

            if (waitingForAttackState)
            {
                if (current.fullPathHash == playingAttackStateHash || next.fullPathHash == playingAttackStateHash)
                    waitingForAttackState = false;
                return;
            }

            // 次段への切り替えはAnimator TransitionとNectoComboInputStateSMBが担当する。
            if (animator.IsInTransition(0) || current.normalizedTime < 0.98f)
                return;

            NectoDebugSkillData nextSkill = queuedNextSkill;
            queuedNextSkill = null;
            playingAttack = false;
            waitingForAttackState = false;
            activeSkillId = string.Empty;
            queuedComboSteps = 0;

            if (nextSkill != null)
            {
                Log($"⑨ 現在の攻撃終了。予約技を開始: {nextSkill.DisplayName}");
                PlaySkill(nextSkill);
                return;
            }

            int locomotionHash = Animator.StringToHash(locomotionStateName);
            if (animator.HasState(0, locomotionHash))
                animator.CrossFadeInFixedTime(locomotionHash, attackFadeDuration, 0, 0f);
            else
                LogError($"攻撃終了後のステート '{locomotionStateName}' が見つかりません。");
        }

        /// <summary>Animatorのコンボ受付Behaviourから呼び出し、1回分の先行入力を消費する。</summary>
        public bool TryConsumeQueuedComboInput()
        {
            if (!playingAttack || queuedComboSteps <= 0) return false;

            queuedComboSteps--;
            Log($"コンボ入力をAnimator Transitionへ渡しました。残り予約={queuedComboSteps}");
            return true;
        }

        private void Log(string message)
        {
            if (showDebugLog) Debug.Log($"[NECTO Attack Test] {message}", this);
        }

        private void LogError(string message)
        {
            if (showDebugLog) Debug.LogError($"[NECTO Attack Test] {message}", this);
        }

        private NectoSkillMasterStateSMB FindRegisteredSkillBehaviour(string skillId)
        {
            foreach (NectoSkillMasterStateSMB behaviour in animator.GetBehaviours<NectoSkillMasterStateSMB>())
                if (behaviour != null && behaviour.SkillId == skillId) return behaviour;
            return null;
        }

        public void RefreshAnimator()
        {
            animator = null;
            foreach (Animator candidate in GetComponentsInChildren<Animator>(true))
            {
                if (!candidate.enabled || !candidate.gameObject.activeInHierarchy) continue;
                animator = candidate;
                break;
            }
        }

        private bool IsAnimatorActive()
        {
            return animator != null && animator.enabled && animator.gameObject.activeInHierarchy;
        }

        private void OnValidate()
        {
            commandTimeout = Mathf.Max(0.05f, commandTimeout);
            attackFadeDuration = Mathf.Max(0f, attackFadeDuration);
        }
    }
}
