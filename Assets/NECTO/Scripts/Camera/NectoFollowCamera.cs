using UnityEngine;
using UnityEngine.InputSystem;
using Necto.Input;

namespace Necto.CameraSystem
{
    /// <summary>
    /// キャラクターとの距離を保ちながら、遅れて滑らかに追従するカメラ。
    /// target が未指定の場合は Player タグのオブジェクトを自動検出する。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class NectoFollowCamera : MonoBehaviour
    {
        [Header("追従対象")]
        [SerializeField] private Transform target;
        [SerializeField] private bool findPlayerAutomatically = true;

        [Header("構図")]
        [SerializeField] private Vector3 positionOffset = new Vector3(0f, 4.5f, -7f);
        [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 1.5f, 0f);

        [Header("右スティックカメラ")]
        [Min(0f)]
        [SerializeField] private float orbitSpeed = 120f;
        [SerializeField] private float minimumPitch = 5f;
        [SerializeField] private float maximumPitch = 65f;
        [SerializeField] private bool invertVertical;

        [Header("追従の滑らかさ")]
        [Min(0f)]
        [SerializeField] private float positionSmoothTime = 0.18f;
        [Min(0f)]
        [SerializeField] private float rotationSharpness = 12f;

        private Vector3 followVelocity;
        private float orbitYaw;
        private float orbitPitch;
        private float orbitDistance;
        private bool orbitInitialized;

        public Transform Target => target;

        private void Awake()
        {
            TryFindPlayer();
        }

        private void OnEnable()
        {
            followVelocity = Vector3.zero;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                TryFindPlayer();
                if (target == null) return;
            }

            UpdateOrbitInput();
            Vector3 orbitOffset = Quaternion.Euler(orbitPitch, orbitYaw, 0f)
                * Vector3.back * orbitDistance;
            Vector3 desiredPosition = target.position + orbitOffset;
            transform.position = positionSmoothTime <= 0f
                ? desiredPosition
                : Vector3.SmoothDamp(
                    transform.position,
                    desiredPosition,
                    ref followVelocity,
                    positionSmoothTime);

            Vector3 lookDirection = target.position + lookAtOffset - transform.position;
            if (lookDirection.sqrMagnitude <= Mathf.Epsilon) return;

            Quaternion desiredRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            float rotationBlend = rotationSharpness <= 0f
                ? 1f
                : 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationBlend);
        }

        /// <summary>生成したキャラクターなどを追従対象に設定する。</summary>
        public void SetTarget(Transform newTarget, bool snapImmediately = true)
        {
            target = newTarget;
            followVelocity = Vector3.zero;
            InitializeOrbit();

            if (snapImmediately && target != null)
            {
                Vector3 orbitOffset = Quaternion.Euler(orbitPitch, orbitYaw, 0f)
                    * Vector3.back * orbitDistance;
                transform.position = target.position + orbitOffset;
                transform.LookAt(target.position + lookAtOffset);
            }
        }

        private void InitializeOrbit()
        {
            Vector3 offset = positionOffset;
            orbitDistance = Mathf.Max(0.1f, offset.magnitude);
            float horizontalDistance = new Vector2(offset.x, offset.z).magnitude;
            orbitYaw = Mathf.Atan2(-offset.x, -offset.z) * Mathf.Rad2Deg;
            orbitPitch = Mathf.Atan2(offset.y, horizontalDistance) * Mathf.Rad2Deg;
            orbitPitch = Mathf.Clamp(orbitPitch, minimumPitch, maximumPitch);
            orbitInitialized = true;
        }

        private void UpdateOrbitInput()
        {
            if (!orbitInitialized) InitializeOrbit();

            Vector2 input = NectoGamepadInput.ReadRightStick(Gamepad.current);
            orbitYaw += input.x * orbitSpeed * Time.deltaTime;
            float verticalDirection = invertVertical ? 1f : -1f;
            orbitPitch = Mathf.Clamp(
                orbitPitch + input.y * orbitSpeed * verticalDirection * Time.deltaTime,
                minimumPitch,
                maximumPitch);
        }

        private void TryFindPlayer()
        {
            if (target != null || !findPlayerAutomatically) return;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) SetTarget(player.transform);
        }

        private void OnValidate()
        {
            positionSmoothTime = Mathf.Max(0f, positionSmoothTime);
            rotationSharpness = Mathf.Max(0f, rotationSharpness);
            orbitSpeed = Mathf.Max(0f, orbitSpeed);
            maximumPitch = Mathf.Max(minimumPitch, maximumPitch);
        }
    }
}
