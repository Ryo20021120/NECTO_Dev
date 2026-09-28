using UnityEngine;
using UnityEngine.InputSystem;

namespace Necto.Input
{
    /// <summary>全ゲーム画面で共通使用するスティック入力。中心ずれを吸収する。</summary>
    public static class NectoGamepadInput
    {
        public const float StickDeadzone = 0.35f;

        public static Vector2 ReadLeftStick(Gamepad gamepad)
        {
            return gamepad == null ? Vector2.zero : ApplyRadialDeadzone(gamepad.leftStick.ReadValue());
        }

        public static Vector2 ReadRightStick(Gamepad gamepad)
        {
            return gamepad == null ? Vector2.zero : ApplyRadialDeadzone(gamepad.rightStick.ReadValue());
        }

        public static Vector2 ReadNavigation(Gamepad gamepad)
        {
            if (gamepad == null) return Vector2.zero;
            Vector2 dpad = gamepad.dpad.ReadValue();
            return dpad.sqrMagnitude > .25f ? dpad : ReadLeftStick(gamepad);
        }

        public static Vector2 ApplyRadialDeadzone(Vector2 value)
        {
            float magnitude = value.magnitude;
            if (magnitude <= StickDeadzone) return Vector2.zero;
            float normalized = Mathf.Clamp01((magnitude - StickDeadzone) / (1f - StickDeadzone));
            return value.normalized * normalized;
        }
    }
}
