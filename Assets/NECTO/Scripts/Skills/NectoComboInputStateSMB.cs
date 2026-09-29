using Necto.Player;
using UnityEngine;

namespace Necto.Skills
{
    /// <summary>
    /// 現在の攻撃ステートが入力受付中のとき、
    /// Player Attack Controllerが保持する先行入力をAnimator Transitionへ渡す。
    /// </summary>
    public sealed class NectoComboInputStateSMB : StateMachineBehaviour
    {
        [Header("次段入力の受付範囲")]
        [Range(0f, 1f)]
        [SerializeField] private float inputWindowStart = 0.2f;

        [Range(0f, 1f)]
        [SerializeField] private float inputWindowEnd = 0.8f;

        [Header("Animator Transition")]
        [SerializeField] private string nextAttackTrigger = "ComboNext";

        private bool triggerSent;
        private NectoPlayerAttackController attackController;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            triggerSent = false;
            animator.ResetTrigger(nextAttackTrigger);
            attackController = animator.GetComponentInParent<NectoPlayerAttackController>();
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (triggerSent || attackController == null) return;

            float normalizedTime = stateInfo.normalizedTime % 1f;
            if (normalizedTime < inputWindowStart || normalizedTime > inputWindowEnd) return;
            if (!attackController.TryConsumeQueuedComboInput()) return;

            triggerSent = true;
            animator.SetTrigger(nextAttackTrigger);
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            animator.ResetTrigger(nextAttackTrigger);
            triggerSent = false;
            attackController = null;
        }

        private void OnValidate()
        {
            inputWindowStart = Mathf.Clamp01(inputWindowStart);
            inputWindowEnd = Mathf.Clamp(inputWindowEnd, inputWindowStart, 1f);
        }
    }
}
