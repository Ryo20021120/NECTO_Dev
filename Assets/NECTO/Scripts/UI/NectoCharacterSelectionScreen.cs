using System.Collections.Generic;
using Necto.Data;
using Necto.Development;
using Necto.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Necto.UI
{
    /// <summary>プレイアブルキャラの一覧・選択・プレビューだけを管理する。</summary>
    public sealed class NectoCharacterSelectionScreen : MonoBehaviour
    {
        [Header("Inspectorで接続")]
        [SerializeField] private NectoPlayableCharacterDatabase playableDatabase;
        [Tooltip("任意。選択確定時に、街に配置済みの操作キャラも即時切り替える場合だけ接続します。")]
        [SerializeField] private NectoCharacterVisualContainer playableCharacters;
        [SerializeField] private Transform slotContainer;
        [SerializeField] private NectoCharacterCarousel carousel;
        [SerializeField] private NectoCharacterPreview preview;

        [Header("生成するアイコン")]
        [SerializeField] private Vector2 iconSize = new Vector2(200f, 200f);
        [SerializeField] private Color selectedShadowColor = new Color(0f, 0.75f, 1f, 1f);
        [SerializeField, Range(0f, 1f)] private float selectedShadowStrength = 0.9f;
        [SerializeField] private Vector2 selectedShadowDistance = new Vector2(6f, -6f);

        private readonly List<NectoCharacterSlot> slots = new List<NectoCharacterSlot>();
        private readonly List<NectoPlayableCharacterData> entries = new List<NectoPlayableCharacterData>();
        private readonly List<GameObject> sceneVisuals = new List<GameObject>();
        private int selectedIndex;

        public int Count => entries.Count;

        private void Awake() { Build(); }

        public void RefreshScreen() { Build(); }

        public void Build()
        {
            entries.Clear();
            NectoPlayableCharacterDatabase database = playableDatabase != null
                ? playableDatabase
                : NectoPlayableCharacterDatabase.Load();
            if (database != null)
                entries.AddRange(database.Characters);

            ClearGeneratedSlots();
            if (slotContainer != null)
                for (int i = 0; i < entries.Count; i++)
                    slots.Add(NectoCharacterSlot.Create(slotContainer, iconSize));

            for (int i = 0; i < slots.Count; i++)
            {
                bool available = i < entries.Count;
                slots[i].gameObject.SetActive(available);
                if (!available) continue;
                slots[i].SetContent(entries[i].iconPrefab, entries[i].displayName);
                slots[i].Bind(i, SelectAndConfirm);
                slots[i].SetInteractable(true);
                slots[i].ConfigureShadow(selectedShadowColor, selectedShadowStrength, selectedShadowDistance);
            }

            if (playableCharacters == null)
                playableCharacters = FindFirstObjectByType<NectoCharacterVisualContainer>();

            Debug.Log($"[Build] playableCharacters={playableCharacters?.name ?? "null"}");
            if (playableCharacters != null)
                Debug.Log($"[Build] playableCharacters.transform.childCount={playableCharacters.transform.childCount}");

            sceneVisuals.Clear();
            if (playableCharacters != null) playableCharacters.CollectCharacters(sceneVisuals);

            Debug.Log($"[Build] sceneVisuals count={sceneVisuals.Count}");
            for (int i = 0; i < sceneVisuals.Count; i++)
                Debug.Log($"  [{i}] {sceneVisuals[i]?.name}");

            // PlayerData から選択済みキャラを読み込む
            PlayerData playerData = PlayerDataManager.Load();
            selectedIndex = -1;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].id == playerData.selectedCharacterId)
                {
                    selectedIndex = i;
                    break;
                }
            }
            selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, entries.Count - 1));
            Refresh();

            ShowSelectedVisual();

            RebuildLayout();
        }

        private void ClearGeneratedSlots()
        {
            slots.Clear();
            if (slotContainer == null) return;
            NectoCharacterSlot[] oldSlots = slotContainer.GetComponentsInChildren<NectoCharacterSlot>(true);
            foreach (NectoCharacterSlot oldSlot in oldSlots)
            {
                oldSlot.gameObject.SetActive(false);
                oldSlot.transform.SetParent(null, false);
                Destroy(oldSlot.gameObject);
            }
        }

        private void RebuildLayout()
        {
            Canvas.ForceUpdateCanvases();
            if (slotContainer is RectTransform rect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }

        public void SelectNext()
        {
            if (entries.Count == 0) return;
            Select((selectedIndex + 1) % entries.Count);
        }

        public void SelectPrevious()
        {
            if (entries.Count == 0) return;
            Select((selectedIndex - 1 + entries.Count) % entries.Count);
        }

        public void ConfirmSelected()
        {
            if (selectedIndex < 0 || selectedIndex >= entries.Count) return;

            // PlayerData に選択キャラクターを保存
            NectoPlayableCharacterData selected = entries[selectedIndex];
            PlayerData playerData = PlayerDataManager.Load();
            playerData.selectedCharacterId = selected.id;
            PlayerDataManager.Save(playerData);

            ShowSelectedVisual();

            Refresh();
        }

        private void RefreshPlayerAnimator()
        {
            if (playableCharacters == null || playableCharacters.PlayerRoot == null) return;

            NectoPlayerMovement movement =
                playableCharacters.PlayerRoot.GetComponent<NectoPlayerMovement>();
            if (movement != null) movement.RefreshAnimator();

            NectoPlayerAttackController attack =
                playableCharacters.PlayerRoot.GetComponent<NectoPlayerAttackController>();
            if (attack != null) attack.RefreshAnimator();

            NectoPlayerDodgeController dodge =
                playableCharacters.PlayerRoot.GetComponent<NectoPlayerDodgeController>();
            if (dodge != null) dodge.RefreshAnimator();
        }

        private void ShowSelectedVisual()
        {
            if (playableCharacters == null || selectedIndex < 0 || selectedIndex >= entries.Count) return;

            playableCharacters.gameObject.SetActive(true);
            GameObject targetVisual = playableCharacters.FindByCharacterId(entries[selectedIndex].id);

            // 旧プレハブとの互換用。ID対応が未設定でも名前から探す。
            if (targetVisual == null && entries[selectedIndex].prefab != null)
                targetVisual = sceneVisuals.Find(visual =>
                    visual != null && visual.name == entries[selectedIndex].prefab.name);

            // 誰も表示されない状態だけは避ける。
            if (targetVisual == null && sceneVisuals.Count > 0)
            {
                targetVisual = sceneVisuals[0];
                Debug.LogWarning(
                    $"NECTO: '{entries[selectedIndex].id}' の表示対応がないため、先頭キャラクターを表示しました。",
                    playableCharacters);
            }

            foreach (GameObject visual in sceneVisuals)
                if (visual != null) visual.SetActive(visual == targetVisual);

            if (targetVisual != null) RefreshPlayerAnimator();
        }

        private void SelectAndConfirm(int index)
        {
            Select(index);
            ConfirmSelected();
        }

        private void Select(int index)
        {
            if (index < 0 || index >= entries.Count) return;
            selectedIndex = index;
            Refresh();
        }

        private void Refresh()
        {
            for (int i = 0; i < slots.Count; i++)
                slots[i].SetSelected(i == selectedIndex);
            if (carousel != null) carousel.Select(selectedIndex);
            if (preview != null && selectedIndex >= 0 && selectedIndex < entries.Count && entries[selectedIndex].prefab != null)
                preview.Show(entries[selectedIndex].prefab);
        }
    }
}
