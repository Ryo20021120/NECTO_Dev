using System;
using UnityEngine;
using UnityEngine.UI;

namespace Necto.UI
{
    /// <summary>設定画面のサイドバーにある1個分のタブ。</summary>
    public sealed class NectoSettingsTabButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Shadow selectionShadow;
        [SerializeField] private GameObject page;

        private int index;
        private Action<int> clicked;

        public GameObject Page => page;

        public void Bind(int tabIndex, Action<int> onClicked)
        {
            index = tabIndex;
            clicked = onClicked;
            if (button == null) return;
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }

        public void SetSelected(bool selected)
        {
            if (selectionShadow != null) selectionShadow.enabled = selected;
        }

        private void HandleClick()
        {
            clicked?.Invoke(index);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (button == null) button = GetComponent<Button>();
            if (selectionShadow == null) selectionShadow = GetComponent<Shadow>();
        }
#endif
    }
}
