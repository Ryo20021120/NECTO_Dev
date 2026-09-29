using UnityEngine;
using UnityEngine.InputSystem;
using Necto.Input;

namespace Necto.CameraSystem
{
    /// <summary>
    /// Starter Assets風の追従ピボットを使う三人称カメラ。
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
        [SerializeField] private float shoulderOffset = 0.35f;

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

        [Header("壁との衝突")]
        [SerializeField] private bool avoidObstacles = true;
        [Min(0.01f)]
        [SerializeField] private float cameraRadius = 0.25f;
        [Min(0f)]
        [SerializeField] private float collisionPadding = 0.12f;
        [SerializeField] private LayerMask collisionLayers = ~0;

        private Vector3 followVelocity;
        private Vector3 smoothedPivot;
        private float orbitYaw;
        private float orbitPitch;
        private float orbitDistance;
        private bool orbitInitialized;
        private readonly RaycastHit[] collisionHits = new RaycastHit[12];

        public Transform Target => target;

        private void Awake()
        {
            TryFindPlayer();
        }

        private void OnEnable()
        {
            followVelocity = Vector3.zero;
            if (target != null) smoothedPivot = target.position + lookAtOffset;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                TryFindPlayer();
                if (target == null) return;
            }

            UpdateOrbitInput();
            Vector3 targetPivot = target.position + lookAtOffset;
            float pivotBlend = positionSmoothTime <= 0f
                ? 1f
                : 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, positionSmoothTime));
            smoothedPivot = Vector3.Lerp(smoothedPivot, targetPivot, pivotBlend);

            Quaternion orbitRotation = Quaternion.Euler(orbitPitch, orbitYaw, 0f);
            Vector3 orbitOffset = orbitRotation *
                (Vector3.right * shoulderOffset + Vector3.back * orbitDistance);
            Vector3 desiredPosition = smoothedPivot + orbitOffset;
            desiredPosition = ResolveObstacleCollision(smoothedPivot, desiredPosition);
            transform.position = positionSmoothTime <= 0f
                ? desiredPosition
                : Vector3.SmoothDamp(
                    transform.position,
                    desiredPosition,
                    ref followVelocity,
                    positionSmoothTime);

            Vector3 lookDirection = smoothedPivot - transform.position;
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
                smoothedPivot = target.position + lookAtOffset;
                Vector3 orbitOffset = Quaternion.Euler(orbitPitch, orbitYaw, 0f) *
                    (Vector3.right * shoulderOffset + Vector3.back * orbitDistance);
                transform.position = ResolveObstacleCollision(smoothedPivot, smoothedPivot + orbitOffset);
                transform.LookAt(smoothedPivot);
            }
        }

        private void InitializeOrbit()
        {
            // positionOffsetはTarget原点基準の旧設定なので、
            // 上半身ピボット基準のオフセットへ変換する。
            Vector3 offset = positionOffset - lookAtOffset - Vector3.right * shoulderOffset;
            orbitDistance = Mathf.Max(0.1f, offset.magnitude);
            float horizontalDistance = new Vector2(offset.x, offset.z).magnitude;
            orbitYaw = Mathf.Atan2(-offset.x, -offset.z) * Mathf.Rad2Deg;
            orbitPitch = Mathf.Atan2(offset.y, horizontalDistance) * Mathf.Rad2Deg;
            orbitPitch = Mathf.Clamp(orbitPitch, minimumPitch, maximumPitch);
            orbitInitialized = true;
        }

        private Vector3 ResolveObstacleCollision(Vector3 pivot, Vector3 desiredPosition)
        {
            if (!avoidObstacles) return desiredPosition;

            Vector3 direction = desiredPosition - pivot;
            float distance = direction.magnitude;
            if (distance <= Mathf.Epsilon) return desiredPosition;
            direction /= distance;

            int hitCount = Physics.SphereCastNonAlloc(
                pivot,
                cameraRadius,
                direction,
                collisionHits,
                distance,
                collisionLayers,
                QueryTriggerInteraction.Ignore);

            float nearestDistance = distance;
            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = collisionHits[i].collider;
                if (hitCollider == null || IsPartOfTarget(hitCollider.transform)) continue;
                nearestDistance = Mathf.Min(nearestDistance, collisionHits[i].distance);
            }

            if (nearestDistance >= distance) return desiredPosition;
            float safeDistance = Mathf.Max(0.05f, nearestDistance - collisionPadding);
            return pivot + direction * safeDistance;
        }

        private bool IsPartOfTarget(Transform candidate)
        {
            if (target == null || candidate == null) return false;
            return candidate == target || candidate.IsChildOf(target) || target.IsChildOf(candidate);
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
            cameraRadius = Mathf.Max(0.01f, cameraRadius);
            collisionPadding = Mathf.Max(0f, collisionPadding);
            maximumPitch = Mathf.Max(minimumPitch, maximumPitch);
        }
    }
}
