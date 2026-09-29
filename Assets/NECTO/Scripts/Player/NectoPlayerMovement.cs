using Necto.Battle;
using Necto.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using Necto.UI;

namespace Necto.Player
{
    /// <summary>カメラ基準でキャラクターを移動させ、移動アニメーションを更新する。</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class NectoPlayerMovement : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");
        private static readonly int GroundedHash = Animator.StringToHash("Grounded");

        [Header("移動")]
        [Min(0f)]
        [SerializeField] private float moveSpeed = 4.5f;
        [Min(0f)]
        [SerializeField] private float rotationSpeed = 12f;

        [Header("重力")]
        [SerializeField] private float gravity = -25f;
        [SerializeField] private float groundedForce = -2f;

        [Header("参照（通常は自動取得）")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Animator animator;

        private CharacterController characterController;
        private NectoPlayerAttackController attackController;
        private float verticalVelocity;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            attackController = GetComponent<NectoPlayerAttackController>();

            RefreshAnimator();

            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            if (animator != null)
                animator.applyRootMotion = true;
        }

        private void Update()
        {
            if (!IsAnimatorActive())
                RefreshAnimator();

            Vector2 moveInput = ReadMoveInput();
            Vector3 moveDirection = GetCameraRelativeDirection(moveInput);

            if (moveDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                float rotationBlend = rotationSpeed <= 0f
                    ? 1f
                    : 1f - Mathf.Exp(-rotationSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationBlend);
            }

            bool isGrounded = characterController.isGrounded;
            if (isGrounded && verticalVelocity < 0f)
                verticalVelocity = groundedForce;
            else
                verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = moveDirection * moveSpeed;
            velocity.y = verticalVelocity;
            characterController.Move(velocity * Time.deltaTime);

            UpdateAnimator(moveInput, isGrounded);
        }

        private Vector2 ReadMoveInput()
        {
            if (NectoSummonBattle.InputLocked ||
                NectoFieldPartyHud.InputCaptured ||
                NectoPlayerDodgeController.IsDodging ||
                (attackController != null && attackController.MovementLocked))
                return Vector2.zero;

            Vector2 input = NectoGamepadInput.ReadLeftStick(Gamepad.current);

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return input;

            Vector2 keyboardInput = Vector2.zero;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) keyboardInput.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) keyboardInput.y -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) keyboardInput.x += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) keyboardInput.x -= 1f;

            if (keyboardInput.sqrMagnitude > 0f)
                input = Vector2.ClampMagnitude(keyboardInput, 1f);

            return input;
        }

        private Vector3 GetCameraRelativeDirection(Vector2 input)
        {
            Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            return Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
        }

        private void UpdateAnimator(Vector2 input, bool isGrounded)
        {
            if (animator == null) return;

            float speed = Mathf.Clamp01(input.magnitude);
            animator.SetFloat(SpeedHash, speed, 0.1f, Time.deltaTime);
            animator.SetBool(IsMovingHash, speed > 0.01f);
            animator.SetFloat(MoveXHash, input.x, 0.1f, Time.deltaTime);
            animator.SetFloat(MoveYHash, input.y, 0.1f, Time.deltaTime);
            animator.SetBool(GroundedHash, isGrounded);
        }

        /// <summary>キャラチェンジ後、現在表示中のAnimatorへ接続し直す。</summary>
        public void RefreshAnimator()
        {
            Animator nextAnimator = null;
            foreach (Animator candidate in GetComponentsInChildren<Animator>(true))
            {
                if (!candidate.enabled || !candidate.gameObject.activeInHierarchy) continue;
                nextAnimator = candidate;
                break;
            }

            animator = nextAnimator;
            if (animator != null)
            {
                NectoInPlaceRootMotion inPlace =
                    animator.GetComponent<NectoInPlaceRootMotion>();
                if (inPlace == null)
                    animator.gameObject.AddComponent<NectoInPlaceRootMotion>();
                animator.applyRootMotion = true;
            }
        }

        private bool IsAnimatorActive()
        {
            return animator != null && animator.enabled && animator.gameObject.activeInHierarchy;
        }

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0f, moveSpeed);
            rotationSpeed = Mathf.Max(0f, rotationSpeed);
            groundedForce = Mathf.Min(0f, groundedForce);
            gravity = Mathf.Min(0f, gravity);
        }
    }
}
