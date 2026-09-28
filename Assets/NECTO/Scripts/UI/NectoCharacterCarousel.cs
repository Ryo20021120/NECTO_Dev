using UnityEngine;

namespace Necto.UI
{
    /// <summary>直下に置かれたUIまたは3Dスロットを正面向きの楕円状に配置する。</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class NectoCharacterCarousel : MonoBehaviour
    {
        [Header("楕円の形")]
        [SerializeField, Min(0f)] private float horizontalRadius = 420f;
        [SerializeField, Min(0f)] private float verticalRadius = 180f;
        [SerializeField] private float selectedAngle = -90f;
        [SerializeField] private bool useFullCircle = true;
        [SerializeField, Range(1f, 180f)] private float angleSpacing = 40f;

        [Header("見た目")]
        [SerializeField] private Vector2 slotSize = new Vector2(200f, 200f);
        [SerializeField, Min(0.01f)] private float selectedScale = 1.2f;
        [SerializeField, Min(0.01f)] private float backScale = 0.6f;
        [SerializeField, Min(0f)] private float smoothSpeed = 10f;
        [SerializeField] private bool faceCircleCenter = true;
        [SerializeField] private float rotationOffset;
        [SerializeField, Range(-89f, 89f)] private float slotTiltX;
        [SerializeField, Range(-89f, 89f)] private float slotTiltY;
        [SerializeField, Range(-89f, 89f)] private float selectedSlotTiltX;
        [SerializeField, Range(-89f, 89f)] private float selectedSlotTiltY;

        [Header("現在の選択")]
        [SerializeField, Min(0)] private int selectedIndex;

        private int lastChildCount = -1;

        public int SelectedIndex => selectedIndex;
        public int SlotCount => transform.childCount;

        private void OnEnable()
        {
            Arrange(true);
        }

        private void OnValidate()
        {
            selectedIndex = ClampIndex(selectedIndex);
            Arrange(true);
        }

        private void Update()
        {
            if (lastChildCount != transform.childCount)
            {
                lastChildCount = transform.childCount;
                selectedIndex = ClampIndex(selectedIndex);
                Arrange(true);
                return;
            }

            Arrange(!Application.isPlaying || smoothSpeed <= 0f);
        }

        public void Select(int index)
        {
            selectedIndex = ClampIndex(index);
            Arrange(!Application.isPlaying);
        }

        public void SelectNext()
        {
            if (SlotCount == 0) return;
            Select((selectedIndex + 1) % SlotCount);
        }

        public void SelectPrevious()
        {
            if (SlotCount == 0) return;
            Select((selectedIndex - 1 + SlotCount) % SlotCount);
        }

        [ContextMenu("子スロットを今すぐ配置")]
        private void ArrangeImmediately()
        {
            Arrange(true);
        }

        private void Arrange(bool immediate)
        {
            int count = transform.childCount;
            if (count == 0) return;

            float step = useFullCircle ? 360f / count : angleSpacing;
            float interpolation = immediate ? 1f : 1f - Mathf.Exp(-smoothSpeed * Time.unscaledDeltaTime);

            for (int i = 0; i < count; i++)
            {
                Transform slot = transform.GetChild(i);

                int relativeIndex = WrappedDistance(i, selectedIndex, count);
                float relativeAngle = relativeIndex * step;
                float angle = (selectedAngle + relativeAngle) * Mathf.Deg2Rad;
                Vector2 targetPosition = new Vector2(
                    Mathf.Cos(angle) * horizontalRadius,
                    Mathf.Sin(angle) * verticalRadius);

                float frontAmount = (Mathf.Cos(relativeAngle * Mathf.Deg2Rad) + 1f) * 0.5f;
                float scale = Mathf.Lerp(backScale, selectedScale, frontAmount);
                Vector3 targetScale = Vector3.one * scale;

                RectTransform uiSlot = slot as RectTransform;
                if (uiSlot != null)
                {
                    // anchoredPosition (0, 0) が必ずContainerの中心になるようにする。
                    uiSlot.anchorMin = new Vector2(0.5f, 0.5f);
                    uiSlot.anchorMax = new Vector2(0.5f, 0.5f);
                    uiSlot.pivot = new Vector2(0.5f, 0.5f);
                    uiSlot.sizeDelta = slotSize;
                    uiSlot.anchoredPosition = Vector2.Lerp(uiSlot.anchoredPosition, targetPosition, interpolation);
                    SetDisplayOrder(uiSlot, relativeIndex == 0, frontAmount);
                }
                else
                {
                    Vector3 targetLocalPosition = new Vector3(targetPosition.x, targetPosition.y, 0f);
                    slot.localPosition = Vector3.Lerp(slot.localPosition, targetLocalPosition, interpolation);
                }
                slot.localScale = Vector3.Lerp(slot.localScale, targetScale, interpolation);

                if (faceCircleCenter)
                {
                    Vector2 directionToCenter = -targetPosition;
                    float zAngle = Mathf.Atan2(directionToCenter.y, directionToCenter.x) * Mathf.Rad2Deg - 90f + rotationOffset;
                    bool isSelected = relativeIndex == 0;
                    float tiltX = isSelected ? selectedSlotTiltX : slotTiltX;
                    float tiltY = isSelected ? selectedSlotTiltY : slotTiltY;
                    Quaternion targetRotation =
                        Quaternion.AngleAxis(zAngle, Vector3.forward) *
                        Quaternion.Euler(tiltX, tiltY, 0f);
                    slot.localRotation = Quaternion.Slerp(slot.localRotation, targetRotation, interpolation);
                }
            }
        }

        private int ClampIndex(int index)
        {
            int count = transform.childCount;
            return count == 0 ? 0 : Mathf.Clamp(index, 0, count - 1);
        }

        private static void SetDisplayOrder(RectTransform slot, bool selected, float frontAmount)
        {
            Canvas slotCanvas = slot.GetComponent<Canvas>();
            if (slotCanvas == null && Application.isPlaying)
                slotCanvas = slot.gameObject.AddComponent<Canvas>();
            if (slotCanvas == null) return;

            if (Application.isPlaying && slot.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                slot.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            slotCanvas.overrideSorting = true;
            slotCanvas.sortingOrder = selected ? 1000 : Mathf.RoundToInt(frontAmount * 100f);
        }

        private static int WrappedDistance(int index, int selected, int count)
        {
            int distance = index - selected;
            int half = count / 2;
            if (distance > half) distance -= count;
            if (distance < -half) distance += count;
            return distance;
        }
    }
}
