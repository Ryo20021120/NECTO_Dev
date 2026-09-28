using System.Collections.Generic;
using UnityEngine;

namespace Necto.UI
{
    /// <summary>設定画面内のサイドバーと各設定ページだけを管理する。</summary>
    public sealed class NectoSettingsScreenController : MonoBehaviour
    {
        [Header("サイドバー（上から順にInspectorで接続）")]
        [SerializeField] private List<NectoSettingsTabButton> tabs = new List<NectoSettingsTabButton>();
        [Header("アイコン設定ページ")]
        [SerializeField] private NectoCharacterSelectionScreen characterSelectionScreen;
        [SerializeField, Min(0)] private int characterTabIndex;

        private int selectedPage;
        private bool initialized;

        public void Initialize()
        {
            if (initialized) return;
            initialized = true;
            tabs.RemoveAll(tab => tab == null);
            for (int i = 0; i < tabs.Count; i++) tabs[i].Bind(i, ShowPage);
            selectedPage = Mathf.Clamp(selectedPage, 0, Mathf.Max(0, tabs.Count - 1));
        }

        public void ShowCurrentPage() { Initialize(); ShowPage(selectedPage); }

        public bool Navigate(Vector2 navigation)
        {
            Initialize();
            if (Mathf.Abs(navigation.y) > .55f && tabs.Count > 0)
            {
                int direction = navigation.y < 0f ? 1 : tabs.Count - 1;
                ShowPage((selectedPage + direction) % tabs.Count);
                return true;
            }
            if (selectedPage == characterTabIndex && characterSelectionScreen != null && Mathf.Abs(navigation.x) > .55f)
            {
                if (navigation.x > 0f) characterSelectionScreen.SelectNext();
                else characterSelectionScreen.SelectPrevious();
                return true;
            }
            return false;
        }

        public void Confirm()
        {
            if (selectedPage == characterTabIndex && characterSelectionScreen != null)
                characterSelectionScreen.ConfirmSelected();
        }

        private void ShowPage(int index)
        {
            if (tabs.Count == 0) return;
            selectedPage = Mathf.Clamp(index, 0, tabs.Count - 1);
            for (int i = 0; i < tabs.Count; i++)
            {
                GameObject page = tabs[i].Page;
                if (page != null) page.SetActive(i == selectedPage);
                tabs[i].SetSelected(i == selectedPage);
            }
            if (selectedPage == characterTabIndex && characterSelectionScreen != null)
                characterSelectionScreen.RefreshScreen();
        }
    }
}
