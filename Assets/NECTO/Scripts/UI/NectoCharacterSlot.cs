using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Necto.UI
{
    /// <summary>設定画面に表示する、1個分のプロフィールアイコン選択枠。</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class NectoCharacterSlot : MonoBehaviour
    {
        [Header("Inspectorで接続")]
        [SerializeField] private Button button;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private NectoPrefabIconPresenter iconPresenter;
        [SerializeField] private Shadow selectionShadow;
        [SerializeField] private TMP_Text nameText;

        [Header("この枠の表示内容")]
        [SerializeField] private GameObject iconPrefab;
        [SerializeField] private string displayName;

        private int index;
        private Action<int> clicked;

        public GameObject IconPrefab => iconPrefab;

        public static NectoCharacterSlot Create(Transform parent, Vector2 size)
        {
            GameObject slotObject = new GameObject("Character Icon", typeof(RectTransform));
            slotObject.transform.SetParent(parent, false);

            RectTransform rect = slotObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            Image background = slotObject.AddComponent<Image>();
            background.sprite = null;
            background.color = Color.clear;
            background.preserveAspect = true;
            background.raycastTarget = true;

            GameObject iconObject = new GameObject("Character Portrait", typeof(RectTransform), typeof(NectoPrefabIconPresenter));
            iconObject.transform.SetParent(slotObject.transform, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(.1f, .1f);
            iconRect.anchorMax = new Vector2(.9f, .9f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            Button slotButton = slotObject.AddComponent<Button>();
            slotButton.targetGraphic = background;
            slotButton.transition = Selectable.Transition.None;

            Shadow shadow = slotObject.AddComponent<Shadow>();
            shadow.enabled = false;

            NectoCharacterSlot slot = slotObject.AddComponent<NectoCharacterSlot>();
            slot.button = slotButton;
            slot.backgroundImage = background;
            slot.iconPresenter = iconObject.GetComponent<NectoPrefabIconPresenter>();
            slot.selectionShadow = shadow;
            return slot;
        }

        public void ConfigureShadow(Color color, float strength, Vector2 distance)
        {
            if (selectionShadow == null) return;
            color.a = Mathf.Clamp01(strength);
            selectionShadow.effectColor = color;
            selectionShadow.effectDistance = distance;
            selectionShadow.useGraphicAlpha = true;
        }

        public void SetContent(GameObject newIconPrefab, string newDisplayName)
        {
            iconPrefab = newIconPrefab;
            displayName = newDisplayName;
            ApplyDisplay();
        }

        private void Awake()
        {
            ApplyDisplay();
        }

        public void Bind(int slotIndex, Action<int> onClicked)
        {
            index = slotIndex;
            clicked = onClicked;
            if (button == null) return;
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }

        public void SetSelected(bool selected)
        {
            if (selectionShadow != null) selectionShadow.enabled = selected;
            // 完成済みIcon Prefabが背景も持つため、選択表現はShadowだけで行う。
        }

        public void SetInteractable(bool interactable)
        {
            if (button != null) button.interactable = interactable;
        }

        private void HandleClick()
        {
            clicked?.Invoke(index);
        }

        private void ApplyDisplay()
        {
            if (iconPresenter != null) iconPresenter.Show(iconPrefab);
            if (nameText != null) nameText.text = displayName;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (button == null) button = GetComponent<Button>();
            if (backgroundImage == null) backgroundImage = GetComponent<Image>();
            if (iconPresenter == null)
            {
                Transform portrait = transform.Find("Character Portrait");
                if (portrait != null) iconPresenter = portrait.GetComponent<NectoPrefabIconPresenter>();
            }
            if (selectionShadow == null) selectionShadow = GetComponent<Shadow>();
            ApplyDisplay();
        }
#endif
    }
}
