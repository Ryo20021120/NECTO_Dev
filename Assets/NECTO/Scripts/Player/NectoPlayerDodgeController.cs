using System.Collections;
using Necto.Input;
using Necto.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Necto.Player
{
    /// <summary>Rボタンで緊急回避し、回避中の移動とアニメーションを管理する。</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    [AddComponentMenu("NECTO/Player/Player Dodge Controller")]
    public sealed class NectoPlayerDodgeController : MonoBehaviour
    {
        [Header("回避")]
        [Min(0.1f)] [SerializeField] private float dodgeDistance = 4f;
        [Min(0.05f)] [SerializeField] private float dodgeDuration = 0.45f;
        [Min(0f)] [SerializeField] private float cooldown = 0.3f;
        [Min(0f)] [SerializeField] private float rotationSpeed = 24f;

        [Header("Animatorステート")]
        [SerializeField] private string dodgeStateName = "Base Layer.Dodge";
        [SerializeField] private string locomotionStateName = "Base Layer.Locamotion";
        [Min(0f)] [SerializeField] private float transitionDuration = 0.06f;

        [Header("参照（通常は自動取得）")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Animator animator;

        private CharacterController characterController;
        private float nextDodgeTime;
        private Coroutine dodgeRoutine;

        public static bool IsDodging { get; private set; }
        public bool IsInvulnerable => IsDodging;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
            RefreshAnimator();
        }

        private void Update()
        {
            if (!IsAnimatorActive()) RefreshAnimator();
            if (NectoFieldPartyHud.InputCaptured || IsDodging || Time.time < nextDodgeTime) return;

            Gamepad gamepad = Gamepad.current;
            Keyboard keyboard = Keyboard.current;
            bool pressed = (gamepad != null && gamepad.rightShoulder.wasPressedThisFrame)
                || (keyboard != null && keyboard.leftShiftKey.wasPressedThisFrame);

            if (pressed) BeginDodge(gamepad);
        }

        private void BeginDodge(Gamepad gamepad)
        {
            Vector2 input = NectoGamepadInput.ReadLeftStick(gamepad);
            Vector3 direction = GetWorldDirection(input);
            if (direction.sqrMagnitude < 0.001f) direction = transform.forward;
            direction.y = 0f;
            direction.Normalize();

            if (dodgeRoutine != null) StopCoroutine(dodgeRoutine);
            dodgeRoutine = StartCoroutine(DodgeRoutine(direction));
        }

        private IEnumerator DodgeRoutine(Vector3 direction)
        {
            IsDodging = true;
            nextDodgeTime = Time.time + dodgeDuration + cooldown;

            if (animator != null)
                animator.CrossFadeInFixedTime(dodgeStateName, transitionDuration);

            float elapsed = 0f;
            float speed = dodgeDistance / dodgeDuration;
            while (elapsed < dodgeDuration)
            {
                elapsed += Time.deltaTime;

                if (rotationSpeed > 0f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                    float blend = 1f - Mathf.Exp(-rotationSpeed * Time.deltaTime);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, blend);
                }

                if (characterController != null && characterController.enabled)
                    characterController.Move(direction * speed * Time.deltaTime);

                yield return null;
            }

            if (animator != null && animator.enabled)
                animator.CrossFadeInFixedTime(locomotionStateName, transitionDuration);

            IsDodging = false;
            dodgeRoutine = null;
        }

        private Vector3 GetWorldDirection(Vector2 input)
        {
            if (input.sqrMagnitude < 0.001f) return transform.forward;

            Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            return Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
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

        private void OnDisable()
        {
            IsDodging = false;
            dodgeRoutine = null;
        }

        private void OnValidate()
        {
            dodgeDistance = Mathf.Max(0.1f, dodgeDistance);
            dodgeDuration = Mathf.Max(0.05f, dodgeDuration);
            cooldown = Mathf.Max(0f, cooldown);
            rotationSpeed = Mathf.Max(0f, rotationSpeed);
            transitionDuration = Mathf.Max(0f, transitionDuration);
        }
    }
}
