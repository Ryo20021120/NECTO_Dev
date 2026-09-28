using System.Collections.Generic;
using System.Linq;
using Necto.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace Necto.UI
{
    /// <summary>縦3枠のパーティと、その右側のボックスを表示する。</summary>
    public sealed class NectoMonsterSelectionScreen : MonoBehaviour
    {
        [Header("一覧の配置先（Inspectorで接続）")]
        [SerializeField] private Transform partySlotContainer;
        [SerializeField] private Transform boxSlotContainer;

        [Header("生成するアイコン")]
        [SerializeField] private Vector2 iconSize = new Vector2(150f, 150f);
        [SerializeField] private Color selectedShadowColor = new Color(0f, 0.75f, 1f, 1f);
        [SerializeField, Range(0f, 1f)] private float selectedShadowStrength = 0.9f;
        [SerializeField] private Vector2 selectedShadowDistance = new Vector2(6f, -6f);
        [SerializeField] private Color swapSourceShadowColor = new Color(1f, 0.65f, 0.1f, 1f);
        [SerializeField, Range(0f, 1f)] private float swapSourceShadowStrength = 1f;

        [Header("左側の詳細表示（必要なものだけ接続）")]
        [SerializeField] private NectoCharacterPreview monsterPreview;
        [SerializeField] private TMP_Text monsterNameText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text descriptionText;

        private readonly List<NectoMonsterSlot> displayedSlots = new List<NectoMonsterSlot>();
        private readonly List<NectoCharacterEntry> displayedMonsters = new List<NectoCharacterEntry>();
        private NectoCharacterDatabase database;
        private int selectedIndex;
        private int partyCount;
        private int pendingPartyIndex = -1;
        private int pendingBoxIndex = -1;
        private float nextNavigation;

        private void OnEnable() { Build(); }

        public void Build()
        {
            database = NectoCharacterDatabase.Load();
            ClearGeneratedSlots();
            displayedSlots.Clear();
            displayedMonsters.Clear();
            if (database == null) { ClearDetails(); return; }

            List<NectoCharacterEntry> party = NectoPartyRoster.PartyIds
                .Select(id => database.Find(id))
                .Where(entry => entry != null)
                .Take(3)
                .ToList();

            List<NectoCharacterEntry> box = NectoPartyRoster.BoxIds
                .Select(id => database.Find(id))
                .Where(entry => entry != null && entry.enabled)
                .ToList();

            partyCount = party.Count;
            BuildGroup(partySlotContainer, party, true);
            BuildGroup(boxSlotContainer, box, false);
            selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, displayedMonsters.Count - 1));
            RefreshSelection();
            RebuildLayout(partySlotContainer);
            RebuildLayout(boxSlotContainer);
        }

        private void BuildGroup(Transform container, List<NectoCharacterEntry> entries, bool isParty)
        {
            if (container == null) return;
            var groupSlots = new List<NectoMonsterSlot>();
            for (int i = 0; i < entries.Count; i++)
                groupSlots.Add(NectoMonsterSlot.Create(container, iconSize));

            GridLayoutGroup grid = container.GetComponent<GridLayoutGroup>();
            if (grid != null) grid.cellSize = iconSize;

            for (int i = 0; i < groupSlots.Count; i++)
            {
                bool available = i < entries.Count;
                groupSlots[i].gameObject.SetActive(available);
                if (!available) continue;
                int selectionIndex = displayedMonsters.Count;
                NectoCharacterEntry entry = entries[i];
                groupSlots[i].name = $"{(isParty ? "Party" : "Box")} Slot {i + 1:00} - {entry.displayName}";
                groupSlots[i].SetContent(entry, entry.iconPrefab);
                groupSlots[i].Bind(selectionIndex, ActivateSlot);
                groupSlots[i].ConfigureShadow(selectedShadowColor, selectedShadowStrength, selectedShadowDistance);
                displayedSlots.Add(groupSlots[i]);
                displayedMonsters.Add(entry);
            }
        }

        private void ClearGeneratedSlots()
        {
            ClearGeneratedSlots(partySlotContainer);
            ClearGeneratedSlots(boxSlotContainer);
        }

        private static void ClearGeneratedSlots(Transform container)
        {
            if (container == null) return;
            NectoMonsterSlot[] oldSlots = container.GetComponentsInChildren<NectoMonsterSlot>(true);
            foreach (NectoMonsterSlot oldSlot in oldSlots)
            {
                oldSlot.gameObject.SetActive(false);
                oldSlot.transform.SetParent(null, false);
                Destroy(oldSlot.gameObject);
            }
        }

        private static void RebuildLayout(Transform container)
        {
            Canvas.ForceUpdateCanvases();
            if (container is RectTransform rect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }

        public void Select(int index)
        {
            if (index < 0 || index >= displayedMonsters.Count) return;
            selectedIndex = index;
            RefreshSelection();
        }

        public void ConfirmSelection()
        {
            ActivateSlot(selectedIndex);
        }

        public bool CancelSwap()
        {
            if (pendingPartyIndex < 0 && pendingBoxIndex < 0) return false;
            pendingPartyIndex = -1;
            pendingBoxIndex = -1;
            RefreshSelection();
            return true;
        }

        private void ActivateSlot(int index)
        {
            if (index < 0 || index >= displayedMonsters.Count) return;
            selectedIndex = index;

            if (index < partyCount)
            {
                pendingPartyIndex = index;
                if (pendingBoxIndex >= partyCount)
                {
                    SwapSelectedPair();
                    return;
                }
                RefreshSelection();
                return;
            }

            pendingBoxIndex = index;
            if (pendingPartyIndex < 0 || pendingPartyIndex >= partyCount)
            {
                RefreshSelection();
                return;
            }

            SwapSelectedPair();
        }

        private void SwapSelectedPair()
        {
            if (pendingPartyIndex < 0 || pendingPartyIndex >= partyCount ||
                pendingBoxIndex < partyCount || pendingBoxIndex >= displayedMonsters.Count) return;
            string outgoingId = displayedMonsters[pendingPartyIndex].id;
            string incomingId = displayedMonsters[pendingBoxIndex].id;
            int newSelectedIndex = pendingPartyIndex;
            NectoPartyRoster.Swap(outgoingId, incomingId);
            pendingPartyIndex = -1;
            pendingBoxIndex = -1;
            selectedIndex = newSelectedIndex;
            Build();
        }

        private void Update()
        {
            if (displayedMonsters.Count == 0) return;
            Gamepad gamepad = Gamepad.current;
            Keyboard keyboard = Keyboard.current;
            // Nintendo配置を含む現在のNECTO操作基準では、右側のA(buttonEast)を決定に使う。
            if ((gamepad != null && gamepad.buttonEast.wasPressedThisFrame) ||
                (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)))
                ConfirmSelection();
            if (Time.unscaledTime < nextNavigation) return;
            Vector2 navigation = Necto.Input.NectoGamepadInput.ReadNavigation(gamepad);
            if (keyboard != null)
            {
                if (keyboard.leftArrowKey.isPressed) navigation = Vector2.left;
                else if (keyboard.rightArrowKey.isPressed) navigation = Vector2.right;
                else if (keyboard.upArrowKey.isPressed) navigation = Vector2.up;
                else if (keyboard.downArrowKey.isPressed) navigation = Vector2.down;
            }
            if (Mathf.Abs(navigation.x) <= .55f && Mathf.Abs(navigation.y) <= .55f) return;
            Vector2 direction = Mathf.Abs(navigation.x) > Mathf.Abs(navigation.y)
                ? new Vector2(Mathf.Sign(navigation.x), 0f)
                : new Vector2(0f, Mathf.Sign(navigation.y));
            MoveSelection(direction);
            nextNavigation = Time.unscaledTime + .22f;
        }

        /// <summary>現在位置から、入力方向に実際に配置されている最寄りのスロットへ移動する。</summary>
        private void MoveSelection(Vector2 direction)
        {
            if (selectedIndex < 0 || selectedIndex >= displayedSlots.Count) return;

            Vector2 current = displayedSlots[selectedIndex].GetComponent<RectTransform>().position;
            int bestIndex = -1;
            float bestScore = float.MaxValue;

            for (int i = 0; i < displayedSlots.Count; i++)
            {
                if (i == selectedIndex || !displayedSlots[i].gameObject.activeInHierarchy) continue;
                Vector2 candidate = displayedSlots[i].GetComponent<RectTransform>().position;
                Vector2 delta = candidate - current;
                float forward = Vector2.Dot(delta, direction);
                if (forward <= 0.01f) continue;

                // 入力方向から外れている候補には大きなペナルティを付ける。
                float sideways = Mathf.Abs(Vector2.Dot(delta, new Vector2(-direction.y, direction.x)));
                float score = forward + sideways * 4f;
                if (score >= bestScore) continue;
                bestScore = score;
                bestIndex = i;
            }

            if (bestIndex >= 0) Select(bestIndex);
        }

        private void RefreshSelection()
        {
            for (int i = 0; i < displayedSlots.Count; i++)
            {
                bool pending = i == pendingPartyIndex || i == pendingBoxIndex;
                displayedSlots[i].ConfigureShadow(
                    pending ? swapSourceShadowColor : selectedShadowColor,
                    pending ? swapSourceShadowStrength : selectedShadowStrength,
                    selectedShadowDistance);
                displayedSlots[i].SetSelected(i == selectedIndex || pending);
            }
            if (displayedMonsters.Count == 0) { ClearDetails(); return; }

            NectoCharacterEntry entry = displayedMonsters[selectedIndex];
            if (monsterPreview != null && entry.prefab != null)
                monsterPreview.Show(entry.prefab);
            if (monsterNameText != null) monsterNameText.text = entry.displayName;
            if (levelText != null) levelText.text = $"Lv.{entry.level}";
            if (statusText != null)
                statusText.text = $"HP {entry.maxHp}　MP {entry.maxMp}\n属性 {NectoMasterLabel.ToJapanese(entry.element)}";
            if (descriptionText != null) descriptionText.text = entry.description;
        }

        private void ClearDetails()
        {
            if (monsterNameText != null) monsterNameText.text = string.Empty;
            if (levelText != null) levelText.text = string.Empty;
            if (statusText != null) statusText.text = string.Empty;
            if (descriptionText != null) descriptionText.text = string.Empty;
        }
    }
}
