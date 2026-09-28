using System;
using Necto.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Necto.UI
{
    /// <summary>モンスター一覧に表示する1枠。</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class NectoMonsterSlot : MonoBehaviour
    {
        [Header("Inspectorで接続")]
        [SerializeField] private Button button;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private NectoPrefabIconPresenter iconPresenter;
        [SerializeField] private Shadow selectionShadow;
        [SerializeField] private TMP_Text nameText;

        private int index;
        private Action<int> clicked;

        public static NectoMonsterSlot Create(Transform parent, Vector2 size)
        {
            GameObject slotObject = new GameObject("Monster Icon", typeof(RectTransform));
            slotObject.transform.SetParent(parent, false);

            RectTransform rect = slotObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            Image background = slotObject.AddComponent<Image>();
            background.sprite = null;
            background.color = Color.clear;
            background.preserveAspect = true;
            background.raycastTarget = true;

            GameObject iconObject = new GameObject("Monster Portrait", typeof(RectTransform), typeof(NectoPrefabIconPresenter));
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

            NectoMonsterSlot slot = slotObject.AddComponent<NectoMonsterSlot>();
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

        public void SetContent(NectoCharacterEntry entry, GameObject iconPrefab)
        {
            if (iconPresenter != null) iconPresenter.Show(iconPrefab);
            if (nameText != null) nameText.text = entry != null ? entry.displayName : string.Empty;
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

        private void HandleClick()
        {
            clicked?.Invoke(index);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (button == null) button = GetComponent<Button>();
            if (backgroundImage == null) backgroundImage = GetComponent<Image>();
            if (iconPresenter == null)
            {
                Transform portrait = transform.Find("Monster Portrait");
                if (portrait != null) iconPresenter = portrait.GetComponent<NectoPrefabIconPresenter>();
            }
        }
#endif
    }
}
